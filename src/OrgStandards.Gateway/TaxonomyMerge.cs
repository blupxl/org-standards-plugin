using OrgStandards.Contracts;

namespace OrgStandards.Gateway;

// Builds the taxonomy from the owners' answers. With a configured taxonomy owner (Gateway:TaxonomyOwner),
// that owner's categories are the taxonomy and any other owner serving categories is reported as a
// conflict and ignored. Without one, every owner's categories are merged: owners are taken in name
// order and the first definition of a category wins, so the answer is the same on every call.
public static class TaxonomyMerge
{
    // The sources to ask for their taxonomy. With a configured owner, only the owner (by its configured
    // name); if it isn't a configured source nobody is asked and the taxonomy comes back unavailable.
    public static string[] SourcesToAsk(IEnumerable<string> sources, string? taxonomyOwner) =>
        string.IsNullOrWhiteSpace(taxonomyOwner)
            ? sources.ToArray()
            : sources.Where(source => source.Equals(taxonomyOwner, StringComparison.OrdinalIgnoreCase)).ToArray();

    public static TaxonomyResult Merge(IReadOnlyList<SourceCall<TaxonomyListing>> calls, string? taxonomyOwner = null)
    {
        var statuses = calls
            .Select(call => new SourceStatus(call.Source, call.Succeeded ? "ok" : "unavailable", call.Error))
            .ToArray();

        return string.IsNullOrWhiteSpace(taxonomyOwner)
            ? MergeAll(calls, statuses)
            : FromOwner(calls, statuses, taxonomyOwner);
    }

    private static TaxonomyResult FromOwner(IReadOnlyList<SourceCall<TaxonomyListing>> calls, SourceStatus[] statuses, string taxonomyOwner)
    {
        var owner = calls.FirstOrDefault(call => call.Source.Equals(taxonomyOwner, StringComparison.OrdinalIgnoreCase));
        var conflicts = calls
            .Where(call => call != owner && call.Succeeded && call.Value is { Categories.Length: > 0 })
            .OrderBy(call => call.Source, StringComparer.Ordinal)
            .Select(call => $"{call.Source} also serves a taxonomy; only {taxonomyOwner}'s is used")
            .ToArray();

        if (owner is not { Succeeded: true, Value: not null })
        {
            return new TaxonomyResult("unavailable", [], statuses, conflicts);
        }

        return new TaxonomyResult("ok", Order(owner.Value.Categories), statuses, conflicts);
    }

    private static TaxonomyResult MergeAll(IReadOnlyList<SourceCall<TaxonomyListing>> calls, SourceStatus[] statuses)
    {
        var offered = calls
            .Where(call => call.Succeeded && call.Value is not null)
            .OrderBy(call => call.Source, StringComparer.Ordinal)
            .SelectMany(call => call.Value!.Categories.Select(category => (Owner: call.Source, Category: category)))
            .ToList();

        var categories = new List<TaxonomyCategory>();
        var conflicts = new List<string>();
        foreach (var definitions in offered.GroupBy(entry => entry.Category.Name, StringComparer.OrdinalIgnoreCase))
        {
            var kept = definitions.First();
            categories.Add(kept.Category);

            foreach (var other in definitions.Skip(1))
            {
                if (!other.Category.Facet.Equals(kept.Category.Facet, StringComparison.OrdinalIgnoreCase))
                {
                    conflicts.Add($"\"{kept.Category.Name}\" is a {kept.Category.Facet} for {kept.Owner} but a " +
                                  $"{other.Category.Facet} for {other.Owner}; {kept.Owner}'s is used");
                }
                else if (!SameMetadata(kept.Category, other.Category))
                {
                    conflicts.Add($"\"{kept.Category.Name}\" is described differently by {kept.Owner} and " +
                                  $"{other.Owner}; {kept.Owner}'s is used");
                }
            }
        }

        var status = categories.Count == 0 ? "unavailable" : calls.All(call => call.Succeeded) ? "ok" : "partial";
        return new TaxonomyResult(status, Order(categories), statuses, conflicts.ToArray());
    }

    private static bool SameMetadata(TaxonomyCategory first, TaxonomyCategory second) =>
        first.Description == second.Description
        && SameValues(first.Signals, second.Signals)
        && SameValues(first.Files, second.Files)
        && SameValues(first.Broader, second.Broader);

    private static bool SameValues(string[] first, string[] second) =>
        first.Order(StringComparer.Ordinal).SequenceEqual(second.Order(StringComparer.Ordinal));

    private static TaxonomyCategory[] Order(IEnumerable<TaxonomyCategory> categories) =>
        categories
            .OrderBy(category => category.Facet, StringComparer.Ordinal)
            .ThenBy(category => category.Name, StringComparer.Ordinal)
            .ToArray();
}
