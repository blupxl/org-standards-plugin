using OrgStandards.Contracts;
using OrgStandards.Gateway;

namespace OrgStandards.Tests;

// get_taxonomy: one owner's categories are the taxonomy. Without a configured owner, every
// owner's categories are merged.
public class TaxonomyMergeTests
{
    private static TaxonomyCategory Category(string name, string facet = "uses", string? description = null, string[]? signals = null) =>
        new(name, facet, description ?? $"{name}, described.", [], null, [], signals ?? [$"{name}-signal"]);

    private static SourceCall<TaxonomyListing> Serves(string source, params TaxonomyCategory[] categories) =>
        new(source, new TaxonomyListing(source, categories), null);

    private static SourceCall<TaxonomyListing> Unavailable(string source) =>
        new(source, null, "timed out after 10s");

    [Fact]
    public void The_owners_categories_come_back_once_each()
    {
        var result = TaxonomyMerge.Merge([Serves("platform", Category("redis"), Category("api", "kind")), Serves("design")]);

        Assert.Equal("ok", result.Status);
        Assert.Equal(["api", "redis"], result.Categories.Select(category => category.Name));
        Assert.Empty(result.Conflicts);
    }

    [Fact]
    public void The_same_category_from_two_owners_is_kept_once()
    {
        var result = TaxonomyMerge.Merge([Serves("platform", Category("redis")), Serves("data", Category("redis"))]);

        Assert.Single(result.Categories);
        Assert.Empty(result.Conflicts);
    }

    [Fact]
    public void A_category_two_owners_file_under_different_facets_is_a_conflict()
    {
        var result = TaxonomyMerge.Merge([Serves("platform", Category("caching", "concern")), Serves("data", Category("caching", "kind"))]);

        var conflict = Assert.Single(result.Conflicts);
        Assert.Contains("caching", conflict);
        Assert.Equal("kind", Assert.Single(result.Categories).Facet);   // owners in name order: data, then platform
    }

    [Fact]
    public void An_unavailable_owner_makes_it_partial()
    {
        var result = TaxonomyMerge.Merge([Serves("platform", Category("redis")), Unavailable("security")]);

        Assert.Equal("partial", result.Status);
        Assert.Contains(result.Sources, source => source.Name == "security" && source.Status == "unavailable");
    }

    [Fact]
    public void Without_the_taxonomy_owner_it_is_unavailable()
    {
        var result = TaxonomyMerge.Merge([Unavailable("platform"), Serves("design")]);

        Assert.Equal("unavailable", result.Status);
        Assert.Empty(result.Categories);
        Assert.Equal(2, result.Sources.Length);
    }

    [Fact]
    public void The_taxonomy_owners_categories_are_the_taxonomy_and_another_owners_are_ignored()
    {
        var result = TaxonomyMerge.Merge(
            [Serves("platform", Category("redis")), Serves("data", Category("redis"), Category("mongodb"))],
            taxonomyOwner: "platform");

        Assert.Equal("ok", result.Status);
        Assert.Equal(["redis"], result.Categories.Select(category => category.Name));
        var conflict = Assert.Single(result.Conflicts);
        Assert.Equal("data also serves a taxonomy; only platform's is used", conflict);
    }

    [Fact]
    public void An_owner_that_serves_no_categories_is_not_a_conflict()
    {
        var result = TaxonomyMerge.Merge([Serves("platform", Category("redis")), Serves("design")], taxonomyOwner: "platform");

        Assert.Empty(result.Conflicts);
    }

    [Fact]
    public void When_the_taxonomy_owner_is_unavailable_the_taxonomy_is_unavailable_even_if_others_answered()
    {
        var result = TaxonomyMerge.Merge([Unavailable("platform"), Serves("data", Category("redis"))], taxonomyOwner: "platform");

        Assert.Equal("unavailable", result.Status);
        Assert.Empty(result.Categories);
    }

    [Fact]
    public void Without_a_taxonomy_owner_the_same_category_with_different_metadata_is_a_conflict()
    {
        var result = TaxonomyMerge.Merge(
            [Serves("platform", Category("redis")), Serves("data", Category("redis", description: "A cache, described differently."))]);

        var conflict = Assert.Single(result.Conflicts);
        Assert.Contains("redis", conflict);
        Assert.Single(result.Categories);
    }

    [Fact]
    public void Without_a_taxonomy_owner_different_signals_are_a_conflict()
    {
        var result = TaxonomyMerge.Merge(
            [Serves("platform", Category("redis")), Serves("data", Category("redis", signals: ["StackExchange.Redis"]))]);

        Assert.Single(result.Conflicts);
    }

    [Fact]
    public void With_a_taxonomy_owner_only_the_owner_is_asked()
    {
        Assert.Equal(["platform"], TaxonomyMerge.SourcesToAsk(["design", "platform", "data"], "platform"));
    }

    [Fact]
    public void The_owner_is_matched_ignoring_case_and_asked_by_its_configured_name()
    {
        Assert.Equal(["platform"], TaxonomyMerge.SourcesToAsk(["design", "platform"], "PLATFORM"));
    }

    [Fact]
    public void An_owner_that_is_not_a_configured_source_is_not_asked_and_the_taxonomy_is_unavailable()
    {
        Assert.Empty(TaxonomyMerge.SourcesToAsk(["design", "data"], "platform"));
        Assert.Equal("unavailable", TaxonomyMerge.Merge([], "platform").Status);
    }

    [Fact]
    public void Without_a_taxonomy_owner_every_source_is_asked()
    {
        Assert.Equal(["design", "platform"], TaxonomyMerge.SourcesToAsk(["design", "platform"], null));
        Assert.Equal(["design", "platform"], TaxonomyMerge.SourcesToAsk(["design", "platform"], " "));
    }
}
