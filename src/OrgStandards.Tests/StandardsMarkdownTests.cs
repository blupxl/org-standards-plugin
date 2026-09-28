using OrgStandards.Migrations;

namespace OrgStandards.Tests;

// How an owner's Markdown file becomes topics: frontmatter tags, "##" topics, "###" details.
public sealed class StandardsMarkdownTests : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("standards-md-").FullName;

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    private List<OrgStandards.Data.Topic> Parse(string markdown, string fileName = "web-ui.md")
    {
        var path = Path.Combine(_folder, fileName);
        File.WriteAllText(path, markdown);
        return StandardsMarkdown.Parse(path);
    }

    private static string[] TagValues(OrgStandards.Data.Topic topic, string field) =>
        topic.Tags.Where(tag => tag.Field == field).Select(tag => tag.Value).ToArray();

    [Fact]
    public void Frontmatter_sets_the_document_and_every_other_key_becomes_a_tag()
    {
        var topic = Assert.Single(Parse("""
            ---
            title: Web UI
            version: 2.1
            company: acme
            technology: [web-api, ui]
            ---
            ## Colors
            - MUST use tokens.
            """));

        Assert.Equal("Web UI", topic.Document);
        Assert.Equal("2.1", topic.Version);
        Assert.Equal(["acme"], TagValues(topic, "company"));
        Assert.Equal(["web-api", "ui"], TagValues(topic, "technology"));
        Assert.Empty(TagValues(topic, "title"));
    }

    [Fact]
    public void Second_level_headings_are_topics_and_third_level_headings_are_details()
    {
        var topics = Parse("""
            ---
            title: Web UI
            ---
            Text before the first topic is ignored.

            ## Colors
            - MUST use tokens.

            Why: one place to change the palette.

            ### Tokens
            | Token | Value |

            ## Components
            - MUST use acme-button.
            """);

        Assert.Equal(["Colors", "Components"], topics.Select(t => t.Name));
        var colors = topics[0];
        Assert.Equal("- MUST use tokens.\n\nWhy: one place to change the palette.", colors.Body);
        var detail = Assert.Single(colors.Details);
        Assert.Equal("Tokens", detail.Title);
        Assert.Equal("| Token | Value |", detail.Markdown);
        Assert.DoesNotContain(topics, t => t.Body.Contains("ignored"));
    }

    [Fact]
    public void Headings_inside_code_fences_are_content()
    {
        var topic = Assert.Single(Parse("""
            ## Caching
            - MUST use the shared cache.

            ### Example
            ```bash
            ## not a topic
            ### not a detail
            ```
            """));

        var example = Assert.Single(topic.Details);
        Assert.Contains("## not a topic", example.Markdown);
        Assert.Contains("### not a detail", example.Markdown);
    }

    [Fact]
    public void Without_frontmatter_the_file_name_is_the_title()
    {
        var topic = Assert.Single(Parse("## Logging\n- MUST log structured.", fileName: "observability.md"));

        Assert.Equal("observability", topic.Document);
        Assert.Equal("unversioned", topic.Version);
    }
}
