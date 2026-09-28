using Microsoft.EntityFrameworkCore;
using OrgStandards.Contracts;

namespace OrgStandards.Data;

public static class StandardMatcher
{
    // OR within a field, AND across fields. Empty value lists don't constrain.
    // Two fields are special; see StandardFields. A topic's company must be requested (companies
    // never mix), and its product must be requested (general topics are the base layer). For
    // discovery, both are visible without a filter, so callers can find out which values exist.
    public static bool Matches(
        IReadOnlyDictionary<string, string[]> tags, IReadOnlyDictionary<string, string[]>? filter, bool discovery = false)
    {
        var wantedCompanies = ValuesOf(filter, StandardFields.Partition);
        if ((wantedCompanies.Length > 0 || !discovery) && !Overlaps(ValuesOf(tags, StandardFields.Partition), wantedCompanies))
        {
            return false;
        }

        var wantedProducts = ValuesOf(filter, StandardFields.Overlay);
        var ruleProducts = ValuesOf(tags, StandardFields.Overlay);
        var productFiltered = wantedProducts.Length > 0;

        if (ruleProducts.Length > 0 && (productFiltered || !discovery) && !Overlaps(ruleProducts, wantedProducts))
        {
            return false;
        }

        foreach (var (field, values) in filter ?? new Dictionary<string, string[]>())
        {
            if (values.Length == 0 ||
                field.Equals(StandardFields.Overlay, StringComparison.OrdinalIgnoreCase) ||
                field.Equals(StandardFields.Partition, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!Overlaps(ValuesOf(tags, field), values))
            {
                return false;
            }
        }

        return true;
    }

    public static StandardTopic ToContract(Topic topic) => new(
        topic.Name,
        topic.Document,
        topic.Version,
        topic.Body,
        topic.Details.ToArray(),
        topic.Tags
            .GroupBy(t => t.Field, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Select(t => t.Value).ToArray(), StringComparer.OrdinalIgnoreCase));

    // Small, hand-written data: load everything and filter in memory.
    public static async Task<List<StandardTopic>> LoadAllAsync(StandardsDbContext db, CancellationToken cancellationToken) =>
        (await db.Topics.AsNoTracking().Include(t => t.Tags).OrderBy(t => t.Id).ToListAsync(cancellationToken))
            .Select(ToContract)
            .ToList();

    public static Dictionary<string, string[]> FieldsOf(IEnumerable<StandardTopic> topics) =>
        topics
            .SelectMany(t => t.Tags)
            .GroupBy(t => t.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.SelectMany(t => t.Value).Distinct(StringComparer.OrdinalIgnoreCase).Order().ToArray(),
                StringComparer.OrdinalIgnoreCase);

    private static string[] ValuesOf(IReadOnlyDictionary<string, string[]>? map, string field) =>
        map?.FirstOrDefault(kv => kv.Key.Equals(field, StringComparison.OrdinalIgnoreCase)).Value ?? [];

    private static bool Overlaps(string[] left, string[] right) =>
        left.Intersect(right, StringComparer.OrdinalIgnoreCase).Any();
}
