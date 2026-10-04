using System.Text.Json;

namespace OrgStandards.Contracts;

// A filter is field -> accepted values. OR within a field, AND across fields.
// Example: { "kind": ["api", "backend"], "concern": ["security"] } means (api OR backend) AND security.

public static class StandardFields
{
    // The field that layers specific topics over general ones.
    // With it in the filter, general topics (no product tag) come back as the base layer and a
    // product topic with the same name replaces them. Without it, only general topics come back.
    public const string Overlay = "product";

    // The runtime a topic is written for (dotnet, node, ...). A topic without one applies to every
    // runtime; a topic with one applies only when the request names that runtime.
    public const string Runtime = "runtime";

    // Fields that qualify a topic instead of classifying it: a topic tagged with one applies only
    // when the request asks for one of its values, and a topic without it applies to every request.
    // Product also layers (see Overlay).
    public static readonly string[] Qualifiers = [Overlay, Runtime];

    // On a runtime's topic: the general topics it says how to meet ("Settings in .NET" implements
    // "Settings"). Shown with the topic, so a reviewer reports one failure, not two.
    public const string Implements = "implements";
}

public static class Filters
{
    // Callers send JSON, so a list can hold nulls and a field can be null or blank. Every tool cleans
    // its input with this first: null and blank values are dropped and the rest trimmed (a repeat is
    // kept once), and a field left with no values, or with no name, is dropped, since an empty list
    // doesn't constrain anything. A missing filter is an empty one.
    public static Dictionary<string, string[]> Clean<TValues>(IEnumerable<KeyValuePair<string, TValues>>? filter)
        where TValues : IEnumerable<string?>?
    {
        var cleaned = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var (field, values) in filter ?? [])
        {
            var name = field?.Trim();
            var kept = (values ?? Enumerable.Empty<string?>())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (!string.IsNullOrEmpty(name) && kept.Length > 0)
            {
                cleaned[name] = cleaned.TryGetValue(name, out var existing)
                    ? existing.Concat(kept).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
                    : kept;
            }
        }

        return cleaned;
    }
}

public static class Json
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}

// Extra, free-form content for a topic (an example, a reference table). Fetched on demand.
public sealed record TopicDetail(string Title, string Markdown);

// One topic of a standards document: its rules (Body) plus optional details.
public sealed record StandardTopic(
    string Topic,
    string Document,
    string Version,
    string Body,
    TopicDetail[] Details,
    Dictionary<string, string[]> Tags);

// Every field a source knows, with every value it knows. The gateway uses it to validate filters.
public sealed record SourceCatalog(string Source, Dictionary<string, string[]> Fields);

public sealed record DescribeResult(SourceCatalog Catalog, Dictionary<string, string[]> Matching);

// Results[i] holds the topics matching the i-th filter of the request.
public sealed record QueryResult(SourceCatalog Catalog, List<StandardTopic>[] Results);
