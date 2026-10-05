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

    [RequiresContainerRuntimeFact]
    public async Task The_taxonomy_comes_from_its_owner_with_signals()
    {
        var taxonomy = await app.CallAsync("get_taxonomy", []);

        Assert.Matches("\"status\"\\s*:\\s*\"ok\"", taxonomy);
        Assert.Contains("\"name\":\"redis\"", taxonomy.Replace(" ", ""));
        Assert.Contains("StackExchange.Redis", taxonomy);
    }

    [RequiresContainerRuntimeFact]
    public async Task Only_the_configured_owner_serves_a_taxonomy()
    {
        var taxonomy = await app.CallAsync("get_taxonomy", []);

        Assert.Matches("\"status\"\\s*:\\s*\"ok\"", taxonomy);
        Assert.Matches("\"conflicts\"\\s*:\\s*\\[\\s*\\]", taxonomy);
    }

    [RequiresContainerRuntimeFact]
    public async Task Headlines_name_every_topic_and_only_its_first_rule()
    {
        var document = await app.CallAsync("get_standards", new()
        {
            ["requests"] = new[] { new Dictionary<string, object?> { ["filter"] = new Dictionary<string, string[]> { ["kind"] = ["api"], ["runtime"] = ["dotnet"] } } },
            ["headlines"] = true,
        });

        Assert.Contains("> **Headlines only:**", document);
        Assert.Contains("## Naming in .NET", document);
        Assert.Contains("- Names MUST follow Microsoft's C# identifier naming rules", document);
        Assert.DoesNotContain("MUST NOT name a type after how it moves data", document);
    }

    [RequiresContainerRuntimeFact]
    public async Task A_kind_also_brings_the_standards_of_its_ancestors()
    {
        // Colors is tagged css and styling, not sass. A sass request now gets it.
        var document = await app.CallAsync("get_standards", Request(new() { ["kind"] = ["sass"] }));

        Assert.Contains("## Colors", document);
        Assert.Contains("## Sass modules", document);
        Assert.Contains("> **Scope:** kind = sass", document);
        Assert.Contains("> **Also applies:** css, styling (broader kinds of the requested kind).", document);
    }

    [RequiresContainerRuntimeFact]
    public async Task A_concern_does_not_bring_its_broader_concern()
    {
        // Cache keys is a caching topic for backend code (api is a backend). Async I/O is a
        // performance-only topic for api: caching is a kind of performance, but a concern never expands.
        var document = await app.CallAsync("get_standards", Request(new() { ["kind"] = ["api"], ["concern"] = ["caching"] }));

        Assert.Contains("## Cache keys", document);
        Assert.DoesNotContain("## Async I/O", document);
    }

    [RequiresContainerRuntimeFact]
    public async Task A_topic_listed_for_a_kind_can_be_fetched_with_the_same_filter()
    {
        var topic = await app.CallAsync("get_topic", new()
        {
            ["topic"] = "Colors",
            ["filter"] = new Dictionary<string, string[]> { ["kind"] = ["sass"] },
        });

        Assert.Contains("## Colors", topic);
        Assert.Contains("> **Also applies:** css, styling", topic);
    }

    [RequiresContainerRuntimeFact]
    public async Task A_mistyped_kind_is_still_reported_as_unknown()
    {
        var document = await app.CallAsync("get_standards", Request(new() { ["kind"] = ["sas"] }));

        Assert.Contains("Unknown value: kind = \"sas\"", document);
        Assert.DoesNotContain("Also applies", document);
    }
}
