namespace OrgStandards.Tests.Integration;

// The whole chain, as Claude Code sees it: MCP calls to the gateway, which fans out to the owners,
// whose standards were loaded from the seed Markdown by the migrations.
[Collection(StandardsAppCollection.Name)]
[Trait("Category", "Integration")]
public class GatewayIntegrationTests(StandardsAppFixture app)
{
    private static Dictionary<string, object?> Request(Dictionary<string, string[]> filter) =>
        new() { ["requests"] = new[] { new Dictionary<string, object?> { ["filter"] = filter } } };

    [RequiresContainerRuntimeFact]
    public async Task Every_owner_answers_and_the_product_overlay_applies_across_them()
    {
        var document = await app.CallAsync("get_standards", Request(new()
        {
            ["product"] = ["xyz-public-app"],
            ["kind"] = ["css", "backend"],
        }));

        Assert.Contains("> **Status:** complete", document);
        Assert.Contains("design ✓", document);
        Assert.Contains("platform ✓", document);
        Assert.Contains("security ✓", document);
        Assert.Contains("replaces the general topic (design v2.2)", document);    // Colors, from design
        Assert.Contains("replaces the general topic (platform v1.2)", document);  // Timeouts, from platform
    }

    [RequiresContainerRuntimeFact]
    public async Task Each_request_in_a_batch_qualifies_its_kind_with_its_own_concerns()
    {
        // Security for the API, performance for the browser code: one call, two documents.
        var documents = await app.CallAsync("get_standards", new()
        {
            ["requests"] = new[]
            {
                new Dictionary<string, object?> { ["id"] = "public-api", ["filter"] = new Dictionary<string, string[]> { ["kind"] = ["api"], ["concern"] = ["security"] } },
                new Dictionary<string, object?> { ["id"] = "web", ["filter"] = new Dictionary<string, string[]> { ["kind"] = ["frontend"], ["concern"] = ["performance"] } },
            },
        });

        var parts = documents.Split("> **Request:** ");
        var api = Assert.Single(parts, part => part.StartsWith("public-api"));
        var web = Assert.Single(parts, part => part.StartsWith("web"));

        Assert.Contains("## Authorization", api);
        Assert.DoesNotContain("## Content Security Policy", api);
        Assert.DoesNotContain("## Bundle budget", api);

        Assert.Contains("## Bundle budget", web);
        Assert.DoesNotContain("## Content Security Policy", web);
        Assert.DoesNotContain("## Authorization", web);
    }

    [RequiresContainerRuntimeFact]
    public async Task Exclusions_leave_topics_out_and_list_them()
    {
        // A project that keeps its own look: its .claude/standards.json excludes branding.
        var document = await app.CallAsync("get_standards", new()
        {
            ["requests"] = new[]
            {
                new Dictionary<string, object?>
                {
                    ["filter"] = new Dictionary<string, string[]> { ["kind"] = ["css"] },
                    ["exclude"] = new Dictionary<string, string[]> { ["concern"] = ["branding"] },
                },
            },
        });

        Assert.Contains("> **Excluded by the project's scope:** Colors (design, concern = branding)", document);
        Assert.DoesNotContain("## Colors", document);
        Assert.Contains("## Spacing", document);   // css without a branding concern still applies
    }

    [RequiresContainerRuntimeFact]
    public async Task A_company_filter_is_an_unknown_field()
    {
        // One deployment serves one company, so "company" isn't a field. A filter from an older
        // project setup that still names it is rejected, with the valid fields listed.
        var document = await app.CallAsync("get_standards", Request(new() { ["company"] = ["acme"] }));

        Assert.Contains("> **Status:** invalid", document);
        Assert.Contains("Unknown field(s): company", document);
        Assert.DoesNotContain("## ", document);
    }

    [RequiresContainerRuntimeFact]
    public async Task The_design_system_link_resolves_to_where_acme_web_actually_runs()
    {
        var topic = await app.CallAsync("get_topic", new()
        {
            ["topic"] = "Components",
            ["filter"] = new Dictionary<string, string[]> { ["kind"] = ["ui"] },
        });

        var stylesheet = $"{app.DesignSystem.ToString().TrimEnd('/')}/css/acme.css";
        Assert.Contains(stylesheet, topic);
        Assert.DoesNotContain("{{", topic);

        using var http = app.CreateHttpClient("acme-web");
        using var response = await http.GetAsync("/css/acme.css");
        Assert.True(response.IsSuccessStatusCode, $"GET {stylesheet} returned {(int)response.StatusCode}");
        Assert.Equal("text/css", response.Content.Headers.ContentType?.MediaType);
    }

    [RequiresContainerRuntimeFact]
    public async Task Discovery_lists_the_products()
    {
        var listing = await app.CallAsync("list_standards", new());

        Assert.DoesNotContain("\"company\"", listing);
        Assert.Contains("acme-website", listing);
        Assert.Contains("xyz-public-app", listing);
    }
}
