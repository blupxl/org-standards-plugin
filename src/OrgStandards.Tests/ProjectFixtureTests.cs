using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using OrgStandards.Data;

namespace OrgStandards.Tests;

// The invented project fixtures (tests/fixtures/projects/<name>) and their scoring keys
// (tests/fixtures/expected/<name>.json), kept apart so a classifier can read a fixture with no
// answers in it. These tests keep each key honest against the taxonomy and the fixture's files.
//
// How a signal is read. Its form decides what it is compared with:
//   "Npgsql*"            a package: a PackageReference Include value in a *.csproj
//   "pg (npm)"           an npm package: a dependency key in package.json
//   "redis:* (image)"    a container image: an `image:` value in a compose file
//   "AddRedis("          an Aspire call: a `.AddRedis(` call in C# code
// A "*" matches any run of characters and the whole name must match, so Microsoft.Extensions.
// Caching.StackExchangeRedis is not StackExchange.Redis, and bitnami/redis:7 is not redis:*.
// "a / b" lists alternatives. An image signal without a wildcard (confluentinc/cp-kafka) matches
// that image with any tag.
//
// An `evidence` value in a key is a file pattern from the category's `files`, or the concrete
// name found in `where` (redis:7 for redis:* (image)). Commented-out lines never count.
public class ProjectFixtureTests
{
    private static readonly string TaxonomyFile = Path.Combine(AppContext.BaseDirectory, "seed", "taxonomy.yaml");
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    // Scored is false for an entry that is exact in the taxonomy's terms but not scored as a miss.
    private sealed record Exact(string Category, string Facet, string Evidence, string Where, bool? Scored, string? Why);
    private sealed record Component(string Name, string Path, Exact[] Exact);
    private sealed record Distractor(string Text, string Where, string Why);
    private sealed record Key(string Fixture, Component[] Components, Exact[]? Outside, Distractor[] Distractors)
    {
        // Entries for files that belong to no component, such as a solution file at the root.
        public Exact[] OutsideEntries => Outside ?? [];

        public Component? OwnerOf(string relativePath) =>
            Components
                .Where(component => component.Path == "." || relativePath.StartsWith(component.Path + "/", StringComparison.Ordinal))
                .OrderByDescending(component => component.Path.Length)
                .FirstOrDefault();
    }

    private enum Form { Package, Npm, Image, Call }

    public static TheoryData<string> FixtureNames()
    {
        var names = new TheoryData<string>();
        foreach (var directory in Directory.GetDirectories(FixturesFolder()).OrderBy(path => path, StringComparer.Ordinal))
            names.Add(Path.GetFileName(directory));
        return names;
    }

    [Fact]
    public void There_are_between_three_and_five_fixtures()
    {
        var count = Directory.GetDirectories(FixturesFolder()).Length;

        Assert.InRange(count, 3, 5);
    }

    [Fact]
    public void Every_fixture_has_a_key_and_every_key_has_a_fixture()
    {
        var fixtures = Directory.GetDirectories(FixturesFolder()).Select(Path.GetFileName).Order().ToArray();
        var keys = Directory.GetFiles(KeysFolder(), "*.json").Select(Path.GetFileNameWithoutExtension).Order().ToArray();

        Assert.Equal(fixtures, keys);
    }

    [Theory]
    [MemberData(nameof(FixtureNames))]
    public void Every_fixture_has_a_key_naming_itself_with_components_and_a_distractor(string fixture)
    {
        var key = ReadKey(fixture);

        Assert.Equal(fixture, key.Fixture);
        Assert.NotEmpty(key.Components);
        Assert.NotEmpty(key.Distractors);
        foreach (var component in key.Components)
        {
            Assert.True(Directory.Exists(Path.Combine(FixturesFolder(), fixture, component.Path)),
                $"{fixture}: component {component.Name} has no folder {component.Path}");
            Assert.NotEmpty(component.Exact);
        }
    }

