using Microsoft.EntityFrameworkCore;
using OrgStandards.Contracts;

namespace OrgStandards.Data;

public static class StandardMatcher
{
    // OR within a field, AND across fields. Empty value lists don't constrain.
    // Qualifier fields are special; see StandardFields. A topic's product or runtime must be
    // requested (topics without one apply to everything). For discovery, qualified topics are
    // visible without a filter, so callers can find out which products and runtimes exist.
    public static bool Matches(
        IReadOnlyDictionary<string, string[]> tags, IReadOnlyDictionary<string, string[]>? filter, bool discovery = false)
    {
        foreach (var qualifier in StandardFields.Qualifiers)
        {
            var wanted = ValuesOf(filter, qualifier);
            var own = ValuesOf(tags, qualifier);
            if (own.Length > 0 && (wanted.Length > 0 || !discovery) && !Overlaps(own, wanted))
            {
                return false;
            }
        }

        foreach (var (field, values) in filter ?? new Dictionary<string, string[]>())
        {
            if (values.Length == 0 || IsQualifier(field))
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
            .GroupBy(tag => tag.Field, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Select(tag => tag.Value).ToArray(), StringComparer.OrdinalIgnoreCase));

    // Small, hand-written data: load everything and filter in memory.
    public static async Task<List<StandardTopic>> LoadAllAsync(StandardsDbContext database, CancellationToken cancellationToken) =>
        (await database.Topics.AsNoTracking().Include(topic => topic.Tags).OrderBy(topic => topic.Id).ToListAsync(cancellationToken))
            .Select(ToContract)
            .ToList();

    public static Dictionary<string, string[]> FieldsOf(IEnumerable<StandardTopic> topics) =>
        topics
            .SelectMany(topic => topic.Tags)
            .GroupBy(tag => tag.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.SelectMany(tag => tag.Value).Distinct(StringComparer.OrdinalIgnoreCase).Order().ToArray(),
                StringComparer.OrdinalIgnoreCase);

    private static bool IsQualifier(string field) =>
        StandardFields.Qualifiers.Contains(field, StringComparer.OrdinalIgnoreCase);

    private static string[] ValuesOf(IReadOnlyDictionary<string, string[]>? map, string field) =>
        map?.FirstOrDefault(entry => entry.Key.Equals(field, StringComparison.OrdinalIgnoreCase)).Value ?? [];

    private static bool Overlaps(string[] left, string[] right) =>
        left.Intersect(right, StringComparer.OrdinalIgnoreCase).Any();
}
