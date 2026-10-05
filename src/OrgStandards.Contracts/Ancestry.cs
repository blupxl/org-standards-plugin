namespace OrgStandards.Contracts;

// What the taxonomy's `broader` means (REQ-FACTS). For kind, work in a category is also work in each
// of its ancestors, so a request for sass also matches topics tagged css and styling. No other
// facet expands: concern narrows a request, and qualifiers have no ancestry. Parents that don't
// exist, or are in another facet, are ignored here and caught by the taxonomy's tests.
public static class Ancestry
{
    public static Dictionary<string, string[]> Expand(
        IReadOnlyDictionary<string, string[]> filter, IEnumerable<TaxonomyCategory> categories)
    {
        var expanded = new Dictionary<string, string[]>(filter, StringComparer.OrdinalIgnoreCase);
        if (!expanded.TryGetValue(StandardFields.Kind, out var kinds))
        {
            return expanded;
        }

        var parents = categories
            .Where(category => category.Facet.Equals(StandardFields.Kind, StringComparison.OrdinalIgnoreCase))
            .GroupBy(category => category.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Broader, StringComparer.OrdinalIgnoreCase);

        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var ordered = new List<string>();
        var pending = new Queue<string>(kinds);
        while (pending.TryDequeue(out var kind))
        {
            if (!visited.Add(kind))
            {
                continue;
            }

            ordered.Add(kind);
            foreach (var parent in parents.GetValueOrDefault(kind, []).Where(parents.ContainsKey))
            {
                pending.Enqueue(parent);
            }
        }

        expanded[StandardFields.Kind] = ordered.ToArray();
        return expanded;
    }

    public static string[] Added(IReadOnlyDictionary<string, string[]> requested, IReadOnlyDictionary<string, string[]> expanded)
    {
        var asked = requested.TryGetValue(StandardFields.Kind, out var values) ? values : [];
        return (expanded.TryGetValue(StandardFields.Kind, out var all) ? all : [])
            .Where(kind => !asked.Contains(kind, StringComparer.OrdinalIgnoreCase))
            .ToArray();
    }
}
