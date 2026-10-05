using OrgStandards.Contracts;
using OrgStandards.Gateway;
using static OrgStandards.Tests.TestData;

namespace OrgStandards.Tests;

// What the gateway decides after the owners answer: the product overlay, conflicts, validation
// and gaps.
public class ResolverTests
{
    private static readonly TopicDetail GeneralExample = new("Example", "general example");
    private static readonly TopicDetail GeneralTokens = new("Tokens", "general tokens");
    private static readonly TopicDetail ProductTokens = new("Tokens", "product tokens");

    [Fact]
    public void A_product_topic_replaces_the_general_topic_and_says_what_it_replaced()
    {
        var (topics, conflicts) = Resolver.Overlay(
        [
            ("design", Topic("Colors", version: "2.0")),
            ("design", Topic("Colors", version: "1.0", product: "xyz-public-app")),
        ]);

        var colors = Assert.Single(topics);
        Assert.Equal(["xyz-public-app"], colors.Product!);
        Assert.Equal(new Replaced("design", "2.0"), colors.Replaces);
        Assert.Empty(conflicts);
    }

    [Fact]
    public void A_product_topic_inherits_the_details_it_does_not_define()
    {
        var (topics, _) = Resolver.Overlay(
        [
            ("platform", Topic("Caching", details: [GeneralExample, GeneralTokens])),
            ("platform", Topic("Caching", product: "xyz-public-app", details: [ProductTokens])),
        ]);

        var caching = Assert.Single(topics);
        Assert.Equal(["Tokens", "Example"], caching.Topic.Details.Select(detail => detail.Title));
        Assert.Equal("product tokens", caching.Topic.Details.Single(detail => detail.Title == "Tokens").Markdown);
        Assert.Equal(["Example"], caching.InheritedDetails);
    }

    [Fact]
    public void The_same_topic_from_two_owners_is_kept_twice_and_flagged()
    {
        var (topics, conflicts) = Resolver.Overlay(
        [
            ("design", Topic("Caching")),
            ("platform", Topic("Caching")),
        ]);

        Assert.Equal(["design", "platform"], topics.Select(topic => topic.Source));
        var conflict = Assert.Single(conflicts);
        Assert.Contains("design", conflict);
        Assert.Contains("platform", conflict);
    }

    [Fact]
    public void An_unknown_value_gets_suggestions_and_is_never_substituted()
    {
        var unknown = Resolver.UnknownValues(
            Map(("product", ["xyz-pubic-app"])),
            Map(("product", ["acme-website", "xyz-public-app"])));

        Assert.Equal(["xyz-public-app"], unknown["product"]["xyz-pubic-app"]);
    }

    [Fact]
    public void Known_values_are_not_reported() =>
        Assert.Empty(Resolver.UnknownValues(Map(("area", ["branding"])), Map(("area", ["branding", "resilience"]))));

    [Fact]
    public void Unknown_fields_are_reported() =>
        Assert.Equal(["tier"], Resolver.UnknownFields(Map(("technology", ["ui"]), ("tier", ["gold"])), Map(("technology", ["ui"]))));

    [Fact]
    public void An_empty_filter_resolves_every_general_topic()
    {
        var resolution = Resolver.Resolve(null, Map(), 0,
            [Answer("design", [Topic("Colors", technology: ["ui"])])]);

        Assert.Equal(["Colors"], resolution.Topics.Select(topic => topic.Topic.Topic));
        Assert.Empty(resolution.UnknownFields);
    }

    [Fact]
    public void Answers_from_every_owner_are_merged()
    {
        var resolution = Resolver.Resolve(null, Map(), 0,
        [
            Answer("design", [Topic("Colors")]),
            Answer("platform", [Topic("Timeouts")]),
        ]);

        Assert.Equal([("design", "Colors"), ("platform", "Timeouts")],
            resolution.Topics.Select(topic => (topic.Source, topic.Topic.Topic)));
    }

    [Fact]
    public void Requested_values_that_nothing_answered_are_listed()
    {
        var resolution = Resolver.Resolve(null, Map(("area", ["branding", "accessibility"])), 0,
            [Answer("design", [Topic("Colors", area: ["branding"])], catalog: [Topic("Colors", area: ["branding", "accessibility"])])]);

        Assert.Contains("Nothing found for area = accessibility.", resolution.NotCovered);
        Assert.DoesNotContain(resolution.NotCovered, gap => gap.Contains("branding"));
    }

    [Fact]
    public void An_unavailable_owner_does_not_fail_the_others()
    {
        var resolution = Resolver.Resolve(null, Map(), 0,
            [Answer("design", [Topic("Colors")]), Unavailable("platform")]);

        Assert.Single(resolution.Topics);
        Assert.Equal("unavailable", resolution.Sources.Single(source => source.Name == "platform").Status);
    }
}