    [Theory]
    [MemberData(nameof(FixtureNames))]
    public void Every_exact_entry_is_a_taxonomy_signal_or_file_pattern_present_in_the_fixture(string fixture)
    {
        var taxonomy = Taxonomy.Load(TaxonomyFile);
        var key = ReadKey(fixture);

        foreach (var (scope, owner, exact) in Entries(key))
        {
            var label = $"{fixture}/{scope}: {exact.Category} <- {exact.Evidence} in {exact.Where}";

            Assert.True(taxonomy.Categories.TryGetValue(exact.Category, out var category), $"{label}: no such category");
            Assert.True(category!.Facet == exact.Facet, $"{label}: the taxonomy puts it in facet {category.Facet}, not {exact.Facet}");
            Assert.True(exact.Scored != false || !string.IsNullOrWhiteSpace(exact.Why), $"{label}: an unscored entry needs a reason");

            var file = Path.Combine(FixturesFolder(), fixture, exact.Where);
            Assert.True(File.Exists(file), $"{label}: no such file");
            Assert.True(key.OwnerOf(exact.Where)?.Name == owner?.Name, $"{label}: the file belongs to a different component than {scope}");

            if (category.Files.Contains(exact.Evidence))
            {
                Assert.True(MatchesGlob(exact.Evidence, Path.GetFileName(file)), $"{label}: the file name doesn't match the pattern");
            }
            else
            {
                Assert.True(category.Signals.Any(signal => SignalNames(signal).Any(name => MatchesGlob(name, exact.Evidence))),
                    $"{label}: not one of {exact.Category}'s signals or files");
                Assert.True(WithoutComments(File.ReadAllText(file)).Contains(exact.Evidence, StringComparison.Ordinal),
                    $"{label}: the text isn't in the file outside comments");
            }
        }
    }

    [Theory]
    [MemberData(nameof(FixtureNames))]
    public void No_exact_entry_is_also_a_distractor(string fixture)
    {
        var key = ReadKey(fixture);

        foreach (var (scope, _, exact) in Entries(key))
            Assert.DoesNotContain(key.Distractors, distractor => distractor.Text == exact.Evidence && distractor.Where == exact.Where);
    }

    [Theory]
    [MemberData(nameof(FixtureNames))]
    public void Every_file_matching_a_taxonomy_file_pattern_is_in_the_key(string fixture)
    {
        var taxonomy = Taxonomy.Load(TaxonomyFile);
        var key = ReadKey(fixture);

        foreach (var relative in FixtureFiles(fixture))
        {
            var keyed = (key.OwnerOf(relative)?.Exact ?? key.OutsideEntries).Select(exact => (exact.Category, exact.Where)).ToHashSet();

            foreach (var (name, category) in taxonomy.Categories)
            {
                if (!category.Files.Any(pattern => MatchesGlob(pattern, Path.GetFileName(relative)))) continue;

                Assert.True(keyed.Contains((name, relative)), $"{fixture}: {relative} matches a {name} file pattern but isn't keyed for it");
            }
        }
    }

    // Every declared package, npm package, image and Aspire call that matches a taxonomy signal
    // must be keyed for its component, unless the key lists it as a distractor.
    [Theory]
    [MemberData(nameof(FixtureNames))]
    public void Every_declaration_matching_a_taxonomy_signal_is_in_the_key(string fixture)
    {
        var taxonomy = Taxonomy.Load(TaxonomyFile);
        var key = ReadKey(fixture);

        foreach (var relative in FixtureFiles(fixture))
        foreach (var (form, value) in Declarations(Path.Combine(FixturesFolder(), fixture, relative)))
        {
            if (key.Distractors.Any(distractor => distractor.Text == value && distractor.Where == relative)) continue;

            var owner = key.OwnerOf(relative);
            Assert.True(owner is not null || !MatchingCategories(taxonomy, form, value).Any(),
                $"{fixture}: {value} in {relative} matches a signal but the file is outside every component");

            foreach (var category in MatchingCategories(taxonomy, form, value))
                Assert.True(owner!.Exact.Any(exact => exact.Category == category),
                    $"{fixture}/{owner.Name}: {value} in {relative} is a {category} signal but {category} isn't keyed for the component");
        }
    }

    [Theory]
    [MemberData(nameof(FixtureNames))]
    public void Every_distractor_text_appears_where_the_key_says(string fixture)
    {
        foreach (var distractor in ReadKey(fixture).Distractors)
        {
            var file = Path.Combine(FixturesFolder(), fixture, distractor.Where);

            Assert.True(File.Exists(file), $"{fixture}: no such file {distractor.Where}");
            Assert.Contains(distractor.Text, File.ReadAllText(file), StringComparison.Ordinal);
            Assert.False(string.IsNullOrWhiteSpace(distractor.Why), $"{fixture}: distractor {distractor.Text} has no reason");
        }
    }

    private static Key ReadKey(string fixture) =>
        JsonSerializer.Deserialize<Key>(File.ReadAllText(Path.Combine(KeysFolder(), fixture + ".json")), Json)
        ?? throw new InvalidOperationException($"the key for {fixture} is empty.");

    // Each entry with the scope it was written under and the component that owns it (none for outside).
    private static IEnumerable<(string Scope, Component? Owner, Exact Entry)> Entries(Key key) =>
        key.Components.SelectMany(component => component.Exact.Select(exact => (component.Name, (Component?)component, exact)))
            .Concat(key.OutsideEntries.Select(exact => ("outside", (Component?)null, exact)));

