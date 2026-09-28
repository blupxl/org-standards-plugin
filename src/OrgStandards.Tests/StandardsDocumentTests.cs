using OrgStandards.Contracts;
using OrgStandards.Gateway;
using static OrgStandards.Tests.TestData;

namespace OrgStandards.Tests;

// The document Claude reads. Its header is a contract: status, sources, and warnings must say how
// far the document can be trusted.
public class StandardsDocumentTests
{
    private static readonly Dictionary<string, string[]> Acme = Map(("company", ["acme"]));

    private static string Render(Dictionary<string, string[]> filter, params SourceCall<QueryResult>[] calls) =>
        StandardsDocument.Render(Resolver.Resolve(null, filter, 0, calls));

    [Fact]
    public void A_complete_answer_says_so_and_shows_its_topics()
    {
        var document = Render(Acme, Answer("design", [Topic("Colors")]));

        Assert.Contains("> **Status:** complete", document);
        Assert.Contains("> **Sources:** design ✓", document);
        Assert.Contains("## Colors", document);
        Assert.Contains("- MUST do the thing.", document);
    }

    [Fact]
    public void A_missing_company_makes_the_document_invalid_and_shows_nothing()
    {
        var document = Render(Map(("technology", ["ui"])), Answer("design", [Topic("Colors", technology: ["ui"])]));

        Assert.Contains("> **Status:** invalid", document);
        Assert.Contains("Missing required field: company", document);
        Assert.Contains("say which one: acme", document);
        Assert.DoesNotContain("## Colors", document);
        Assert.DoesNotContain("**Precedence:**", document);
    }

    [Fact]
    public void An_unknown_field_makes_the_document_invalid_and_lists_the_valid_fields()
    {
        var document = Render(Map(("company", ["acme"]), ("produkt", ["x"])), Answer("design", [Topic("Colors")]));

        Assert.Contains("> **Status:** invalid", document);
        Assert.Contains("Unknown field(s): produkt", document);
        Assert.Contains("Valid fields: company", document);
    }

    [Fact]
    public void An_unavailable_owner_makes_the_document_partial_and_names_it()
    {
        var document = Render(Acme, Answer("design", [Topic("Colors")]), Unavailable("platform"));

        Assert.Contains("> **Status:** partial", document);
        Assert.Contains("platform is unavailable", document);
        Assert.Contains("don't assume they don't exist", document);
    }

    [Fact]
    public void An_unknown_product_makes_the_document_unresolved_with_a_suggestion()
    {
        var document = Render(Map(("company", ["acme"]), ("product", ["xyz-pubic-app"])),
            Answer("design", [Topic("Colors")], catalog: [Topic("Colors"), Topic("Colors", product: "xyz-public-app")]));

        Assert.Contains("> **Status:** unresolved", document);
        Assert.Contains("Did you mean xyz-public-app?", document);
        Assert.Contains("Confirm the product before relying on this document.", document);
    }

    [Fact]
    public void A_product_topic_says_what_it_replaced()
    {
        var document = Render(Map(("company", ["acme"]), ("product", ["xyz-public-app"])),
            Answer("design", [Topic("Colors", version: "2.0"), Topic("Colors", product: "xyz-public-app")]));

        Assert.Contains("product: xyz-public-app · replaces the general topic (design v2.0)", document);
    }

    [Fact]
    public void Details_stay_out_of_the_main_document_but_are_announced()
    {
        var document = Render(Acme, Answer("design", [Topic("Colors", details: [new("Tokens", "| token | value |")])]));

        Assert.Contains("*More in this topic: Tokens. Fetch with `get_topic(\"Colors\")` and the same filter.*", document);
        Assert.DoesNotContain("| token | value |", document);
    }

    [Fact]
    public void Requested_values_nothing_answered_are_listed_as_not_covered()
    {
        var document = Render(Map(("company", ["acme"]), ("area", ["branding", "accessibility"])),
            Answer("design", [Topic("Colors", area: ["branding"])], catalog: [Topic("Colors", area: ["branding", "accessibility"])]));

        Assert.Contains("## Not covered", document);
        Assert.Contains("- Nothing found for area = accessibility.", document);
    }

    [Fact]
    public void A_topic_is_fetched_in_full_and_inherited_details_are_labeled()
    {
        var resolution = Resolver.Resolve(null, Map(("company", ["acme"]), ("product", ["xyz-public-app"])), 0,
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
        var resolution = Resolver.Resolve(null, Acme, 0, [Answer("design", [Topic("Colors"), Topic("Components")])]);

        var document = StandardsDocument.RenderTopic(resolution, "Fonts");

        Assert.Contains("no topic \"Fonts\" under this scope. Topics available: Colors, Components.", document);
    }
}
