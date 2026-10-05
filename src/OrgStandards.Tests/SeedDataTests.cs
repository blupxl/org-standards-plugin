using OrgStandards.Data;
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

    // (facet, category) for every category a standard uses. A recipe's adds are dependencies too.
    private static (string Facet, string Category)[] Used =>
        Corpus.Topics
            .SelectMany(topic => Taxonomy.Facets.SelectMany(facet => topic.Topic.Tags.GetValueOrDefault(facet, []).Select(category => (facet, category)))
                .Concat(topic.Topic.Tags.GetValueOrDefault("adds", []).Select(category => (Taxonomy.Uses, category))))
            .Distinct()
            .ToArray();

    [Fact]
    public void Every_standard_has_a_kind() =>
        Assert.DoesNotContain(Corpus.Topics, topic => topic.Topic.Tags.GetValueOrDefault(Taxonomy.Kind, []).Length == 0);

    [Fact]
    public void Every_category_a_standard_uses_is_in_the_taxonomy_under_its_facet() =>
        Assert.DoesNotContain(Used, used =>
            !Taxonomy.Categories.TryGetValue(used.Category, out var category) || category.Facet != used.Facet);

    [Fact]
    public void Every_category_in_the_taxonomy_is_used_by_a_standard() =>
        Assert.Empty(Taxonomy.Categories.Keys.Except(Used.Select(used => used.Category), StringComparer.OrdinalIgnoreCase));

    [Fact]
    public void Every_category_has_a_facet_and_a_description() =>
        Assert.DoesNotContain(Taxonomy.Categories.Values, category =>
            !Taxonomy.Facets.Contains(category.Facet) || string.IsNullOrWhiteSpace(category.Description));

    [Fact]
    public void Broader_categories_exist_in_the_same_facet() =>
        Assert.DoesNotContain(
            Taxonomy.Categories.SelectMany(entry => entry.Value.Broader.Select(broader => (Name: entry.Key, entry.Value.Facet, Broader: broader))),
            pair => !Taxonomy.Categories.TryGetValue(pair.Broader, out var broader) || broader.Facet != pair.Facet);

    [Fact]
    public void The_ancestry_has_no_cycles() =>
        Assert.Empty(Cycles(Taxonomy.Categories));

    [Fact]
    public void The_cycle_finder_reports_the_path_of_a_cycle()
    {
        var cyclic = new Dictionary<string, Category>(StringComparer.OrdinalIgnoreCase)
        {
            ["sass"] = new() { Facet = "kind", Broader = ["css"] },
            ["css"] = new() { Facet = "kind", Broader = ["sass"] },
        };

        Assert.Contains("sass -> css -> sass", Cycles(cyclic));
    }

    // Every path that leads back to a category it already passed through, such as "sass -> css -> sass".
    private static IEnumerable<string> Cycles(IReadOnlyDictionary<string, Category> categories)
    {
        var cycles = new List<string>();
        foreach (var start in categories.Keys)
        {
            var pending = new Stack<(string Name, List<string> Path)>();
            pending.Push((start, [start]));
            while (pending.TryPop(out var current))
            {
                if (!categories.TryGetValue(current.Name, out var category))
                {
                    continue;
                }

                foreach (var broader in category.Broader)
                {
                    if (current.Path.Contains(broader, StringComparer.OrdinalIgnoreCase))
                    {
                        cycles.Add(string.Join(" -> ", current.Path.Append(broader)));
                        continue;
                    }

                    pending.Push((broader, [.. current.Path, broader]));
                }
            }
        }

        return cycles;
    }

    [Fact]
    public void Only_kind_and_concern_have_ancestry() =>
        Assert.DoesNotContain(Taxonomy.Categories.Values, category =>
            category.Facet is not (Taxonomy.Kind or Taxonomy.Concern) && category.Broader.Length > 0);

    [Fact]
    public void Topics_with_a_narrower_concern_also_carry_its_ancestors()
    {
        string[] Ancestors(string name)
        {
            var found = new List<string>();
            var pending = new Queue<string>([name]);
            while (pending.TryDequeue(out var current))
            {
                if (!Taxonomy.Categories.TryGetValue(current, out var category))
                {
                    continue;
                }

                foreach (var broader in category.Broader.Where(broader => !found.Contains(broader, StringComparer.OrdinalIgnoreCase)))
                {
                    found.Add(broader);
                    pending.Enqueue(broader);
                }
            }

            return [.. found];
        }

        var gaps = Corpus.Topics.SelectMany(topic =>
        {
            var concerns = topic.Topic.Tags.GetValueOrDefault(Taxonomy.Concern, []);
            return concerns.SelectMany(concern => Ancestors(concern)
                    .Where(ancestor => !concerns.Contains(ancestor, StringComparer.OrdinalIgnoreCase))
                    .Select(ancestor => $"{topic.Topic.Topic}: concern {concern} lacks {ancestor}"));
        }).ToList();

        Assert.True(gaps.Count == 0, string.Join("; ", gaps));
    }

    [Fact]
    public void No_two_owners_define_the_same_topic_in_the_same_layer()
    {
        var (_, conflicts) = Resolver.Overlay(Corpus.Topics
            .Where(topic => !topic.Topic.Tags.ContainsKey("product"))
            .Select(topic => (topic.Source, topic.Topic)));

        Assert.Empty(conflicts);
    }

    [Fact]
    public void Golden_set_topics_exist()
    {
        var topics = Corpus.Topics.Select(topic => topic.Topic.Topic).ToHashSet(StringComparer.OrdinalIgnoreCase);
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

        var forNode = Corpus.Topics.Where(topic => Reaches(topic, "node")).ToList();
        Assert.DoesNotContain(forNode, topic => topic.Topic.Tags.GetValueOrDefault("runtime", []).Contains("dotnet"));
        Assert.Contains(forNode, topic => topic.Topic.Topic == "Tests in Node");
        Assert.Contains(forNode, topic => topic.Topic.Topic == "Timeouts");   // general rules reach every runtime
    }

    [Fact]
    public void Implements_links_name_general_topics_that_exist()
    {
        var general = Corpus.Topics.Where(topic => !topic.Topic.Tags.ContainsKey("runtime")).Select(topic => topic.Topic.Topic)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var links = Corpus.Topics.SelectMany(topic => topic.Topic.Tags.GetValueOrDefault("implements", [])).ToList();

        Assert.NotEmpty(links);
        Assert.DoesNotContain(links, link => !general.Contains(link));
    }

    [Fact]
    public void Recipes_stay_out_of_the_rules_and_offer_a_recommended_option()
    {
        bool Matches(OwnedTopic owned, Dictionary<string, string[]> filter) => OrgStandards.Data.StandardMatcher.Matches(owned.Topic.Tags, filter);
        var rules = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            { ["kind"] = ["data-access"], ["runtime"] = ["dotnet"], ["uses"] = ["postgres", "ef-core"] };
        var recipes = new Dictionary<string, string[]>(rules, StringComparer.OrdinalIgnoreCase) { ["template"] = ["recipe"] };

        Assert.DoesNotContain(Corpus.Topics.Where(topic => Matches(topic, rules)), topic => topic.Topic.Tags.ContainsKey("template"));
        var options = Corpus.Topics.Where(topic => Matches(topic, recipes) && topic.Topic.Tags.ContainsKey("template")).ToList();
        Assert.Contains(options, topic => topic.Topic.Topic == "Recipe: EF Core with PostgreSQL" && topic.Topic.Tags.ContainsKey("recommended"));
        Assert.Contains(options, topic => topic.Topic.Topic == "Recipe: Dapper with PostgreSQL");
        Assert.DoesNotContain(options, topic => topic.Topic.Topic == "Recipe: EF Core with SQL Server");
    }

    [Fact]
    public void The_seed_has_four_owners() =>
        Assert.Equal(["data", "design", "platform", "security"], Corpus.Owners);

    [Fact]
    public void A_dependencys_standards_reach_only_components_that_use_it()
    {
        bool Reaches(OwnedTopic owned, string[] uses) => OrgStandards.Data.StandardMatcher.Matches(owned.Topic.Tags,
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase) { ["kind"] = ["data-access", "backend"], ["uses"] = uses });

        var withPostgres = Corpus.Topics.Where(topic => Reaches(topic, ["postgres"])).Select(topic => topic.Topic.Topic).ToList();
        Assert.Contains("Postgres naming", withPostgres);
        Assert.Contains("Queries", withPostgres);                    // general rules still apply
        Assert.DoesNotContain("SQL Server schemas", withPostgres);
        Assert.DoesNotContain("MongoDB indexes", withPostgres);
    }
}
