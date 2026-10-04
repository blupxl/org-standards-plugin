using OrgStandards.Contracts;
using OrgStandards.Gateway;
using static OrgStandards.Tests.TestData;

namespace OrgStandards.Tests;

// Input from MCP callers is JSON, so any list can hold nulls and any field can be null or blank.
// The tools clean it at the boundary; nothing downstream sees a null.
public class InputTests
{
    [Fact]
    public void Null_and_blank_values_are_dropped_and_the_rest_trimmed()
    {
        var cleaned = Filters.Clean(new Dictionary<string, string[]?>
        {
            ["kind"] = ["api", null!, " ", " backend "],
        });

        Assert.Equal(["api", "backend"], cleaned["kind"]);
    }

    [Fact]
    public void Fields_with_no_values_or_no_name_are_dropped()
    {
        var cleaned = Filters.Clean(new Dictionary<string, string[]?>
        {
            ["concern"] = null,
            ["product"] = [null!, ""],
            [" "] = ["api"],
            ["kind"] = ["api"],
        });

        Assert.Equal(["kind"], cleaned.Keys);
    }

    [Fact]
    public void Duplicate_values_are_kept_once() =>
        Assert.Equal(["api"], Filters.Clean(new Dictionary<string, string[]?> { ["kind"] = ["api", "API", " api"] })["kind"]);

    [Fact]
    public void A_missing_filter_is_an_empty_one() =>
        Assert.Empty(Filters.Clean((Dictionary<string, string[]>?)null));

    [Fact]
    public void Resolving_a_filter_with_null_values_does_not_throw()
    {
        // The reported crash: a null value reached "did you mean" (Resolver.Normalize).
        var filter = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase) { ["technology"] = ["ui", null!] };

        var resolution = Resolver.Resolve(null, filter, 0, [Answer("design", [Topic("Colors", technology: ["ui"])])]);

        Assert.Empty(resolution.UnknownFields);
        Assert.Empty(resolution.UnknownValues);
        Assert.Single(resolution.Topics);
    }

    [Fact]
    public void Unknown_values_with_nulls_get_suggestions_for_the_rest()
    {
        var unknown = Resolver.UnknownValues(Map(("kind", ["ap", null!])), Map(("kind", ["api"])));

        Assert.Equal(["api"], unknown["kind"]["ap"]);
    }
}
