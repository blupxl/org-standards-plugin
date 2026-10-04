using OrgStandards.Contracts;
using OrgStandards.Data;
using OrgStandards.Gateway;

namespace OrgStandards.Tests;

// Builders for the small, hand-made data the tests use.
internal static class TestData
{
    public static Dictionary<string, string[]> Map(params (string Field, string[] Values)[] entries) =>
        entries.ToDictionary(entry => entry.Field, entry => entry.Values, StringComparer.OrdinalIgnoreCase);

    public static StandardTopic Topic(
        string name,
        string version = "1.0",
        string body = "- MUST do the thing.",
        TopicDetail[]? details = null,
        string? product = null,
        string[]? technology = null,
        string[]? area = null,
        string[]? implements = null,
        string document = "Test document")
    {
        var tags = Map();
        if (product is not null) tags["product"] = [product];
        if (technology is not null) tags["technology"] = technology;
        if (area is not null) tags["area"] = area;
        if (implements is not null) tags["implements"] = implements;

        return new StandardTopic(name, document, version, body, details ?? [], tags);
    }

    // A source that answered one filter with these topics. Its catalog is every topic it holds,
    // which defaults to the answered ones.
    public static SourceCall<QueryResult> Answer(string source, StandardTopic[] answered, StandardTopic[]? catalog = null) =>
        new(source, new QueryResult(new SourceCatalog(source, StandardMatcher.FieldsOf(catalog ?? answered)), [answered.ToList()]), null);

    public static SourceCall<QueryResult> Unavailable(string source) =>
        new(source, default, "timed out after 10s");
}
