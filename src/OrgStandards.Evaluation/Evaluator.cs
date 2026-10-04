namespace OrgStandards.Evaluation;

public sealed record TaskResult(
    string Task,
    Dictionary<string, string[]> Scope,
    string[] Expected,
    string[] Returned,
    string[] Found,
    string[] Missed,
    string[] Forbidden,
    double? Recall,
    double? Precision,
    double? ReciprocalRank,
    double? RecallAt5,
    string[] ExpectedCategories,
    string[] Categories,
    double? CategoryRecall,
    double? CategoryPrecision);

// Retrieval quality over the golden set (REQ-EVAL L1). Averages are over the tasks a metric
// applies to, and each says how many that was.
public sealed record Summary(
    int Tasks,
    int TasksWithStandards,
    int TasksWithoutStandards,
    double MeanRecall,
    double FoundAllRate,
    double FoundAnyRate,
    int ReturnedNothing,
    double? MeanPrecision,
    double MeanReturned,
    int ForbiddenReturned,
    int NoStandardCorrect,
    double? MeanReciprocalRank,
    double? MeanRecallAt5,
    double MeanCategoryRecall,
    double? MeanCategoryPrecision);

public static class Evaluator
{
    public const int K = 5;

    public static TaskResult Score(GoldenTask task, Retrieval retrieval, bool ranked)
    {
        var comparer = StringComparer.OrdinalIgnoreCase;
        var returned = retrieval.Topics.Select(topic => topic.Topic).Distinct(comparer).ToArray();
        var found = task.Expect.Where(expected => returned.Contains(expected, comparer)).ToArray();
        var missed = task.Expect.Except(found, comparer).ToArray();
        var forbidden = task.Not.Where(topic => returned.Contains(topic, comparer)).ToArray();

        double? recall = task.Expect.Length == 0 ? null : (double)found.Length / task.Expect.Length;
        double? precision = returned.Length == 0 ? null : (double)returned.Count(topic => task.Expect.Contains(topic, comparer)) / returned.Length;

        double? reciprocalRank = null, recallAt5 = null;
        if (ranked && task.Expect.Length > 0)
        {
            var first = Array.FindIndex(returned, topic => task.Expect.Contains(topic, comparer));
            reciprocalRank = first < 0 ? 0 : 1.0 / (first + 1);
            recallAt5 = (double)returned.Take(K).Count(topic => task.Expect.Contains(topic, comparer)) / task.Expect.Length;
        }

        var categoriesFound = task.Categories.Count(category => retrieval.Categories.Contains(category, comparer));
        double? categoryRecall = task.Categories.Length == 0 ? null : (double)categoriesFound / task.Categories.Length;
        double? categoryPrecision = retrieval.Categories.Length == 0
            ? null
            : (double)retrieval.Categories.Count(category => task.Categories.Contains(category, comparer)) / retrieval.Categories.Length;

        return new TaskResult(task.Task, task.Scope, task.Expect, returned, found, missed, forbidden,
            recall, precision, reciprocalRank, recallAt5, task.Categories, retrieval.Categories, categoryRecall, categoryPrecision);
    }

    public static Summary Summarize(IReadOnlyList<TaskResult> results, bool ranked)
    {
        var withStandards = results.Where(result => result.Expected.Length > 0).ToList();
        var withoutStandards = results.Where(result => result.Expected.Length == 0).ToList();
        var answered = results.Where(result => result.Precision is not null).ToList();
        var classified = results.Where(result => result.CategoryRecall is not null).ToList();
        var withCategories = results.Where(result => result.CategoryPrecision is not null).ToList();

        return new Summary(
            Tasks: results.Count,
            TasksWithStandards: withStandards.Count,
            TasksWithoutStandards: withoutStandards.Count,
            MeanRecall: Mean(withStandards.Select(result => result.Recall!.Value)),
            FoundAllRate: Mean(withStandards.Select(result => result.Recall == 1 ? 1.0 : 0.0)),
            FoundAnyRate: Mean(withStandards.Select(result => result.Recall > 0 ? 1.0 : 0.0)),
            ReturnedNothing: results.Count(result => result.Returned.Length == 0),
            MeanPrecision: answered.Count == 0 ? null : Mean(answered.Select(result => result.Precision!.Value)),
            MeanReturned: Mean(results.Select(result => (double)result.Returned.Length)),
            ForbiddenReturned: results.Sum(result => result.Forbidden.Length),
            NoStandardCorrect: withoutStandards.Count(result => result.Returned.Length == 0),
            MeanReciprocalRank: ranked ? Mean(withStandards.Select(result => result.ReciprocalRank!.Value)) : null,
            MeanRecallAt5: ranked ? Mean(withStandards.Select(result => result.RecallAt5!.Value)) : null,
            MeanCategoryRecall: Mean(classified.Select(result => result.CategoryRecall!.Value)),
            MeanCategoryPrecision: withCategories.Count == 0 ? null : Mean(withCategories.Select(result => result.CategoryPrecision!.Value)));
    }

    private static double Mean(IEnumerable<double> values)
    {
        var list = values.ToList();
        return list.Count == 0 ? 0 : list.Average();
    }
}
