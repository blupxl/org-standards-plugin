using OrgStandards.Evaluation;
using OrgStandards.Gateway;

namespace OrgStandards.Tests;

// The seed standards, the taxonomy and the golden set agree with each other. The taxonomy grows
// over time; these keep every category used, every used category defined in the right facet, and
// the golden set pointing at standards that exist.
public class SeedDataTests
{
    private static readonly string Seed = Path.Combine(AppContext.BaseDirectory, "seed");
    private static readonly StandardsCorpus Corpus = StandardsCorpus.Load(Seed);
    private static readonly Taxonomy Taxonomy = Taxonomy.Load(Path.Combine(Seed, "taxonomy.yaml"));
    private static readonly GoldenSet Golden = GoldenSet.Load(Path.Combine(Seed, "evals", "golden.yaml"));

    // (facet, category) for every category a standard uses.
    private static (string Facet, string Category)[] Used =>
        Corpus.Topics
            .SelectMany(t => Taxonomy.Facets.SelectMany(facet => t.Topic.Tags.GetValueOrDefault(facet, []).Select(category => (facet, category))))
            .Distinct()
            .ToArray();

    [Fact]
    public void Every_standard_has_a_kind() =>
        Assert.DoesNotContain(Corpus.Topics, t => t.Topic.Tags.GetValueOrDefault(Taxonomy.Kind, []).Length == 0);

    [Fact]
    public void Every_category_a_standard_uses_is_in_the_taxonomy_under_its_facet() =>
        Assert.DoesNotContain(Used, used =>
            !Taxonomy.Categories.TryGetValue(used.Category, out var category) || category.Facet != used.Facet);

    [Fact]
    public void Every_category_in_the_taxonomy_is_used_by_a_standard() =>
        Assert.Empty(Taxonomy.Categories.Keys.Except(Used.Select(used => used.Category), StringComparer.OrdinalIgnoreCase));

    [Fact]
    public void Every_category_has_a_facet_and_a_description() =>
        Assert.DoesNotContain(Taxonomy.Categories.Values, c =>
            !Taxonomy.Facets.Contains(c.Facet) || string.IsNullOrWhiteSpace(c.Description));

    [Fact]
    public void Broader_categories_exist_in_the_same_facet() =>
        Assert.DoesNotContain(Taxonomy.Categories.Values.SelectMany(c => c.Broader.Select(broader => (c.Facet, broader))), pair =>
            !Taxonomy.Categories.TryGetValue(pair.broader, out var broader) || broader.Facet != pair.Facet);

    [Fact]
    public void No_two_owners_define_the_same_topic_in_the_same_layer()
    {
        var (_, conflicts) = Resolver.Overlay(Corpus.Topics
            .Where(t => !t.Topic.Tags.ContainsKey("product"))
            .Select(t => (t.Source, t.Topic)));

        Assert.Empty(conflicts);
    }

    [Fact]
    public void Golden_set_topics_exist()
    {
        var topics = Corpus.Topics.Select(t => t.Topic.Topic).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain(Golden.Tasks.SelectMany(task => task.Expect.Concat(task.Not)), topic => !topics.Contains(topic));
    }

    [Fact]
    public void Golden_set_categories_and_products_exist()
    {
        var products = Corpus.Fields.GetValueOrDefault("product", []);
        Assert.DoesNotContain(Golden.Tasks.SelectMany(task => task.Categories), category => !Taxonomy.Categories.ContainsKey(category));
        Assert.DoesNotContain(Golden.Tasks.SelectMany(task => task.Scope.GetValueOrDefault("product", [])),
            product => !products.Contains(product, StringComparer.OrdinalIgnoreCase));
    }

    [Fact]
    public void A_runtimes_standards_reach_only_that_runtime()
    {
        bool Reaches(OwnedTopic owned, string runtime) => OrgStandards.Data.StandardMatcher.Matches(owned.Topic.Tags,
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase) { ["kind"] = ["backend", "api", "testing"], ["runtime"] = [runtime] });

        var forNode = Corpus.Topics.Where(t => Reaches(t, "node")).ToList();
        Assert.DoesNotContain(forNode, t => t.Topic.Tags.GetValueOrDefault("runtime", []).Contains("dotnet"));
        Assert.Contains(forNode, t => t.Topic.Topic == "Tests in Node");
        Assert.Contains(forNode, t => t.Topic.Topic == "Timeouts");   // general rules reach every runtime
    }

    [Fact]
    public void Implements_links_name_general_topics_that_exist()
    {
        var general = Corpus.Topics.Where(t => !t.Topic.Tags.ContainsKey("runtime")).Select(t => t.Topic.Topic)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var links = Corpus.Topics.SelectMany(t => t.Topic.Tags.GetValueOrDefault("implements", [])).ToList();

        Assert.NotEmpty(links);
        Assert.DoesNotContain(links, link => !general.Contains(link));
    }

    [Fact]
    public void The_seed_has_three_owners() =>
        Assert.Equal(["design", "platform", "security"], Corpus.Owners);
}
