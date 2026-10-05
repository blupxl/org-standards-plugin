using OrgStandards.Data;

namespace OrgStandards.Tests;

// The owners' taxonomy, as the servers serve it.
public class TaxonomyTests
{
    private static readonly string TaxonomyFile = Path.Combine(AppContext.BaseDirectory, "seed", "taxonomy.yaml");

    [Fact]
    public void Every_category_is_served_with_its_facet_description_and_signals()
    {
        var taxonomy = Taxonomy.Load(TaxonomyFile);

        var categories = taxonomy.ToContract();

        Assert.Equal(taxonomy.Categories.Count, categories.Length);
        var redis = Assert.Single(categories, category => category.Name == "redis");
        Assert.Equal("uses", redis.Facet);
        Assert.Contains("StackExchange.Redis", redis.Signals);
        Assert.False(string.IsNullOrWhiteSpace(redis.Description));
    }

    [Fact]
    public void Categories_are_ordered_by_facet_then_name()
    {
        var categories = Taxonomy.Load(TaxonomyFile).ToContract();

        var ordered = categories
            .OrderBy(category => category.Facet, StringComparer.Ordinal)
            .ThenBy(category => category.Name, StringComparer.Ordinal);
        Assert.Equal(ordered.Select(category => category.Name), categories.Select(category => category.Name));
    }

    [Fact]
    public void The_taxonomy_owner_serves_every_category()
    {
        var categories = Taxonomy.Load(TaxonomyFile).ToContract();
        var source = new OrgStandards.Source.SourceInfo("platform", new Dictionary<string, string>(), categories);

        var listing = OrgStandards.Source.SourceTools.DescribeTaxonomy(source);

        Assert.Equal("platform", listing.Source);
        Assert.Equal(categories.Length, listing.Categories.Length);
    }

    [Fact]
    public void Other_owners_serve_no_categories()
    {
        var source = new OrgStandards.Source.SourceInfo("design", new Dictionary<string, string>());

        var listing = OrgStandards.Source.SourceTools.DescribeTaxonomy(source);

        Assert.Equal("design", listing.Source);
        Assert.Empty(listing.Categories);
    }
}
