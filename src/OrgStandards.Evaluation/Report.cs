using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace OrgStandards.Evaluation;

// Everything needed to compare one run with another (REQ-EVAL R10).
public sealed record RunInfo(
    string Strategy,
    string Description,
    bool Ranked,
    string Date,
    string Commit,
    bool UncommittedChanges,
    string Data,
    string StandardsFingerprint,
    string GoldenSetFingerprint,
    int Owners,
    int Topics,
    string Model);

public sealed record EvaluationReport(RunInfo Run, Summary Summary, IReadOnlyList<TaskResult> Tasks);

public static class Report
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    // outputFolder is left out of the "uncommitted changes" check: earlier reports there don't
    // change what's measured.
    public static EvaluationReport Run(IRetrievalStrategy strategy, StandardsCorpus corpus, GoldenSet golden, string date, string outputFolder)
    {
        var results = golden.Tasks
            .Select(task => Evaluator.Score(task, strategy.Retrieve(task, corpus), strategy.Ranked))
            .ToList();

        var (commit, dirty) = GitState(outputFolder);
        var run = new RunInfo(
            strategy.Name, strategy.Description, strategy.Ranked, date, commit, dirty,
            Data: "seed (fictitious placeholder standards)",
            StandardsFingerprint: corpus.Fingerprint,
            GoldenSetFingerprint: golden.Fingerprint,
            Owners: corpus.Owners.Length,
            Topics: corpus.Topics.Count,
            Model: "none");

        return new EvaluationReport(run, Evaluator.Summarize(results, strategy.Ranked), results);
    }

    public static string ToJson(EvaluationReport report) => JsonSerializer.Serialize(report, JsonOptions);

    public static string ToMarkdown(EvaluationReport report)
    {
        var run = report.Run;
        var summary = report.Summary;
        var markdown = new StringBuilder();

        markdown.AppendLine($"# Retrieval evaluation: {run.Strategy}");
        markdown.AppendLine();
        markdown.AppendLine(run.Description);
        markdown.AppendLine();
        markdown.AppendLine($"- **Date:** {run.Date} · **Commit:** {run.Commit}{(run.UncommittedChanges ? " (with uncommitted changes)" : "")}");
        markdown.AppendLine($"- **Data:** {run.Data}: {run.Topics} topics from {run.Owners} owners (standards `{run.StandardsFingerprint}`, golden set `{run.GoldenSetFingerprint}`)");
        markdown.AppendLine($"- **Model:** {run.Model} · **Ranked:** {(run.Ranked ? "yes" : "no (rank metrics don't apply)")}");
        markdown.AppendLine();
        markdown.AppendLine("## Summary");
        markdown.AppendLine();
        markdown.AppendLine("| Metric | Value |");
        markdown.AppendLine("|---|---|");
        markdown.AppendLine($"| Tasks | {summary.Tasks} ({summary.TasksWithStandards} with standards, {summary.TasksWithoutStandards} without) |");
        markdown.AppendLine($"| Mean recall | {Percent(summary.MeanRecall)} |");
        markdown.AppendLine($"| Found every expected topic | {Percent(summary.FoundAllRate)} of tasks |");
        markdown.AppendLine($"| Found at least one | {Percent(summary.FoundAnyRate)} of tasks |");
        markdown.AppendLine($"| Returned nothing | {summary.ReturnedNothing} of {summary.Tasks} tasks |");
        markdown.AppendLine($"| Mean precision (tasks that returned something) | {Percent(summary.MeanPrecision)} |");
        markdown.AppendLine($"| Mean topics returned | {summary.MeanReturned.ToString("0.0", CultureInfo.InvariantCulture)} |");
        markdown.AppendLine($"| Forbidden topics returned | {summary.ForbiddenReturned} |");
        markdown.AppendLine($"| No-standard tasks answered with nothing | {summary.NoStandardCorrect} of {summary.TasksWithoutStandards} |");
        if (run.Ranked)
        {
            markdown.AppendLine($"| Mean reciprocal rank | {summary.MeanReciprocalRank?.ToString("0.00", CultureInfo.InvariantCulture)} |");
            markdown.AppendLine($"| Mean recall@{Evaluator.K} | {Percent(summary.MeanRecallAt5)} |");
        }

        markdown.AppendLine($"| Category recall | {Percent(summary.MeanCategoryRecall)} |");
        markdown.AppendLine($"| Category precision | {Percent(summary.MeanCategoryPrecision)} |");
        markdown.AppendLine();
        markdown.AppendLine("## Tasks");
        markdown.AppendLine();
        markdown.AppendLine("| Task | Categories (expected → found) | Expected | Missed | Returned | Recall | Precision |");
        markdown.AppendLine("|---|---|---|---|---|---|---|");
        foreach (var task in report.Tasks)
        {
            var scope = task.Scope.Count == 0 ? "" : $" *({string.Join(", ", task.Scope.Select(entry => $"{entry.Key}: {string.Join(", ", entry.Value)}"))})*";
            markdown.AppendLine($"| {Escape(task.Task)}{scope} | {List(task.ExpectedCategories)} → {List(task.Categories)} | {List(task.Expected)} | " +
                          $"{List(task.Missed)} | {task.Returned.Length} | {Percent(task.Recall)} | {Percent(task.Precision)} |");
        }

        return markdown.ToString();
    }

    // Several strategies on the same golden set, side by side (REQ-EVAL AC3).
    public static string Comparison(IReadOnlyList<EvaluationReport> reports)
    {
        var markdown = new StringBuilder();
        var first = reports[0].Run;
        markdown.AppendLine("# Retrieval evaluation: comparison");
        markdown.AppendLine();
        markdown.AppendLine($"- **Date:** {first.Date} · **Commit:** {first.Commit}{(first.UncommittedChanges ? " (with uncommitted changes)" : "")}");
        markdown.AppendLine($"- **Data:** {first.Data}: {first.Topics} topics from {first.Owners} owners (standards `{first.StandardsFingerprint}`, golden set `{first.GoldenSetFingerprint}`)");
        markdown.AppendLine();
        markdown.AppendLine($"| Metric | {string.Join(" | ", reports.Select(report => report.Run.Strategy))} |");
        markdown.AppendLine($"|---|{string.Concat(reports.Select(_ => "---|"))}");
        Row("Mean recall", report => Percent(report.Summary.MeanRecall));
        Row("Found every expected topic", report => Percent(report.Summary.FoundAllRate));
        Row("Found at least one", report => Percent(report.Summary.FoundAnyRate));
        Row("Returned nothing", report => $"{report.Summary.ReturnedNothing} of {report.Summary.Tasks}");
        Row("Mean precision", report => Percent(report.Summary.MeanPrecision));
        Row("Mean topics returned", report => report.Summary.MeanReturned.ToString("0.0", CultureInfo.InvariantCulture));
        Row("Forbidden topics returned", report => report.Summary.ForbiddenReturned.ToString(CultureInfo.InvariantCulture));
        Row("No-standard tasks answered with nothing", report => $"{report.Summary.NoStandardCorrect} of {report.Summary.TasksWithoutStandards}");
        Row($"Mean recall@{Evaluator.K}", report => report.Run.Ranked ? Percent(report.Summary.MeanRecallAt5) : "n/a (unranked)");
        Row("Category recall", report => Percent(report.Summary.MeanCategoryRecall));
        Row("Category precision", report => Percent(report.Summary.MeanCategoryPrecision));
        return markdown.ToString();

        void Row(string name, Func<EvaluationReport, string> value) =>
            markdown.AppendLine($"| {name} | {string.Join(" | ", reports.Select(value))} |");
    }

    private static string Percent(double? value) =>
        value is null ? "n/a" : (value.Value * 100).ToString("0", CultureInfo.InvariantCulture) + "%";

    private static string List(string[] values) => values.Length == 0 ? "–" : Escape(string.Join(", ", values));

    private static string Escape(string text) => text.Replace("|", "\\|");

    private static (string Commit, bool Dirty) GitState(string outputFolder)
    {
        var commit = Git("rev-parse --short HEAD");
        var status = Git($"status --porcelain -- . \":(exclude){Path.GetFullPath(outputFolder).Replace('\\', '/')}\"");
        return (commit ?? "unknown", !string.IsNullOrWhiteSpace(status));
    }

    private static string? Git(string arguments)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo("git", arguments)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            });
            if (process is null)
            {
                return null;
            }

            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();
            return process.ExitCode == 0 ? output : null;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return null;
        }
    }
}
