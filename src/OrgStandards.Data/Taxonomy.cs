using OrgStandards.Contracts;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace OrgStandards.Data;

// The taxonomy (seed/taxonomy.yaml): the categories standards are filed under. Owned by the
// architecture group; served by its owner's server (describe_taxonomy) and read by the harness.

public sealed class Category
{
    // Taxonomy.Kind, Concern, Runtime, Uses or Pattern: the field standards and filters use for this category.
    public string Facet { get; set; } = "";
    public string Description { get; set; } = "";
    public string[] Broader { get; set; } = [];
    public string? Source { get; set; }
    public string[] Files { get; set; } = [];
    public string[] Signals { get; set; } = [];
}

public sealed class Taxonomy
{
    // The facets, and the frontmatter and filter fields they're written in. Within a field, values
    // are alternatives; kind and concern narrow each other; runtime qualifies (see StandardFields).
    public const string Kind = "kind";
    public const string Concern = "concern";
    public const string Runtime = "runtime";
    public const string Uses = "uses";
    public const string Pattern = "pattern";
    public static readonly string[] Facets = [Kind, Concern, Runtime, Uses, Pattern];

    public Dictionary<string, Category> Categories { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public static Taxonomy Load(string path)
    {
        var taxonomy = SeedYaml.Read<Taxonomy>(path);
        taxonomy.Categories = new Dictionary<string, Category>(taxonomy.Categories, StringComparer.OrdinalIgnoreCase);
        return taxonomy;
    }

    // As served to Claude: every category, ordered by facet, then name.
    public TaxonomyCategory[] ToContract() =>
        Categories
            .OrderBy(entry => entry.Value.Facet, StringComparer.Ordinal)
            .ThenBy(entry => entry.Key, StringComparer.Ordinal)
            .Select(entry => new TaxonomyCategory(
                entry.Key, entry.Value.Facet, entry.Value.Description, entry.Value.Broader,
                entry.Value.Source, entry.Value.Files, entry.Value.Signals))
            .ToArray();
}

public static class SeedYaml
{
    private static readonly IDeserializer Yaml = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    public static T Read<T>(string path) => Yaml.Deserialize<T>(File.ReadAllText(path)) ??
        throw new InvalidOperationException($"{path} is empty.");
}
