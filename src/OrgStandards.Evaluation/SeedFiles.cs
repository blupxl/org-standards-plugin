using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace OrgStandards.Evaluation;

// The taxonomy (taxonomy.yaml) and the golden set (evals/golden.yaml), kept with the standards.

public sealed class Category
{
    // Taxonomy.Kind, Concern or Runtime: the field standards and filters use for this category.
    public string Facet { get; set; } = "";
    public string Description { get; set; } = "";
    public string[] Broader { get; set; } = [];
    public string? Source { get; set; }
    public string[] Files { get; set; } = [];
}

public sealed class Taxonomy
{
    // The facets, and the frontmatter and filter fields they're written in. Within a field, values
    // are alternatives; kind and concern narrow each other; runtime qualifies (see StandardFields).
    public const string Kind = "kind";
    public const string Concern = "concern";
    public const string Runtime = "runtime";
    public static readonly string[] Facets = [Kind, Concern, Runtime];

    public Dictionary<string, Category> Categories { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public static Taxonomy Load(string path)
    {
        var taxonomy = SeedYaml.Read<Taxonomy>(path);
        taxonomy.Categories = new Dictionary<string, Category>(taxonomy.Categories, StringComparer.OrdinalIgnoreCase);
        return taxonomy;
    }
}

public sealed class GoldenTask
{
    public string Task { get; set; } = "";
    public Dictionary<string, string[]> Scope { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public string[] Categories { get; set; } = [];
    public string[] Expect { get; set; } = [];
    public string[] Not { get; set; } = [];
}

public sealed class GoldenSet
{
    public List<GoldenTask> Tasks { get; set; } = [];

    // A short hash of the file, so reports say which version of the golden set they used.
    public string Fingerprint { get; private set; } = "";

    public static GoldenSet Load(string path)
    {
        var golden = SeedYaml.Read<GoldenSet>(path);
        golden.Fingerprint = StandardsCorpus.Hash([File.ReadAllText(path).Replace("\r\n", "\n")]);
        foreach (var task in golden.Tasks)
        {
            task.Scope = new Dictionary<string, string[]>(task.Scope, StringComparer.OrdinalIgnoreCase);
        }

        return golden;
    }
}

internal static class SeedYaml
{
    private static readonly IDeserializer Yaml = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    public static T Read<T>(string path) => Yaml.Deserialize<T>(File.ReadAllText(path)) ??
        throw new InvalidOperationException($"{path} is empty.");
}