    // Paths relative to the fixture, with forward slashes.
    private static IEnumerable<string> FixtureFiles(string fixture)
    {
        var folder = Path.Combine(FixturesFolder(), fixture);
        return Directory.GetFiles(folder, "*", SearchOption.AllDirectories)
            .Select(file => Path.GetRelativePath(folder, file).Replace('\\', '/'));
    }

    private static (Form Form, string[] Names) ParseSignal(string signal) =>
        signal switch
        {
            _ when signal.EndsWith(" (npm)") => (Form.Npm, SplitAlternatives(signal[..^" (npm)".Length])),
            _ when signal.EndsWith(" (image)") => (Form.Image, SplitAlternatives(signal[..^" (image)".Length])),
            _ when signal.EndsWith('(') => (Form.Call, [signal]),
            _ => (Form.Package, [signal]),
        };

    private static string[] SplitAlternatives(string names) => names.Split(" / ").Select(name => name.Trim()).ToArray();

    private static IEnumerable<string> SignalNames(string signal) => ParseSignal(signal).Names;

    private static IEnumerable<string> MatchingCategories(Taxonomy taxonomy, Form form, string value) =>
        taxonomy.Categories
            .Where(entry => entry.Value.Signals.Any(signal =>
            {
                var (signalForm, names) = ParseSignal(signal);
                return signalForm == form && names.Any(name => MatchesDeclaration(form, name, value));
            }))
            .Select(entry => entry.Key);

    private static bool MatchesDeclaration(Form form, string name, string value) =>
        MatchesGlob(name, value) || (form == Form.Image && !name.Contains(':') && value.StartsWith(name + ":", StringComparison.OrdinalIgnoreCase));

    // What a file declares that a signal could match, with comments removed.
    private static IEnumerable<(Form Form, string Value)> Declarations(string file)
    {
        var name = Path.GetFileName(file);
        var text = File.ReadAllText(file);

        if (name.EndsWith(".csproj"))
            foreach (Match match in Regex.Matches(WithoutComments(text), "PackageReference\\s+Include=\"([^\"]+)\""))
                yield return (Form.Package, match.Groups[1].Value);

        if (name == "package.json")
        {
            using var package = JsonDocument.Parse(text);
            foreach (var section in new[] { "dependencies", "devDependencies" })
                if (package.RootElement.TryGetProperty(section, out var dependencies))
                    foreach (var dependency in dependencies.EnumerateObject())
                        yield return (Form.Npm, dependency.Name);
        }

        if (name.StartsWith("compose") && name.EndsWith(".yaml"))
            foreach (Match match in Regex.Matches(WithoutComments(text), @"(?m)^\s*image:\s*(\S+)"))
                yield return (Form.Image, match.Groups[1].Value);

        if (name.EndsWith(".cs"))
            foreach (Match match in Regex.Matches(WithoutComments(text), @"\.(Add\w+)\("))
                yield return (Form.Call, match.Groups[1].Value + "(");
    }

    // Drops <!-- ... --> comments and whole lines starting with // or #.
    private static string WithoutComments(string text)
    {
        var withoutXml = Regex.Replace(text, "<!--.*?-->", "", RegexOptions.Singleline);
        var lines = withoutXml.Split('\n').Where(line =>
        {
            var trimmed = line.TrimStart();
            return !trimmed.StartsWith("//") && !trimmed.StartsWith('#');
        });
        return string.Join('\n', lines);
    }

    // Only * is special: any run of characters. Case doesn't matter for names or images.
    private static bool MatchesGlob(string pattern, string text) =>
        Regex.IsMatch(text, "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$", RegexOptions.IgnoreCase);

    private static string KeysFolder() => Path.Combine(Directory.GetParent(FixturesFolder())!.FullName, "expected");

    // The fixtures sit in the repo, not in the build output. Walk up from the test's folder to the
    // first folder holding tests/fixtures/projects: org-standards/ here, the repository root once
    // published. If the build output is somewhere else entirely (--artifacts-path), walk up from
    // this source file instead.
    private static string FixturesFolder([CallerFilePath] string sourceFile = "")
    {
        foreach (var start in new[] { AppContext.BaseDirectory, Path.GetDirectoryName(sourceFile)! })
        {
            for (var folder = new DirectoryInfo(start); folder is not null; folder = folder.Parent)
            {
                var candidate = Path.Combine(folder.FullName, "tests", "fixtures", "projects");
                if (Directory.Exists(candidate)) return candidate;
            }
        }
        throw new DirectoryNotFoundException("Couldn't find tests/fixtures/projects above the test folder.");
    }
}
