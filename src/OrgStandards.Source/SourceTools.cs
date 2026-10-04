using System.ComponentModel;
using ModelContextProtocol.Server;
using OrgStandards.Contracts;
using OrgStandards.Data;

namespace OrgStandards.Source;

public sealed record SourceInfo(string Name, IReadOnlyDictionary<string, string> Links)
{
    // Replaces {{name}} with the address of a resource this owner points to.
    public StandardTopic Resolve(StandardTopic topic) => Links.Count == 0 ? topic : topic with
    {
        Body = Fill(topic.Body),
        Details = topic.Details.Select(detail => detail with { Markdown = Fill(detail.Markdown) }).ToArray(),
    };

    private string Fill(string text) =>
        Links.Aggregate(text, (current, link) => current.Replace($"{{{{{link.Key}}}}}", link.Value, StringComparison.OrdinalIgnoreCase));
}

// Called by the gateway, not by Claude directly.
[McpServerToolType]
public static class SourceTools
{
    [McpServerTool(Name = "describe_fields", ReadOnly = true, Idempotent = true)]
    [Description("Returns every field and value this source knows, plus the fields and values of the topics that match the filter.")]
    public static async Task<DescribeResult> DescribeFields(
        StandardsDbContext db,
        SourceInfo source,
        [Description("Optional filter: field -> accepted values. OR within a field, AND across fields.")]
        Dictionary<string, string[]>? filter = null,
        CancellationToken cancellationToken = default)
    {
        var cleaned = Filters.Clean(filter);
        var topics = await StandardMatcher.LoadAllAsync(db, cancellationToken);
        var matching = topics.Where(topic => StandardMatcher.Matches(topic.Tags, cleaned, discovery: true));

        return new DescribeResult(new SourceCatalog(source.Name, StandardMatcher.FieldsOf(topics)), StandardMatcher.FieldsOf(matching));
    }

    [McpServerTool(Name = "query_topics", ReadOnly = true, Idempotent = true)]
    [Description("Returns the topics (with their details) matching each filter, in the same order as the filters.")]
    public static async Task<QueryResult> QueryTopics(
        StandardsDbContext db,
        SourceInfo source,
        [Description("Filters: each is field -> accepted values. OR within a field, AND across fields.")]
        Dictionary<string, string[]>?[]? filters,
        CancellationToken cancellationToken = default)
    {
        // Callable directly, not only by the gateway: clean the input here too.
        var topics = await StandardMatcher.LoadAllAsync(db, cancellationToken);
        var results = (filters ?? [])
            .Select(filter => Filters.Clean(filter))
            .Select(filter => topics.Where(topic => StandardMatcher.Matches(topic.Tags, filter)).Select(source.Resolve).ToList())
            .ToArray();

        return new QueryResult(new SourceCatalog(source.Name, StandardMatcher.FieldsOf(topics)), results);
    }
}
