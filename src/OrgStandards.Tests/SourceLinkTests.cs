using OrgStandards.Source;
using static OrgStandards.Tests.TestData;

namespace OrgStandards.Tests;

// An owner fills in {{links}} to resources it owns, so standards never hard-code an address.
public class SourceLinkTests
{
    private static readonly SourceInfo Design = new("design",
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["design-system"] = "http://localhost:5500" });

    [Fact]
    public void Links_are_filled_in_bodies_and_details()
    {
        var topic = Topic("Components",
            body: "MUST load `{{design-system}}/css/acme.css`.",
            details: [new("Example", "<link href=\"{{design-system}}/css/acme.css\">")]);

        var resolved = Design.Resolve(topic);

        Assert.Equal("MUST load `http://localhost:5500/css/acme.css`.", resolved.Body);
        Assert.Equal("<link href=\"http://localhost:5500/css/acme.css\">", resolved.Details.Single().Markdown);
    }

    [Fact]
    public void Link_names_ignore_case() =>
        Assert.Equal("http://localhost:5500/x", Design.Resolve(Topic("T", body: "{{Design-System}}/x")).Body);

    [Fact]
    public void Links_the_owner_does_not_have_are_left_visible() =>
        Assert.Equal("{{brand-portal}}/x", Design.Resolve(Topic("T", body: "{{brand-portal}}/x")).Body);

    [Fact]
    public void An_owner_without_links_returns_the_topic_unchanged()
    {
        var topic = Topic("T", body: "{{design-system}}");
        var noLinks = new SourceInfo("platform", new Dictionary<string, string>());

        Assert.Same(topic, noLinks.Resolve(topic));
    }
}
