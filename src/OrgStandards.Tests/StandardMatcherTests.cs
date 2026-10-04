using OrgStandards.Data;
using static OrgStandards.Tests.TestData;

namespace OrgStandards.Tests;

// Which topics a filter selects: OR within a field, AND across fields, product as an overlay.
public class StandardMatcherTests
{
    private static readonly Dictionary<string, string[]> UiTopic =
        Map(("technology", ["ui"]), ("area", ["branding"]));

    private static readonly Dictionary<string, string[]> XyzTopic =
        Map(("product", ["xyz-public-app"]), ("technology", ["ui"]));

    [Fact]
    public void An_empty_filter_matches_every_general_topic()
    {
        Assert.True(StandardMatcher.Matches(UiTopic, Map()));
        Assert.True(StandardMatcher.Matches(UiTopic, null));
    }

    [Fact]
    public void Values_within_a_field_are_alternatives() =>
        Assert.True(StandardMatcher.Matches(UiTopic, Map(("technology", ["web-api", "ui"]))));

    [Fact]
    public void Every_field_must_match() =>
        Assert.False(StandardMatcher.Matches(UiTopic, Map(("technology", ["ui"]), ("area", ["resilience"]))));

    [Fact]
    public void An_empty_value_list_does_not_constrain() =>
        Assert.True(StandardMatcher.Matches(UiTopic, Map(("area", []))));

    [Fact]
    public void A_general_topic_is_the_base_layer_for_a_product_request() =>
        Assert.True(StandardMatcher.Matches(UiTopic, Map(("product", ["xyz-public-app"]))));

    [Fact]
    public void A_product_topic_applies_only_when_its_product_is_requested()
    {
        Assert.False(StandardMatcher.Matches(XyzTopic, Map()));
        Assert.False(StandardMatcher.Matches(XyzTopic, Map(("product", ["acme-website"]))));
        Assert.True(StandardMatcher.Matches(XyzTopic, Map(("product", ["xyz-public-app"]))));
    }

    [Fact]
    public void Discovery_shows_product_topics_without_a_product() =>
        Assert.True(StandardMatcher.Matches(XyzTopic, Map(), discovery: true));

    private static readonly Dictionary<string, string[]> DotnetTopic =
        Map(("runtime", ["dotnet"]), ("kind", ["backend"]));

    [Fact]
    public void A_runtime_topic_applies_only_when_its_runtime_is_requested()
    {
        Assert.False(StandardMatcher.Matches(DotnetTopic, Map(("kind", ["backend"]))));
        Assert.False(StandardMatcher.Matches(DotnetTopic, Map(("kind", ["backend"]), ("runtime", ["node"]))));
        Assert.True(StandardMatcher.Matches(DotnetTopic, Map(("kind", ["backend"]), ("runtime", ["dotnet"]))));
    }

    [Fact]
    public void A_topic_without_a_runtime_applies_to_every_runtime() =>
        Assert.True(StandardMatcher.Matches(UiTopic, Map(("technology", ["ui"]), ("runtime", ["node"]))));

    [Fact]
    public void Discovery_shows_runtime_topics_without_a_runtime() =>
        Assert.True(StandardMatcher.Matches(DotnetTopic, Map(), discovery: true));

    [Fact]
    public void Fields_and_values_ignore_case() =>
        Assert.True(StandardMatcher.Matches(UiTopic, Map(("Technology", ["UI"]))));
}
