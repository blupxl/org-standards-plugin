using OrgStandards.Data;
using OrgStandards.Evaluation;

namespace OrgStandards.Tests;

// The classifier, scored like every other strategy: categories against the golden set's, and the
// topics its filter retrieves.
public class ClassifierStrategyTests
{
    private static readonly StandardsCorpus Corpus = StandardsCorpus.Load(Path.Combine(AppContext.BaseDirectory, "seed"));

    private static readonly Taxonomy Taxonomy = Taxonomy.Load(Path.Combine(AppContext.BaseDirectory, "seed", "taxonomy.yaml"));

    private sealed class FixedClassifier(Classification classification) : IClassifier
    {
        public Classification Classify(string task, IReadOnlyDictionary<string, string[]> declaredScope) => classification;
    }

    private static GoldenTask CacheTask(Dictionary<string, string[]>? scope = null) =>
        new() { Task = "Cache customer lookups in Redis", Scope = scope ?? new(StringComparer.OrdinalIgnoreCase) };

    [Fact]
    public void Its_filter_retrieves_the_topics_and_its_categories_are_reported()
    {
        var strategy = new ClassifierStrategy(new FixedClassifier(new(
            [new("task", ["backend", "redis", "caching"], new() { ["kind"] = ["backend"], ["uses"] = ["redis"] })])), Taxonomy);

        var retrieval = strategy.Retrieve(CacheTask(), Corpus);

        Assert.Equal(["backend", "redis", "caching"], retrieval.Categories);
        Assert.Contains(retrieval.Topics, topic => topic.Topic == "Redis usage");
    }

    [Fact]
    public void The_declared_scope_is_always_in_the_filter_beside_the_classifier_values()
    {
        var strategy = new ClassifierStrategy(new FixedClassifier(new([new("task", ["api"], new() { ["kind"] = ["api"], ["runtime"] = ["node"] })])), Taxonomy);

        var retrieval = strategy.Retrieve(CacheTask(new(StringComparer.OrdinalIgnoreCase) { ["runtime"] = ["dotnet"], ["uses"] = ["redis"] }), Corpus);

        Assert.Contains("dotnet", retrieval.Filter["runtime"]);
        Assert.Contains("node", retrieval.Filter["runtime"]);
        Assert.Equal(["redis"], retrieval.Filter["uses"]);
        Assert.Equal(["api"], retrieval.Filter["kind"]);
    }

    [Fact]
    public void Each_component_is_retrieved_with_its_own_filter_and_the_results_are_combined()
    {
        var strategy = new ClassifierStrategy(new FixedClassifier(new([
            new("orders-api", ["backend", "redis"], new() { ["uses"] = ["redis"] }),
            new("billing-api", ["backend", "postgres"], new() { ["uses"] = ["postgres"] }),
        ])), Taxonomy);

        var retrieval = strategy.Retrieve(CacheTask(), Corpus);

        Assert.Contains(retrieval.Topics, topic => topic.Topic == "Redis usage");
        Assert.Contains(retrieval.Topics, topic => topic.Topic == "Postgres naming");
        Assert.Equal(retrieval.Topics.Count, retrieval.Topics.Select(topic => (topic.Topic, topic.Source)).Distinct().Count());
        Assert.Equal(["backend", "redis", "postgres"], retrieval.Categories);
    }

    [Theory]
    [InlineData("plugins/my-company", "my-company")]
    [InlineData("plugins/my-company/", "my-company")]
    [InlineData("plugins/acme_standards", "acme_standards")]
    [InlineData("plugins/my company.v2", "my_company_v2")]
    public void The_allowed_tools_follow_the_plugin_folder_name(string pluginDirectory, string pluginName)
    {
        Assert.Equal(
            $"Agent,mcp__plugin_{pluginName}_standards__get_taxonomy,mcp__plugin_{pluginName}_standards__list_standards",
            ClaudeClassifier.AllowedTools(pluginDirectory));
    }

    [Fact]
    public void Nothing_classified_retrieves_nothing()
    {
        var strategy = new ClassifierStrategy(new FixedClassifier(new([])), Taxonomy);

        Assert.Empty(strategy.Retrieve(CacheTask(), Corpus).Topics);
    }

    [Fact]
    public void Claude_output_is_parsed_into_one_classification_per_component()
    {
        const string output = """
            {"type":"result","result":"```json\n{\"status\":\"ok\",\"product\":null,\"components\":[{\"name\":\"task\",\"path\":\"\",\"declared\":{},\"inferred\":{\"kind\":[\"backend\"],\"uses\":[\"redis\"],\"concern\":[\"caching\"]},\"evidence\":{},\"differences\":[],\"filter\":{\"kind\":[\"backend\"],\"uses\":[\"redis\"]}}],\"questions\":[]}\n```\nRedis cache work."}
            """;

        var classification = ClaudeClassifier.Parse(output);

        var component = Assert.Single(classification.Components);
        Assert.Equal("task", component.Name);
        Assert.Equal(["backend", "redis", "caching"], classification.Categories);
        Assert.Equal(["redis"], component.Filter["uses"]);
    }

    [Fact]
    public void Claude_output_without_a_result_is_nothing_classified()
    {
        var classification = ClaudeClassifier.Parse("""{"type":"result","result":"I couldn't reach the standards server."}""");

        Assert.Empty(classification.Components);
    }

    [Fact]
    public void Claude_output_with_malformed_json_is_nothing_classified()
    {
        var classification = ClaudeClassifier.Parse("""{"type":"result","result":"{ not json }"}""");

        Assert.Empty(classification.Components);
    }

    [Fact]
    public void Empty_claude_output_is_nothing_classified()
    {
        var classification = ClaudeClassifier.Parse("");

        Assert.Empty(classification.Components);
    }

    [Fact]
    public void Claude_output_of_the_wrong_shape_is_read_as_far_as_it_makes_sense()
    {
        Assert.Empty(ClaudeClassifier.Parse("""{"result":"{\"components\":{}}"}""").Components);

        var classification = ClaudeClassifier.Parse(
            """{"result":"{\"components\":[1,{\"inferred\":{\"uses\":[\"redis\",1]},\"filter\":{\"Language\":[\"a\"],\"language\":[\"b\"]}}]}"}""");

        Assert.Equal(["redis"], classification.Categories);
        Assert.Equal(["b"], Assert.Single(classification.Components).Filter["language"]);
    }
}
