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
            ["company"] = ["acme"],
            ["product"] = ["xyz-public-app"],
            ["technology"] = ["web-api"],
        }));

        Assert.Contains("> **Status:** complete", document);
        Assert.Contains("design ✓", document);
        Assert.Contains("platform ✓", document);
        Assert.Contains("replaces the general topic (design v2.1)", document);    // Colors, from design
        Assert.Contains("replaces the general topic (platform v1.0)", document);  // Timeouts, from platform
    }

    [RequiresContainerRuntimeFact]
    public async Task A_request_without_a_company_is_invalid()
    {
        var document = await app.CallAsync("get_standards", Request(new() { ["technology"] = ["ui"] }));

        Assert.Contains("> **Status:** invalid", document);
        Assert.Contains("Missing required field: company", document);
        Assert.DoesNotContain("## ", document);
    }

    [RequiresContainerRuntimeFact]
    public async Task The_design_system_link_resolves_to_where_acme_web_actually_runs()
    {
        var topic = await app.CallAsync("get_topic", new()
        {
            ["topic"] = "Components",
            ["filter"] = new Dictionary<string, string[]> { ["company"] = ["acme"], ["technology"] = ["ui"] },
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
    public async Task Discovery_lists_the_companies_and_products()
    {
        var listing = await app.CallAsync("list_standards", new());

        Assert.Contains("\"company\":[\"acme\"]", listing.Replace(" ", ""));
        Assert.Contains("acme-website", listing);
        Assert.Contains("xyz-public-app", listing);
    }
}
