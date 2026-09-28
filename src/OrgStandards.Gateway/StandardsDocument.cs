using System.Text;
using OrgStandards.Contracts;

namespace OrgStandards.Gateway;

// Renders what the implementing agent reads. The envelope (before the first "##") is a fixed
// contract: scope, status, sources, how precedence was resolved, and what the keywords mean.
// Everything after it is the owners' Markdown.
public static class StandardsDocument
{
    public static string Render(Resolution resolution)
    {
        var document = new StringBuilder();
        document.AppendLine("# Standards");
        AppendEnvelope(document, resolution);
        if (IsInvalid(resolution))
        {
            return document.ToString().TrimEnd();
        }

        document.AppendLine("> **Precedence:** already resolved. A product-specific topic replaces the general topic with the same name, and says so.");
        document.AppendLine("> **Keywords:** MUST / MUST NOT = required · SHOULD = strong default · anything else is guidance.");
        document.AppendLine("> **More detail:** topics with examples or reference material (e.g. color tokens) name the `get_topic` call that fetches it. Call it when your task needs that detail.");

        foreach (var topic in resolution.Topics)
        {
            document.AppendLine();
            AppendTopic(document, topic);

            if (topic.Topic.Details.Length > 0)
            {
                document.AppendLine();
                document.AppendLine($"*More in this topic: {string.Join(", ", topic.Topic.Details.Select(d => d.Title))}. " +
                                    $"Fetch with `get_topic(\"{topic.Topic.Topic}\")` and the same filter.*");
            }
        }

        if (resolution.NotCovered.Length > 0)
        {
            document.AppendLine();
            document.AppendLine("## Not covered");
            foreach (var gap in resolution.NotCovered)
            {
                document.AppendLine($"- {gap}");
            }
        }

        return document.ToString().TrimEnd();
    }

    public static string RenderTopic(Resolution resolution, string name)
    {
        var matches = resolution.Topics
            .Where(topic => topic.Topic.Topic.Equals(name, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var document = new StringBuilder();
        document.AppendLine($"# Topic: {name}");
        AppendEnvelope(document, resolution);

        if (IsInvalid(resolution))
        {
            return document.ToString().TrimEnd();
        }

        if (matches.Count == 0)
        {
            var available = resolution.Topics.Select(topic => topic.Topic.Topic).Distinct(StringComparer.OrdinalIgnoreCase).Order();
            document.AppendLine($"> **Not found:** no topic \"{name}\" under this scope. Topics available: {string.Join(", ", available)}.");
            return document.ToString().TrimEnd();
        }

        foreach (var topic in matches)
        {
            document.AppendLine();
            AppendTopic(document, topic);

            foreach (var detail in topic.Topic.Details)
            {
                var inherited = topic.InheritedDetails.Contains(detail.Title, StringComparer.OrdinalIgnoreCase) && topic.Replaces is not null
                    ? $" *(from the general topic, {topic.Replaces.Source} {Version(topic.Replaces.Version)})*"
                    : "";

                document.AppendLine();
                document.AppendLine($"### {detail.Title}{inherited}");
                document.AppendLine(detail.Markdown);
            }
        }

        return document.ToString().TrimEnd();
    }

    private static void AppendEnvelope(StringBuilder document, Resolution resolution)
    {
        if (resolution.Id is not null)
        {
            document.AppendLine($"> **Request:** {resolution.Id}");
        }

        document.AppendLine($"> **Scope:** {Scope(resolution.Filter)}");
        document.AppendLine($"> **Status:** {Status(resolution)}");
        document.AppendLine($"> **Sources:** {string.Join(", ", resolution.Sources.Select(Source))}");

        if (!Resolver.Requests(resolution.Filter, StandardFields.Partition))
        {
            document.AppendLine($"> ⚠ Missing required field: {StandardFields.Partition}. Standards are kept per company; " +
                                $"say which one: {string.Join(", ", resolution.ValidCompanies)}. Nothing is shown; add it and ask again.");
        }

        if (resolution.UnknownFields.Length > 0)
        {
            document.AppendLine($"> ⚠ Unknown field(s): {string.Join(", ", resolution.UnknownFields)}. " +
                                $"Valid fields: {string.Join(", ", resolution.ValidFields)}. Nothing is shown; fix the filter and ask again.");
        }

        foreach (var (field, values) in resolution.UnknownValues)
        {
            foreach (var (value, suggestions) in values)
            {
                var hint = suggestions.Length > 0 ? $" Did you mean {string.Join(" or ", suggestions)}?" : "";
                var consequence = IsOverlay(field)
                    ? " Only general standards are shown. Confirm the product before relying on this document."
                    : " Nothing matched it.";
                document.AppendLine($"> ⚠ Unknown value: {field} = \"{value}\".{hint}{consequence}");
            }
        }

        foreach (var source in resolution.Sources.Where(source => source.Status != "ok"))
        {
            document.AppendLine($"> ⚠ {source.Name} is unavailable ({source.Error}). Its standards are missing below; don't assume they don't exist.");
        }

        foreach (var conflict in resolution.Conflicts)
        {
            document.AppendLine($"> ⚠ Conflict: {conflict}.");
        }
    }

    private static void AppendTopic(StringBuilder document, ResolvedTopic topic)
    {
        var layer = topic.Product is null ? "general" : $"product: {string.Join(", ", topic.Product)}";
        var replaces = topic.Replaces is null ? "" : $" · replaces the general topic ({topic.Replaces.Source} {Version(topic.Replaces.Version)})";

        document.AppendLine($"## {topic.Topic.Topic}");
        document.AppendLine($"*{topic.Source} · {topic.Topic.Document} {Version(topic.Topic.Version)} · {layer}{replaces}*");
        document.AppendLine();
        document.AppendLine(topic.Topic.Body);
    }

    private static bool IsInvalid(Resolution resolution) =>
        resolution.UnknownFields.Length > 0 || !Resolver.Requests(resolution.Filter, StandardFields.Partition);

    private static string Status(Resolution resolution)
    {
        if (IsInvalid(resolution))
        {
            return "invalid";
        }

        var words = new List<string>();
        if (resolution.UnknownValues.Keys.Any(IsOverlay))
        {
            words.Add("unresolved");
        }

        if (resolution.Sources.Any(source => source.Status != "ok"))
        {
            words.Add("partial");
        }

        return words.Count > 0 ? string.Join(", ", words) : "complete";
    }

    private static string Scope(Dictionary<string, string[]> filter) =>
        filter.Count == 0
            ? "no filter (general standards only)"
            : string.Join(" · ", filter.Select(entry => $"{entry.Key} = {string.Join(", ", entry.Value)}"));

    private static string Source(SourceStatus source) => source.Status == "ok" ? $"{source.Name} ✓" : $"{source.Name} ✗";

    private static string Version(string version) => version.Length > 0 && char.IsDigit(version[0]) ? $"v{version}" : version;

    private static bool IsOverlay(string field) => field.Equals(StandardFields.Overlay, StringComparison.OrdinalIgnoreCase);
}
