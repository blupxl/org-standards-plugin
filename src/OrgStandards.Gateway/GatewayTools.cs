using System.ComponentModel;
using ModelContextProtocol.Server;
using OrgStandards.Contracts;

namespace OrgStandards.Gateway;

// The tools Claude sees. Descriptions are part of the model's context: keep them accurate.
[McpServerToolType]
public static class GatewayTools
{
    private const string FilterRules =
        "A filter is a JSON object of field -> accepted values. OR within a field, AND across fields. " +
        "Fields and values are not fixed: discover them with list_standards. Unknown fields are rejected with the valid fields listed.";

    [McpServerTool(Name = "list_standards", ReadOnly = true, Idempotent = true)]
    [Description("Discover which standards exist. Returns each field (for example product, kind, concern) with the values " +
                 "available under the optional filter. Use the returned values as the filter for get_standards, or narrow " +
                 "further by calling list_standards again with a filter. " + FilterRules)]
    public static async Task<ListResult> ListStandards(
        SourceClient sources,
        [Description("Optional filter, e.g. { \"kind\": [\"api\"] }.")] Dictionary<string, string[]>? filter = null,
        CancellationToken cancellationToken = default)
    {
        filter = Filters.Clean(filter);
        var calls = await sources.CallAllAsync<DescribeResult>(
            "describe_fields", new Dictionary<string, object?> { ["filter"] = filter }, cancellationToken);

        var available = calls.Where(call => call.Succeeded && call.Value is not null).Select(call => call.Value!).ToList();
        var catalog = Resolver.MergeFields(available.Select(result => result.Catalog.Fields));
        var statuses = calls.Select(call => new SourceStatus(call.Source, call.Succeeded ? "ok" : "unavailable", call.Error)).ToArray();

        if (available.Count == 0)
        {
            return new ListResult("unavailable", [], statuses);
        }

        var unknownFields = Resolver.UnknownFields(filter, catalog);
        if (unknownFields.Length > 0)
        {
            return new ListResult("invalid", catalog, statuses, unknownFields, catalog.Keys.Order().ToArray());
        }

        return new ListResult(
            calls.All(call => call.Succeeded) ? "ok" : "partial",
            Resolver.MergeFields(available.Select(result => result.Matching)),
            statuses);
    }

    [McpServerTool(Name = "get_standards", ReadOnly = true, Idempotent = true)]
    [Description("Returns the standards an implementer must follow, as one Markdown document per request (several requests " +
                 "in one call). Read each document's header first: it says whether the document is complete, which sources " +
                 "answered, and what is missing. Rules use MUST / MUST NOT / SHOULD. \"product\" is a specific filter: general " +
                 "topics come back as the base, and a product topic with the same name replaces the general one. Without " +
                 "\"product\", only general topics come back. Topics with examples or reference material (such as color " +
                 "tokens or component lists) say so and name the get_topic call that fetches it. " + FilterRules)]
    public static async Task<string> GetStandards(
        SourceClient sources,
        [Description("The requests to resolve, e.g. [{ \"id\": \"api\", \"filter\": { \"product\": [\"xyz-public-app\"], \"kind\": [\"api\"], \"concern\": [\"resilience\"] } }].")]
        StandardsRequest?[]? requests,
        CancellationToken cancellationToken = default)
    {
        // Clean every request first: no nulls go past this point.
        var cleaned = (requests ?? [])
            .OfType<StandardsRequest>()
            .Select(request => request with { Filter = Filters.Clean(request.Filter), Exclude = Filters.Clean(request.Exclude) })
            .ToArray();
        if (cleaned.Length == 0)
        {
            return "# Standards\n> **Status:** invalid\n> ⚠ No requests were given. Pass at least one request, " +
                   "for example [{ \"filter\": { \"kind\": [\"api\"] } }].";
        }

        // One call per source for the whole batch.
        var calls = await sources.CallAllAsync<QueryResult>(
            "query_topics",
            new Dictionary<string, object?> { ["filters"] = cleaned.Select(request => request.Filter).ToArray() },
            cancellationToken);

        var documents = cleaned.Select((request, index) =>
            StandardsDocument.Render(Resolver.Resolve(request.Id, request.Filter, index, calls, request.Exclude)));

        return string.Join("\n\n---\n\n", documents);
    }

    [McpServerTool(Name = "get_topic", ReadOnly = true, Idempotent = true)]
    [Description("Returns one topic in full, including its details: examples, code, and reference material such as color " +
                 "tokens or component lists. Call it when a get_standards topic says more is available and your task needs " +
                 "it, for example before writing UI code that needs the actual colors. Pass the same filter you used for " +
                 "get_standards so the product's version of the topic is returned. " + FilterRules)]
    public static async Task<string> GetTopic(
        SourceClient sources,
        [Description("The topic name, as shown in a get_standards heading, e.g. \"Colors\".")] string? topic,
        [Description("The filter used for get_standards, e.g. { \"product\": [\"xyz-public-app\"], \"kind\": [\"css\"] }.")]
        Dictionary<string, string[]>? filter = null,
        [Description("Optional: the exclusions used for get_standards (only those in the project's .claude/standards.json).")]
        Dictionary<string, string[]>? exclude = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(topic))
        {
            return "# Topic\n> **Status:** invalid\n> ⚠ No topic name was given. Use a name from a get_standards heading, for example \"Colors\".";
        }

        filter = Filters.Clean(filter);
        var calls = await sources.CallAllAsync<QueryResult>(
            "query_topics", new Dictionary<string, object?> { ["filters"] = new[] { filter } }, cancellationToken);

        return StandardsDocument.RenderTopic(Resolver.Resolve(null, filter, 0, calls, exclude), topic.Trim());
    }
}
