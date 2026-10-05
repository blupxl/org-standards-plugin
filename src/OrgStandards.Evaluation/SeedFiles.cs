using OrgStandards.Data;

namespace OrgStandards.Evaluation;

// The golden set (evals/golden.yaml), kept with the standards.

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
