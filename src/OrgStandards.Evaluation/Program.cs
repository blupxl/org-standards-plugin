using OrgStandards.Evaluation;

// Measures how well each retrieval strategy finds the standards for the golden set's tasks, and
// writes a report per strategy plus a side-by-side comparison. Runs in process on the seed files:
// no services, no containers, no model calls, so it's free and gives the same result every time.
//
//   dotnet run --project src/OrgStandards.Evaluation -- [--strategy <name>] [--out <folder>]
//
// Default: every strategy, reports in evaluation/results/ under the current folder.

var strategyName = Option("--strategy");
var output = Option("--out") ?? Path.Combine("evaluation", "results");
var seed = Option("--seed") ?? Path.Combine(AppContext.BaseDirectory, "seed");

var taxonomy = Taxonomy.Load(Path.Combine(seed, "taxonomy.yaml"));
var strategies = new IRetrievalStrategy[] { new TagStrategy(taxonomy, acceptSuggestions: false), new TagStrategy(taxonomy, acceptSuggestions: true) };

var selected = strategyName is null
    ? strategies
    : strategies.Where(s => s.Name.Equals(strategyName, StringComparison.OrdinalIgnoreCase)).ToArray();
if (selected.Length == 0)
{
    Console.Error.WriteLine($"Unknown strategy \"{strategyName}\". Strategies: {string.Join(", ", strategies.Select(s => s.Name))}.");
    return 1;
}

var corpus = StandardsCorpus.Load(seed);
var golden = GoldenSet.Load(Path.Combine(seed, "evals", "golden.yaml"));
var date = DateTime.Now.ToString("yyyy-MM-dd");

Directory.CreateDirectory(output);
var reports = new List<EvaluationReport>();
foreach (var strategy in selected)
{
    var report = Report.Run(strategy, corpus, golden, date, output);
    reports.Add(report);

    var path = Path.Combine(output, $"{date}-{strategy.Name}");
    File.WriteAllText(path + ".json", Report.ToJson(report));
    File.WriteAllText(path + ".md", Report.ToMarkdown(report));

    var s = report.Summary;
    Console.WriteLine($"{strategy.Name,-14} recall {s.MeanRecall:P0} · found all {s.FoundAllRate:P0} · " +
                      $"returned nothing {s.ReturnedNothing}/{s.Tasks} · precision {s.MeanPrecision:P0} → {path}.md");
}

if (reports.Count > 1)
{
    var comparison = Path.Combine(output, $"{date}-comparison.md");
    File.WriteAllText(comparison, Report.Comparison(reports));
    Console.WriteLine($"Comparison → {comparison}");
}

return 0;

string? Option(string name)
{
    var index = Array.IndexOf(args, name);
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}
