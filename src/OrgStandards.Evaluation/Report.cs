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
        var s = report.Summary;
        var md = new StringBuilder();

        md.AppendLine($"# Retrieval evaluation: {run.Strategy}");
        md.AppendLine();
        md.AppendLine(run.Description);
        md.AppendLine();
        md.AppendLine($"- **Date:** {run.Date} · **Commit:** {run.Commit}{(run.UncommittedChanges ? " (with uncommitted changes)" : "")}");
        md.AppendLine($"- **Data:** {run.Data}: {run.Topics} topics from {run.Owners} owners (standards `{run.StandardsFingerprint}`, golden set `{run.GoldenSetFingerprint}`)");
        md.AppendLine($"- **Model:** {run.Model} · **Ranked:** {(run.Ranked ? "yes" : "no (rank metrics don't apply)")}");
        md.AppendLine();
        md.AppendLine("## Summary");
        md.AppendLine();
        md.AppendLine("| Metric | Value |");
        md.AppendLine("|---|---|");
        md.AppendLine($"| Tasks | {s.Tasks} ({s.TasksWithStandards} with standards, {s.TasksWithoutStandards} without) |");
        md.AppendLine($"| Mean recall | {Percent(s.MeanRecall)} |");
        md.AppendLine($"| Found every expected topic | {Percent(s.FoundAllRate)} of tasks |");
        md.AppendLine($"| Found at least one | {Percent(s.FoundAnyRate)} of tasks |");
        md.AppendLine($"| Returned nothing | {s.ReturnedNothing} of {s.Tasks} tasks |");
        md.AppendLine($"| Mean precision (tasks that returned something) | {Percent(s.MeanPrecision)} |");
        md.AppendLine($"| Mean topics returned | {s.MeanReturned.ToString("0.0", CultureInfo.InvariantCulture)} |");
        md.AppendLine($"| Forbidden topics returned | {s.ForbiddenReturned} |");
        md.AppendLine($"| No-standard tasks answered with nothing | {s.NoStandardCorrect} of {s.TasksWithoutStandards} |");
        if (run.Ranked)
        {
            md.AppendLine($"| Mean reciprocal rank | {s.MeanReciprocalRank?.ToString("0.00", CultureInfo.InvariantCulture)} |");
            md.AppendLine($"| Mean recall@{Evaluator.K} | {Percent(s.MeanRecallAt5)} |");
        }

        md.AppendLine($"| Category recall | {Percent(s.MeanCategoryRecall)} |");
        md.AppendLine($"| Category precision | {Percent(s.MeanCategoryPrecision)} |");
        md.AppendLine();
        md.AppendLine("## Tasks");
        md.AppendLine();
        md.AppendLine("| Task | Categories (expected → found) | Expected | Missed | Returned | Recall | Precision |");
        md.AppendLine("|---|---|---|---|---|---|---|");
        foreach (var t in report.Tasks)
        {
            var scope = t.Scope.Count == 0 ? "" : $" *({string.Join(", ", t.Scope.Select(e => $"{e.Key}: {string.Join(", ", e.Value)}"))})*";
            md.AppendLine($"| {Escape(t.Task)}{scope} | {List(t.ExpectedCategories)} → {List(t.Categories)} | {List(t.Expected)} | " +
                          $"{List(t.Missed)} | {t.Returned.Length} | {Percent(t.Recall)} | {Percent(t.Precision)} |");
        }

        return md.ToString();
    }

    // Several strategies on the same golden set, side by side (REQ-EVAL AC3).
    public static string Comparison(IReadOnlyList<EvaluationReport> reports)
    {
        var md = new StringBuilder();
        var first = reports[0].Run;
        md.AppendLine("# Retrieval evaluation: comparison");
        md.AppendLine();
        md.AppendLine($"- **Date:** {first.Date} · **Commit:** {first.Commit}{(first.UncommittedChanges ? " (with uncommitted changes)" : "")}");
        md.AppendLine($"- **Data:** {first.Data}: {first.Topics} topics from {first.Owners} owners (standards `{first.StandardsFingerprint}`, golden set `{first.GoldenSetFingerprint}`)");
        md.AppendLine();
        md.AppendLine($"| Metric | {string.Join(" | ", reports.Select(r => r.Run.Strategy))} |");
        md.AppendLine($"|---|{string.Concat(reports.Select(_ => "---|"))}");
        Row("Mean recall", r => Percent(r.Summary.MeanRecall));
        Row("Found every expected topic", r => Percent(r.Summary.FoundAllRate));
        Row("Found at least one", r => Percent(r.Summary.FoundAnyRate));
        Row("Returned nothing", r => $"{r.Summary.ReturnedNothing} of {r.Summary.Tasks}");
        Row("Mean precision", r => Percent(r.Summary.MeanPrecision));
        Row("Mean topics returned", r => r.Summary.MeanReturned.ToString("0.0", CultureInfo.InvariantCulture));
        Row("Forbidden topics returned", r => r.Summary.ForbiddenReturned.ToString(CultureInfo.InvariantCulture));
        Row("No-standard tasks answered with nothing", r => $"{r.Summary.NoStandardCorrect} of {r.Summary.TasksWithoutStandards}");
        Row($"Mean recall@{Evaluator.K}", r => r.Run.Ranked ? Percent(r.Summary.MeanRecallAt5) : "n/a (unranked)");
        Row("Category recall", r => Percent(r.Summary.MeanCategoryRecall));
        Row("Category precision", r => Percent(r.Summary.MeanCategoryPrecision));
        return md.ToString();

        void Row(string name, Func<EvaluationReport, string> value) =>
            md.AppendLine($"| {name} | {string.Join(" | ", reports.Select(value))} |");
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
