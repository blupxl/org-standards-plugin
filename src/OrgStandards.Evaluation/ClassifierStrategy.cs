using System.Diagnostics;
using System.Text.Json;
using OrgStandards.Contracts;
using OrgStandards.Data;
using OrgStandards.Gateway;

namespace OrgStandards.Evaluation;

// What a classifier decided for one component of a task: the categories it inferred and the
// get_standards filter for that component.
public sealed record ComponentClassification(string Name, string[] Categories, Dictionary<string, string[]> Filter);

// What a classifier decided for one task: one entry per component, each with its own filter.
public sealed record Classification(IReadOnlyList<ComponentClassification> Components)
{
    public string[] Categories =>
        Components.SelectMany(component => component.Categories).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
}

public interface IClassifier
{
    Classification Classify(string task, IReadOnlyDictionary<string, string[]> declaredScope);
}

// The plugin's classify skill, scored on the golden set. The task's scope stands in for the
// project's declared scope and stays authoritative, as it does for the skill: its values are always
// in the filter, beside the ones the classifier adds.
public sealed class ClassifierStrategy(IClassifier classifier, Taxonomy taxonomy) : IRetrievalStrategy
{
    public string Name => "classifier";

    public string Description => "The plugin's classify skill, run by its classifier agent in Claude Code, with the taxonomy from get_taxonomy.";

    public bool Ranked => false;

    public Retrieval Retrieve(GoldenTask task, StandardsCorpus corpus)
    {
        var classification = classifier.Classify(task.Task, task.Scope);
        var combinedFilter = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        var topics = new List<RetrievedTopic>();
        var categories = taxonomy.ToContract();

        // Each component is retrieved with its own filter, so one component's values never narrow
        // or widen another's.
        foreach (var component in classification.Components)
        {
            var filter = WithDeclaredScope(component.Filter, task.Scope);
            foreach (var (field, values) in filter)
            {
                combinedFilter[field] = combinedFilter.TryGetValue(field, out var earlier)
                    ? earlier.Concat(values).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
                    : values;
            }

            if (component.Categories.Length == 0 && component.Filter.Count == 0)
            {
                continue;
            }

            var expanded = Ancestry.Expand(filter, categories);
            var matches = corpus.Topics
                .Where(owned => StandardMatcher.Matches(owned.Topic.Tags, expanded))
                .Select(owned => (owned.Source, owned.Topic));
            var (resolved, _) = Resolver.Overlay(matches);
            topics.AddRange(resolved.Select(topic => new RetrievedTopic(topic.Topic.Topic, topic.Source, topic.Product)));
        }

        if (classification.Components.Count == 0)
        {
            combinedFilter = WithDeclaredScope([], task.Scope);
        }

        return new Retrieval(
            classification.Categories,
            combinedFilter,
            topics.DistinctBy(topic => (topic.Topic, topic.Source)).ToList());
    }

    private static Dictionary<string, string[]> WithDeclaredScope(
        Dictionary<string, string[]> classified, IReadOnlyDictionary<string, string[]> declaredScope)
    {
        var filter = new Dictionary<string, string[]>(classified, StringComparer.OrdinalIgnoreCase);
        foreach (var (field, values) in declaredScope)
        {
            filter[field] = filter.TryGetValue(field, out var inferred)
                ? values.Concat(inferred).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
                : values;
        }

        return filter;
    }
}

// Runs the classifier agent through Claude Code, headless, one task per call. Needs the services
// running (get_taxonomy) and the `claude` executable on the PATH, and costs one model call per task,
// so the harness runs it only when asked (--strategy classifier). It runs in an empty folder, so no
// project's CLAUDE.md or settings change the result.
public sealed class ClaudeClassifier(string pluginDirectory) : IClassifier
{
    private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(3);

    public Classification Classify(string task, IReadOnlyDictionary<string, string[]> declaredScope)
    {
        var prompt =
            $"Use the classifier agent to classify this task. Its declared scope, as .claude/standards.json would give it: " +
            $"{JsonSerializer.Serialize(declaredScope)}. Task: {task}\n" +
            "Reply with only the agent's JSON block.";

        var workingDirectory = Directory.CreateTempSubdirectory("classifier-");
        try
        {
            return Run(task, prompt, workingDirectory.FullName);
        }
        finally
        {
            try
            {
                workingDirectory.Delete(recursive: true);
            }
            catch (IOException)
            {
                // Something still holds a file in it; the system's temp cleanup gets it later.
            }
        }
    }

