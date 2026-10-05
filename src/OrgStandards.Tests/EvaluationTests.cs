using OrgStandards.Data;
using OrgStandards.Evaluation;

namespace OrgStandards.Tests;

// The retrieval evaluation: its metrics, and what the tag-only baseline can and can't find.
public class EvaluationTests
{
    private static readonly string Seed = Path.Combine(AppContext.BaseDirectory, "seed");
    private static readonly StandardsCorpus Corpus = StandardsCorpus.Load(Seed);
    private static readonly Taxonomy Taxonomy = Taxonomy.Load(Path.Combine(Seed, "taxonomy.yaml"));

    private static GoldenTask Task(string task, string[] expect, string[]? not = null, string[]? categories = null) =>
        new() { Task = task, Expect = expect, Not = not ?? [], Categories = categories ?? [] };

    private static Retrieval Returned(params string[] topics) =>
        new([], [], topics.Select(topic => new RetrievedTopic(topic, "design", null)).ToList());

    [Fact]
    public void Recall_and_precision_count_expected_topics()
    {
        var result = Evaluator.Score(Task("t", ["Colors", "Components"]), Returned("Colors", "Spacing", "Units", "Forms"), ranked: false);

        Assert.Equal(0.5, result.Recall);
        Assert.Equal(0.25, result.Precision);
        Assert.Equal(["Components"], result.Missed);
        Assert.Null(result.ReciprocalRank);
    }

    [Fact]
    public void Returning_nothing_has_no_precision_and_zero_recall()
    {
        var result = Evaluator.Score(Task("t", ["Colors"]), Returned(), ranked: false);

        Assert.Equal(0, result.Recall);
        Assert.Null(result.Precision);
    }

    [Fact]
    public void A_task_without_standards_is_right_to_return_nothing()
    {
        var results = new[]
        {
            Evaluator.Score(Task("none", []), Returned(), ranked: false),
            Evaluator.Score(Task("noisy", []), Returned("Colors"), ranked: false),
        };

        Assert.Equal(1, Evaluator.Summarize(results, ranked: false).NoStandardCorrect);
    }

    [Fact]
    public void Forbidden_topics_are_counted()
    {
        var result = Evaluator.Score(Task("t", ["Errors"], not: ["Error messages"]), Returned("Errors", "Error messages"), ranked: false);

        Assert.Equal(["Error messages"], result.Forbidden);
    }

    [Fact]
    public void Ranked_strategies_get_reciprocal_rank_and_recall_at_5()
    {
        var result = Evaluator.Score(Task("t", ["Retries"]), Returned("Timeouts", "Logging", "Retries"), ranked: true);

        Assert.Equal(1.0 / 3, result.ReciprocalRank);
        Assert.Equal(1.0, result.RecallAt5);
    }

    [Fact]
    public void Task_words_include_hyphenated_pairs() =>
        Assert.Contains("data-access", TagStrategy.Words("Review the data access layer"));

    [Fact]
    public void The_baseline_finds_a_task_that_names_its_category()
    {
        var retrieval = new TagStrategy(Taxonomy, acceptSuggestions: false)
            .Retrieve(Task("Our Sass files still use @import, clean them up", ["Sass modules"]), Corpus);

        Assert.Equal(["sass"], retrieval.Categories);
        Assert.Equal(["sass"], retrieval.Filter["kind"]);
        Assert.Contains(retrieval.Topics, topic => topic.Topic == "Sass modules");
    }

    [Fact]
    public void A_kind_also_retrieves_topics_tagged_with_its_ancestors()
    {
        // Spacing is tagged css, not sass; sass is broader-tagged css, so asking about sass reaches it.
        var retrieval = new TagStrategy(Taxonomy, acceptSuggestions: false)
            .Retrieve(Task("Our Sass files still use @import, clean them up", []), Corpus);

        Assert.Equal(["sass"], retrieval.Filter["kind"]);
        Assert.Contains(retrieval.Topics, topic => topic.Topic == "Spacing");
    }

    [Fact]
    public void Kind_and_concern_narrow_each_other()
    {
        // The end-to-end test that led to facets: "security" alone brings back browser security too.
        var retrieval = new TagStrategy(Taxonomy, acceptSuggestions: false)
            .Retrieve(Task("Security for the backend API", []), Corpus);

        Assert.Equal(["security"], retrieval.Filter["concern"]);
        Assert.Contains(retrieval.Topics, topic => topic.Topic == "Authorization");
        Assert.DoesNotContain(retrieval.Topics, topic => topic.Topic == "Content Security Policy");
    }

    [Fact]
    public void The_baseline_misses_retries_because_no_tag_says_retries()
    {
        // The worked example in REQ-RAG: the standard exists, but tag matching can't reach it.
        var task = Task("Add retries to the outbound call to the pricing service", ["Retries", "Timeouts"]);

        foreach (var strategy in new[] { new TagStrategy(Taxonomy, false), new TagStrategy(Taxonomy, true) })
        {
            var result = Evaluator.Score(task, strategy.Retrieve(task, Corpus), strategy.Ranked);
            Assert.Equal(0, result.Recall);
        }
    }

    [Fact]
    public void The_product_layer_replaces_the_general_topic()
    {
        var task = Task("caching for the catalog", ["Caching"]);
        task.Scope["product"] = ["xyz-public-app"];

        var retrieval = new TagStrategy(Taxonomy, acceptSuggestions: false).Retrieve(task, Corpus);

        var caching = Assert.Single(retrieval.Topics, topic => topic.Topic == "Caching");
        Assert.Equal(["xyz-public-app"], caching.Product ?? []);
    }
}
