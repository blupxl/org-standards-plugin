using OrgStandards.Contracts;

namespace OrgStandards.Tests;

// What `broader` means: a kind is also each of its ancestors, to the root. Nothing else expands.
public class AncestryTests
{
    private static TaxonomyCategory Category(string name, string facet, params string[] broader) =>
        new(name, facet, $"{name}, described.", broader, null, [], []);

    private static readonly TaxonomyCategory[] Taxonomy =
    [
        Category("presentation", "kind"), Category("styling", "kind", "presentation"), Category("css", "kind", "styling"),
        Category("sass", "kind", "css"), Category("performance", "concern"), Category("caching", "concern", "performance"),
    ];

    private static Dictionary<string, string[]> Filter(params (string Field, string[] Values)[] entries) =>
        entries.ToDictionary(entry => entry.Field, entry => entry.Values, StringComparer.OrdinalIgnoreCase);

    [Fact]
    public void A_kind_expands_to_every_ancestor()
    {
        var expanded = Ancestry.Expand(Filter(("kind", ["sass"])), Taxonomy);

        Assert.Equal(["sass", "css", "styling", "presentation"], expanded["kind"]);
    }

    [Fact]
    public void A_concern_never_expands()
    {
        var expanded = Ancestry.Expand(Filter(("kind", ["css"]), ("concern", ["caching"])), Taxonomy);

        Assert.Equal(["caching"], expanded["concern"]);
    }

    [Fact]
    public void The_requested_filter_is_not_changed()
    {
        var requested = Filter(("kind", ["sass"]));

        Ancestry.Expand(requested, Taxonomy);

        Assert.Equal(["sass"], requested["kind"]);
    }

    [Fact]
    public void Unknown_kinds_are_kept_as_they_are()
    {
        var expanded = Ancestry.Expand(Filter(("kind", ["sas"])), Taxonomy);

        Assert.Equal(["sas"], expanded["kind"]);
    }

    [Fact]
    public void A_cycle_ends_the_walk()
    {
        TaxonomyCategory[] cyclic = [Category("sass", "kind", "css"), Category("css", "kind", "sass")];

        var expanded = Ancestry.Expand(Filter(("kind", ["sass"])), cyclic);

        Assert.Equal(["sass", "css"], expanded["kind"]);
    }

    [Fact]
    public void Missing_parents_and_parents_in_another_facet_are_ignored()
    {
        TaxonomyCategory[] broken = [Category("sass", "kind", "css", "caching"), Category("caching", "concern")];

        var expanded = Ancestry.Expand(Filter(("kind", ["sass"])), broken);

        Assert.Equal(["sass"], expanded["kind"]);
    }

    [Fact]
    public void Added_lists_only_the_ancestors()
    {
        var requested = Filter(("kind", ["sass", "css"]));

        Assert.Equal(["styling", "presentation"], Ancestry.Added(requested, Ancestry.Expand(requested, Taxonomy)));
    }
}