    // The tools the run may use: the Agent tool and the plugin's two read-only taxonomy tools. A
    // plugin without a plugin.json is named after its folder, and Claude Code names a plugin's MCP
    // tools mcp__plugin_<plugin>_<server>__<tool>, with any character other than a letter, digit,
    // - or _ replaced by _.
    public static string AllowedTools(string pluginDirectory)
    {
        var folder = Path.GetFileName(Path.TrimEndingDirectorySeparator(Path.GetFullPath(pluginDirectory)));
        var plugin = new string(folder.Select(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_' ? character : '_').ToArray());
        return $"Agent,mcp__plugin_{plugin}_standards__get_taxonomy,mcp__plugin_{plugin}_standards__list_standards";
    }

    private Classification Run(string task, string prompt, string workingDirectory)
    {
        var start = new ProcessStartInfo("claude")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = workingDirectory,
        };
        foreach (var argument in new[]
                 {
                     "-p", prompt, "--plugin-dir", Path.GetFullPath(pluginDirectory), "--output-format", "json",
                     "--allowedTools", AllowedTools(pluginDirectory),
                 })
        {
            start.ArgumentList.Add(argument);
        }

        using var process = Process.Start(start) ?? throw new InvalidOperationException("Couldn't start claude.");
        var errorText = process.StandardError.ReadToEndAsync();
        var outputText = process.StandardOutput.ReadToEndAsync();
        if (!process.WaitForExit(Timeout))
        {
            process.Kill(entireProcessTree: true);
            Console.Error.WriteLine($"Classifier: \"{task}\" timed out after {Timeout.TotalMinutes:0} minutes.");
            return new Classification([]);
        }

        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            Console.Error.WriteLine($"Classifier: \"{task}\" failed with exit code {process.ExitCode}: {errorText.Result.Trim()}");
            return new Classification([]);
        }

        return Parse(outputText.Result);
    }

    // claude --output-format json wraps the reply as { "result": "<text>" }; the reply holds the
    // classification as one JSON object. Each component keeps its own inferred values and filter.
    public static Classification Parse(string claudeOutput)
    {
        try
        {
            return ParseOrThrow(claudeOutput);
        }
        catch (JsonException)
        {
            return new Classification([]);
        }
    }

    private static Classification ParseOrThrow(string claudeOutput)
    {
        using var wrapper = JsonDocument.Parse(claudeOutput);
        var reply = wrapper.RootElement.ValueKind == JsonValueKind.Object && wrapper.RootElement.TryGetProperty("result", out var result) && result.ValueKind == JsonValueKind.String
            ? result.GetString() ?? ""
            : "";
        var start = reply.IndexOf('{');
        var end = reply.LastIndexOf('}');
        if (start < 0 || end <= start)
        {
            return new Classification([]);
        }

        using var json = JsonDocument.Parse(reply[start..(end + 1)]);
        if (!json.RootElement.TryGetProperty("components", out var components) || components.ValueKind != JsonValueKind.Array)
        {
            return new Classification([]);
        }

        var objects = components.EnumerateArray().Where(component => component.ValueKind == JsonValueKind.Object).ToArray();
        if (objects.Length == 0)
        {
            return new Classification([]);
        }

        return new Classification(objects.Select(Component).ToArray());
    }

    private static ComponentClassification Component(JsonElement component)
    {
        var name = component.TryGetProperty("name", out var nameValue) && nameValue.ValueKind == JsonValueKind.String
            ? nameValue.GetString() ?? ""
            : "";
        var categories = Fields(component, "inferred")
            .SelectMany(field => field.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var filter = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in Fields(component, "filter"))
        {
            filter[field.Key] = field.Value;
        }

        return new ComponentClassification(name, categories, filter);
    }

    private static IEnumerable<KeyValuePair<string, string[]>> Fields(JsonElement component, string property) =>
        component.TryGetProperty(property, out var fields) && fields.ValueKind == JsonValueKind.Object
            ? fields.EnumerateObject().Select(field => new KeyValuePair<string, string[]>(
                field.Name,
                field.Value.ValueKind == JsonValueKind.Array
                    ? field.Value.EnumerateArray().Where(value => value.ValueKind == JsonValueKind.String).Select(value => value.GetString()).OfType<string>().ToArray()
                    : []))
            : [];
}
