using OrgStandards.Data;
using static OrgStandards.Tests.TestData;

namespace OrgStandards.Tests;

// Which topics a filter selects: OR within a field, AND across fields, company as a partition,
// product as an overlay.
public class StandardMatcherTests
{
    private static readonly Dictionary<string, string[]> UiTopic =
        Map(("company", ["acme"]), ("technology", ["ui"]), ("area", ["branding"]));

    private static readonly Dictionary<string, string[]> XyzTopic =
        Map(("company", ["acme"]), ("product", ["xyz-public-app"]), ("technology", ["ui"]));

    [Fact]
    public void A_request_without_a_company_matches_nothing() =>
        Assert.False(StandardMatcher.Matches(UiTopic, Map(("technology", ["ui"]))));

    [Fact]
    public void Companies_never_mix()
    {
        Assert.True(StandardMatcher.Matches(UiTopic, Map(("company", ["acme"]))));
        Assert.False(StandardMatcher.Matches(UiTopic, Map(("company", ["globex"]))));
    }

    [Fact]
    public void Discovery_shows_topics_without_a_company() =>
        Assert.True(StandardMatcher.Matches(UiTopic, Map(("technology", ["ui"])), discovery: true));

    [Fact]
    public void Values_within_a_field_are_alternatives() =>
        Assert.True(StandardMatcher.Matches(UiTopic, Map(("company", ["acme"]), ("technology", ["web-api", "ui"]))));

    [Fact]
    public void Every_field_must_match() =>
        Assert.False(StandardMatcher.Matches(UiTopic, Map(("company", ["acme"]), ("technology", ["ui"]), ("area", ["resilience"]))));

    [Fact]
    public void An_empty_value_list_does_not_constrain() =>
        Assert.True(StandardMatcher.Matches(UiTopic, Map(("company", ["acme"]), ("area", []))));

    [Fact]
    public void A_general_topic_is_the_base_layer_for_a_product_request() =>
        Assert.True(StandardMatcher.Matches(UiTopic, Map(("company", ["acme"]), ("product", ["xyz-public-app"]))));

    [Fact]
    public void A_product_topic_applies_only_when_its_product_is_requested()
    {
        Assert.False(StandardMatcher.Matches(XyzTopic, Map(("company", ["acme"]))));
        Assert.False(StandardMatcher.Matches(XyzTopic, Map(("company", ["acme"]), ("product", ["acme-website"]))));
        Assert.True(StandardMatcher.Matches(XyzTopic, Map(("company", ["acme"]), ("product", ["xyz-public-app"]))));
    }

    [Fact]
    public void Discovery_shows_product_topics_without_a_product() =>
        Assert.True(StandardMatcher.Matches(XyzTopic, Map(("company", ["acme"])), discovery: true));

    [Fact]
    public void Fields_and_values_ignore_case() =>
        Assert.True(StandardMatcher.Matches(UiTopic, Map(("Company", ["ACME"]), ("Technology", ["UI"]))));
}
