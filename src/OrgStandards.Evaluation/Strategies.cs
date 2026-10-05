using System.Text.RegularExpressions;
using OrgStandards.Contracts;
using OrgStandards.Data;
using OrgStandards.Gateway;

namespace OrgStandards.Evaluation;

public sealed record RetrievedTopic(string Topic, string Source, string[]? Product);

// What a strategy found for one task: how it classified the task, the filter it used, and the
// topics that came back. Ranked strategies return them best first.
public sealed record Retrieval(string[] Categories, Dictionary<string, string[]> Filter, IReadOnlyList<RetrievedTopic> Topics);

// One way of finding the standards for a task. Every strategy is measured with the same golden set.
public interface IRetrievalStrategy
{
    string Name { get; }
    string Description { get; }

    // Whether the order of Topics means anything (best first). Rank metrics are only reported for
    // ranked strategies.
    bool Ranked { get; }

    Retrieval Retrieve(GoldenTask task, StandardsCorpus corpus);
}

// The baseline: today's system, which matches tag values only. It can't read a task, so it stands
// in for what a model does today: take the task's words, keep the ones that are category values
// (optionally accepting the gateway's "did you mean" suggestions), and filter with them, each under
// its facet's field (kind or concern). With no matching words there is nothing to filter by, and
// nothing is returned.
public sealed partial class TagStrategy(Taxonomy taxonomy, bool acceptSuggestions) : IRetrievalStrategy
{
    private const string AnyCategory = "category";

    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "a", "an", "and", "are", "can", "does", "every", "for", "from", "how", "into", "is", "its",
        "make", "new", "our", "should", "still", "sure", "that", "the", "their", "them", "then", "this",
        "to", "use", "we", "what", "when", "where", "which", "with",
    };

    public string Name => acceptSuggestions ? "tags-suggest" : "tags-exact";

    public string Description => acceptSuggestions
        ? "Today's tag-only matching: task words that are category values, plus the gateway's first \"did you mean\" suggestion for the rest."
        : "Today's tag-only matching: task words (and two-word phrases) that are exactly category values.";

    public bool Ranked => false;

    public Retrieval Retrieve(GoldenTask task, StandardsCorpus corpus)
    {
        var known = taxonomy.Categories.Keys.ToArray();
        var words = Words(task.Task);

        var matched = words.Where(word => known.Contains(word, StringComparer.OrdinalIgnoreCase)).ToList();

        if (acceptSuggestions)
        {
            var candidates = words
                .Where(word => word.Length >= 4 && !StopWords.Contains(word) && !matched.Contains(word, StringComparer.OrdinalIgnoreCase))
                .ToArray();
            var unknown = Resolver.UnknownValues(new() { [AnyCategory] = candidates }, new() { [AnyCategory] = known });
            matched.AddRange(unknown.GetValueOrDefault(AnyCategory, [])
                .Select(entry => entry.Value.FirstOrDefault())
                .OfType<string>());
        }

        var categories = matched.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var filter = new Dictionary<string, string[]>(task.Scope, StringComparer.OrdinalIgnoreCase);
        if (categories.Length == 0)
        {
            return new Retrieval([], filter, []);
        }

        foreach (var facet in categories.GroupBy(category => taxonomy.Categories[category].Facet))
        {
            filter[facet.Key] = facet.ToArray();
        }

        // The request is reported as made; matching uses it with kinds widened to their ancestors.
        var expanded = Ancestry.Expand(filter, taxonomy.ToContract());
        var matches = corpus.Topics
            .Where(owned => StandardMatcher.Matches(owned.Topic.Tags, expanded))
            .Select(owned => (owned.Source, owned.Topic));

        var (resolved, _) = Resolver.Overlay(matches);
        return new Retrieval(
            categories,
            filter,
            resolved.Select(topic => new RetrievedTopic(topic.Topic.Topic, topic.Source, topic.Product)).ToList());
    }

    // Lower-case words, plus each pair of neighbors joined with a hyphen ("data access" →
    // "data-access"), since multi-word categories are written that way.
    public static string[] Words(string text)
    {
        var words = WordPattern().Matches(text.ToLowerInvariant()).Select(match => match.Value).ToArray();
        var pairs = words.Zip(words.Skip(1), (first, second) => $"{first}-{second}");
        return words.Concat(pairs).Distinct().ToArray();
    }

    [GeneratedRegex("[a-z0-9]+(?:-[a-z0-9]+)*")]
    private static partial Regex WordPattern();
}
