using OrgStandards.Contracts;
using OrgStandards.Gateway;
using static OrgStandards.Tests.TestData;

namespace OrgStandards.Tests;

// The document Claude reads. Its header is a contract: status, sources, and warnings must say how
// far the document can be trusted.
public class StandardsDocumentTests
{
    private static readonly Dictionary<string, string[]> NoFilter = Map();

    private static string Render(Dictionary<string, string[]> filter, params SourceCall<QueryResult>[] calls) =>
        StandardsDocument.Render(Resolver.Resolve(null, filter, 0, calls));

    [Fact]
    public void A_complete_answer_says_so_and_shows_its_topics()
    {
        var document = Render(NoFilter, Answer("design", [Topic("Colors")]));

        Assert.Contains("> **Status:** complete", document);
        Assert.Contains("> **Sources:** design ✓", document);
        Assert.Contains("## Colors", document);
        Assert.Contains("- MUST do the thing.", document);
    }

    [Fact]
    public void No_filter_means_the_general_standards()
    {
        var document = Render(NoFilter, Answer("design", [Topic("Colors", technology: ["ui"])]));

        Assert.Contains("> **Scope:** no filter (general standards only)", document);
        Assert.Contains("> **Status:** complete", document);
        Assert.Contains("## Colors", document);
    }

    [Fact]
    public void An_unknown_field_makes_the_document_invalid_and_lists_the_valid_fields()
    {
        var document = Render(Map(("produkt", ["x"])), Answer("design", [Topic("Colors", technology: ["ui"])]));

        Assert.Contains("> **Status:** invalid", document);
        Assert.Contains("Unknown field(s): produkt", document);
        Assert.Contains("Valid fields: technology", document);
        Assert.DoesNotContain("## Colors", document);
        Assert.DoesNotContain("**Precedence:**", document);
    }

    [Fact]
    public void With_no_owner_answering_nothing_is_called_unknown()
    {
        // Seen in the integration tests: with every owner timed out, every field was reported as
        // unknown ("Valid fields: ."), which blamed the filter for an outage.
        var document = Render(Map(("kind", ["ui"])), Unavailable("design"), Unavailable("platform"));

        Assert.Contains("> **Status:** unavailable", document);
        Assert.DoesNotContain("Unknown field", document);
        Assert.Contains("design is unavailable", document);
        Assert.Contains("platform is unavailable", document);
    }

    [Fact]
    public void A_runtime_topic_says_which_general_rule_it_implements()
    {
        var document = Render(NoFilter, Answer("platform", [Topic("Settings in .NET", implements: ["Settings"])]));

        Assert.Contains("· implements: Settings", document);
    }

    [Fact]
    public void An_unavailable_owner_makes_the_document_partial_and_names_it()
    {
        var document = Render(NoFilter, Answer("design", [Topic("Colors")]), Unavailable("platform"));

        Assert.Contains("> **Status:** partial", document);
        Assert.Contains("platform is unavailable", document);
        Assert.Contains("don't assume they don't exist", document);
    }

    [Fact]
    public void An_unknown_product_makes_the_document_unresolved_with_a_suggestion()
    {
        var document = Render(Map(("product", ["xyz-pubic-app"])),
            Answer("design", [Topic("Colors")], catalog: [Topic("Colors"), Topic("Colors", product: "xyz-public-app")]));

        Assert.Contains("> **Status:** unresolved", document);
        Assert.Contains("Did you mean xyz-public-app?", document);
        Assert.Contains("Confirm the product before relying on this document.", document);
    }

    [Fact]
    public void A_product_topic_says_what_it_replaced()
    {
        var document = Render(Map(("product", ["xyz-public-app"])),
            Answer("design", [Topic("Colors", version: "2.0"), Topic("Colors", product: "xyz-public-app")]));

        Assert.Contains("product: xyz-public-app · replaces the general topic (design v2.0)", document);
    }

    [Fact]
    public void Details_stay_out_of_the_main_document_but_are_announced()
    {
        var document = Render(NoFilter, Answer("design", [Topic("Colors", details: [new("Tokens", "| token | value |")])]));

        Assert.Contains("*More in this topic: Tokens. Fetch with `get_topic(\"Colors\")` and the same filter.*", document);
        Assert.DoesNotContain("| token | value |", document);
    }

    [Fact]
    public void Requested_values_nothing_answered_are_listed_as_not_covered()
    {
        var document = Render(Map(("area", ["branding", "accessibility"])),
            Answer("design", [Topic("Colors", area: ["branding"])], catalog: [Topic("Colors", area: ["branding", "accessibility"])]));

        Assert.Contains("## Not covered", document);
        Assert.Contains("- Nothing found for area = accessibility.", document);
    }

    [Fact]
    public void A_topic_is_fetched_in_full_and_inherited_details_are_labeled()
    {
        var resolution = Resolver.Resolve(null, Map(("product", ["xyz-public-app"])), 0,
        [
            Answer("platform",
            [
                Topic("Caching", details: [new("Example", "general example")]),
                Topic("Caching", product: "xyz-public-app"),
            ]),
        ]);

        var document = StandardsDocument.RenderTopic(resolution, "caching");

        Assert.Contains("# Topic: caching", document);
        Assert.Contains("### Example *(from the general topic, platform v1.0)*", document);
        Assert.Contains("general example", document);
    }

    [Fact]
    public void An_unknown_topic_lists_the_topics_that_exist()
    {
        var resolution = Resolver.Resolve(null, NoFilter, 0, [Answer("design", [Topic("Colors"), Topic("Components")])]);

        var document = StandardsDocument.RenderTopic(resolution, "Fonts");

        Assert.Contains("no topic \"Fonts\" under this scope. Topics available: Colors, Components.", document);
    }
}
