using System.Text.Json;

namespace OrgStandards.Contracts;

// A filter is field -> accepted values. OR within a field, AND across fields.
// Example: { "technology": ["web-api"], "area": ["branding", "ui"] } means web-api AND (branding OR ui).

public static class StandardFields
{
    // The field that layers specific topics over general ones.
    // With it in the filter, general topics (no product tag) come back as the base layer and a
    // product topic with the same name replaces them. Without it, only general topics come back.
    public const string Overlay = "product";

    // The field that partitions standards by company. Every topic belongs to a company, every
    // request must name one, and companies never mix. (Products layer within a company.)
    public const string Partition = "company";
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
