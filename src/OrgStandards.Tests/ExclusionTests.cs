using OrgStandards.Contracts;
using OrgStandards.Gateway;
using static OrgStandards.Tests.TestData;

namespace OrgStandards.Tests;

// Exclusions come from a project's .claude/standards.json, set by its architects. Excluded topics
// are removed from the answer and listed, so they're never mistaken for rules that passed.
public class ExclusionTests
{
    private static readonly SourceCall<QueryResult>[] Design =
        [Answer("design", [Topic("Colors", area: ["branding"]), Topic("Forms", area: ["ux"])])];

    [Fact]
    public void Topics_with_an_excluded_value_are_removed_and_listed()
    {
        var resolution = Resolver.Resolve(null, Map(), 0, Design, exclude: Map(("area", ["branding"])));

        Assert.Equal(["Forms"], resolution.Topics.Select(topic => topic.Topic.Topic));
        var excluded = Assert.Single(resolution.Excluded);
        Assert.Equal(("Colors", "design", "area = branding"), (excluded.Topic, excluded.Source, excluded.Reason));
    }

    [Fact]
    public void A_single_topic_can_be_excluded_by_name()
    {
        var resolution = Resolver.Resolve(null, Map(), 0, Design, exclude: Map(("topic", ["forms"])));

        Assert.Equal(["Colors"], resolution.Topics.Select(topic => topic.Topic.Topic));
        Assert.Equal("topic", Assert.Single(resolution.Excluded).Reason);
    }

    [Fact]
    public void An_unknown_exclusion_field_makes_the_request_invalid()
    {
        var resolution = Resolver.Resolve(null, Map(), 0, Design, exclude: Map(("aera", ["branding"])));

        Assert.Equal(["aera"], resolution.UnknownFields);
        Assert.Empty(resolution.Topics);
    }

    [Fact]
    public void A_value_answered_only_by_an_excluded_topic_is_not_reported_as_missing()
    {
        var resolution = Resolver.Resolve(null, Map(("area", ["branding"])), 0, Design, exclude: Map(("topic", ["Colors"])));

        Assert.DoesNotContain(resolution.NotCovered, gap => gap.Contains("branding"));
    }

    [Fact]
    public void The_document_lists_exclusions_and_says_they_are_not_passes()
    {
        var document = StandardsDocument.Render(Resolver.Resolve(null, Map(), 0, Design, exclude: Map(("area", ["branding"]))));

        Assert.Contains("> **Excluded by the project's scope:** Colors (design, area = branding)", document);
        Assert.Contains("not checked", document);
        Assert.DoesNotContain("## Colors", document);
        Assert.Contains("## Forms", document);
    }

    [Fact]
    public void Fetching_an_excluded_topic_says_it_is_excluded()
    {
        var document = StandardsDocument.RenderTopic(
            Resolver.Resolve(null, Map(), 0, Design, exclude: Map(("area", ["branding"]))), "Colors");

        Assert.Contains("\"Colors\" is excluded by the project's scope (area = branding)", document);
    }

    [Fact]
    public void Exclusions_are_cleaned_like_filters() =>
        Assert.Empty(Resolver.Resolve(null, Map(), 0, Design, exclude: new Dictionary<string, string[]> { ["area"] = [null!, " "] }).Excluded);
}
