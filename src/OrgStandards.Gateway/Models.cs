using System.ComponentModel;
using OrgStandards.Contracts;

namespace OrgStandards.Gateway;

public sealed record StandardsRequest(
    [property: Description("Optional id, echoed back so documents can be matched to requests.")] string? Id,
    [property: Description("Field -> accepted values, e.g. { \"product\": [\"xyz-public-app\"], \"kind\": [\"api\"], \"concern\": [\"resilience\"] }.")]
    Dictionary<string, string[]> Filter,
    [property: Description("Optional. Only the exclusions in the project's .claude/standards.json: field -> values, plus \"topic\" -> topic names, " +
                           "e.g. { \"concern\": [\"branding\"] }. Matching topics are left out and listed as excluded. Never add exclusions of your own.")]
    Dictionary<string, string[]>? Exclude = null);

public sealed record SourceStatus(string Name, string Status, string? Error);

public sealed record ListResult(
    string Status,
    Dictionary<string, string[]> Fields,
    SourceStatus[] Sources,
    string[]? UnknownFields = null,
    string[]? ValidFields = null);

public sealed record Replaced(string Source, string Version);

// A topic after the overlay. Product is set for a product-specific topic, null for a general one.
// InheritedDetails names the details that came from the general topic it replaced.
public sealed record ResolvedTopic(
    StandardTopic Topic, string Source, string[]? Product, Replaced? Replaces, string[] InheritedDetails);

// Everything the document for one request is rendered from.
public sealed record Resolution(
    string? Id,
    Dictionary<string, string[]> Filter,
    SourceStatus[] Sources,
    ResolvedTopic[] Topics,
    string[] Conflicts,
    string[] UnknownFields,
    string[] ValidFields,
    Dictionary<string, Dictionary<string, string[]>> UnknownValues,
    string[] NotCovered,
    ExcludedTopic[] Excluded);

// A topic left out by the project's exclusions. Reason says which exclusion matched it.
public sealed record ExcludedTopic(string Topic, string Source, string Reason);
