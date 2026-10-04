using System.Security.Cryptography;
using System.Text;
using OrgStandards.Contracts;
using OrgStandards.Data;
using OrgStandards.Migrations;

namespace OrgStandards.Evaluation;

public sealed record OwnedTopic(string Source, StandardTopic Topic);

// Every owner's standards, read from the seed folder (one subfolder of Markdown per owner) with the
// same parser the migrations use. What the owners' servers would hold, without running them.
public sealed class StandardsCorpus
{
    public required IReadOnlyList<OwnedTopic> Topics { get; init; }
    public required Dictionary<string, string[]> Fields { get; init; }
    public required string[] Owners { get; init; }

    // A short hash of the standards' content, so reports say exactly which standards they measured.
    public required string Fingerprint { get; init; }

    public static StandardsCorpus Load(string seedFolder)
    {
        var owners = Directory.EnumerateDirectories(seedFolder)
            .Where(folder => Directory.EnumerateFiles(folder, "*.md").Any())
            .Order(StringComparer.Ordinal)
            .ToArray();

        var files = owners.SelectMany(owner => Directory.EnumerateFiles(owner, "*.md").Order(StringComparer.Ordinal)).ToArray();

        var topics = owners
            .SelectMany(owner => Directory.EnumerateFiles(owner, "*.md")
                .Order(StringComparer.Ordinal)
                .SelectMany(StandardsMarkdown.Parse)
                .Select(topic => new OwnedTopic(Path.GetFileName(owner), StandardMatcher.ToContract(topic))))
            .ToList();

        return new StandardsCorpus
        {
            Topics = topics,
            Fields = StandardMatcher.FieldsOf(topics.Select(t => t.Topic)),
            Owners = owners.Select(Path.GetFileName).Select(name => name!).ToArray(),
            Fingerprint = Hash(files.Select(file => Path.GetRelativePath(seedFolder, file) + "\n" + File.ReadAllText(file).Replace("\r\n", "\n"))),
        };
    }

    public static string Hash(IEnumerable<string> parts) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("\0", parts))))[..12];
}
