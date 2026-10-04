using OrgStandards.Contracts;

namespace OrgStandards.Gateway;

// Filter validation, the product overlay, exclusions, and gap reporting. Pure functions, no I/O.
public static class Resolver
{
    // In an exclusion, the field that names topics rather than tags.
    public const string TopicField = "topic";

    public static Resolution Resolve(
        string? id, Dictionary<string, string[]> filter, int index, SourceCall<QueryResult>[] calls,
        Dictionary<string, string[]>? exclude = null)
    {
        filter = Filters.Clean(filter);
        var exclusions = Filters.Clean(exclude);
        var available = calls.Where(call => call.Succeeded && call.Value is not null).ToList();
        var catalog = MergeFields(available.Select(call => call.Value!.Catalog.Fields));
        var sources = calls.Select(call => new SourceStatus(call.Source, call.Succeeded ? "ok" : "unavailable", call.Error)).ToArray();

        // Fields can only be checked against what some owner knows. With none answering, nothing is
        // "unknown": the document reports the outage instead of blaming the filter.
        var unknownFields = available.Count == 0
            ? []
            : UnknownFields(filter, catalog)
                .Concat(UnknownFields(exclusions, catalog).Where(field => !field.Equals(TopicField, StringComparison.OrdinalIgnoreCase)))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        if (unknownFields.Length > 0)
        {
            return new Resolution(id, filter, sources, [], [], unknownFields, catalog.Keys.Order().ToArray(), [], [], []);
        }

        var matches = available
            .Where(call => index < call.Value!.Results.Length)
            .SelectMany(call => call.Value!.Results[index].Select(topic => (call.Source, topic)));

        var (resolved, conflicts) = Overlay(matches);
        var unknownValues = UnknownValues(filter, catalog);

        // Exclusions apply to the final topics. Gaps are judged before them: a value answered only by
        // an excluded topic was answered, not missing.
        var excluded = resolved
            .Select(topic => (Topic: topic, Reason: ExcludedBy(topic.Topic, exclusions)))
            .Where(pair => pair.Reason is not null)
            .ToList();
        var kept = resolved.Except(excluded.Select(pair => pair.Topic)).ToArray();

        return new Resolution(id, filter, sources, kept, conflicts, [], [], unknownValues,
            NotCovered(filter, resolved, unknownValues),
            excluded.Select(pair => new ExcludedTopic(pair.Topic.Topic.Topic, pair.Topic.Source, pair.Reason!)).ToArray());
    }

    // Which exclusion matches a topic ("topic", or "field = values"), or null if none does.
    private static string? ExcludedBy(StandardTopic topic, Dictionary<string, string[]> exclusions)
    {
        foreach (var (field, values) in exclusions)
        {
            if (field.Equals(TopicField, StringComparison.OrdinalIgnoreCase))
            {
                if (values.Contains(topic.Topic, StringComparer.OrdinalIgnoreCase))
                {
                    return TopicField;
                }

                continue;
            }

            var own = topic.Tags.FirstOrDefault(tag => tag.Key.Equals(field, StringComparison.OrdinalIgnoreCase)).Value ?? [];
            var hit = own.Intersect(values, StringComparer.OrdinalIgnoreCase).ToArray();
            if (hit.Length > 0)
            {
                return $"{field} = {string.Join(", ", hit)}";
            }
        }

        return null;
    }

    public static Dictionary<string, string[]> MergeFields(IEnumerable<Dictionary<string, string[]>> fieldSets) =>
        fieldSets
            .SelectMany(fields => fields)
            .GroupBy(field => field.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.SelectMany(field => field.Value).Distinct(StringComparer.OrdinalIgnoreCase).Order().ToArray(),
                StringComparer.OrdinalIgnoreCase);

    public static string[] UnknownFields(Dictionary<string, string[]>? filter, Dictionary<string, string[]> catalog) =>
        (filter ?? []).Keys.Where(field => !catalog.ContainsKey(field)).ToArray();

    // Values no source knows, each with close matches ("did you mean").
    public static Dictionary<string, Dictionary<string, string[]>> UnknownValues(
        Dictionary<string, string[]> filter, Dictionary<string, string[]> catalog)
    {
        filter = Filters.Clean(filter);
        var unknown = new Dictionary<string, Dictionary<string, string[]>>(StringComparer.OrdinalIgnoreCase);

        foreach (var (field, values) in filter)
        {
            if (!catalog.TryGetValue(field, out var known))
            {
                continue;
            }

            var missing = values
                .Where(value => !known.Contains(value, StringComparer.OrdinalIgnoreCase))
                .ToDictionary(value => value, value => Suggest(value, known));

            if (missing.Count > 0)
            {
                unknown[field] = missing;
            }
        }

        return unknown;
    }

    // A product topic replaces the general topic with the same name. Its details replace the
    // general topic's details by title; details it doesn't define are inherited. The same topic
    // from two owners in the same layer is kept twice and reported as a conflict, never picked.
    public static (ResolvedTopic[] Topics, string[] Conflicts) Overlay(IEnumerable<(string Source, StandardTopic Topic)> matches)
    {
        var resolved = new List<ResolvedTopic>();
        var conflicts = new List<string>();

        foreach (var group in matches.GroupBy(match => match.Topic.Topic, StringComparer.OrdinalIgnoreCase))
        {
            var specific = group.Where(match => ProductOf(match.Topic) is not null).ToList();
            var general = group.Where(match => ProductOf(match.Topic) is null).ToList();
            var winners = specific.Count > 0 ? specific : general;

            if (winners.Count > 1)
            {
                conflicts.Add($"\"{group.Key}\" is defined by {string.Join(" and ", winners.Select(w => w.Source).Distinct())}; each version is shown");
            }

            var replaces = specific.Count > 0 && general.Count > 0
                ? new Replaced(general[0].Source, general[0].Topic.Version)
                : null;

            var inheritable = specific.Count > 0 && general.Count > 0 ? general[0].Topic.Details : [];

            foreach (var winner in winners)
            {
                var inherited = inheritable
                    .Where(detail => !winner.Topic.Details.Any(own => own.Title.Equals(detail.Title, StringComparison.OrdinalIgnoreCase)))
                    .ToArray();

                resolved.Add(new ResolvedTopic(
                    winner.Topic with { Details = [.. winner.Topic.Details, .. inherited] },
                    winner.Source,
                    ProductOf(winner.Topic),
                    replaces,
                    inherited.Select(detail => detail.Title).ToArray()));
            }
        }

        return (resolved.ToArray(), conflicts.ToArray());
    }

    // Requested values that nothing answered, so the agent doesn't read silence as permission.
    private static string[] NotCovered(
        Dictionary<string, string[]> filter, ResolvedTopic[] topics, Dictionary<string, Dictionary<string, string[]>> unknownValues)
    {
        var gaps = new List<string>();

        foreach (var (field, values) in filter)
        {
            foreach (var value in values)
            {
                if (unknownValues.TryGetValue(field, out var unknown) && unknown.ContainsKey(value))
                {
                    continue;
                }

                var answered = topics.Any(topic =>
                    topic.Topic.Tags.Any(tag => tag.Key.Equals(field, StringComparison.OrdinalIgnoreCase) &&
                                                tag.Value.Contains(value, StringComparer.OrdinalIgnoreCase)));

                if (!answered)
                {
                    gaps.Add(StandardFields.Qualifiers.Contains(field, StringComparer.OrdinalIgnoreCase)
                        ? $"No {field}-specific standards for {field} = {value}; the general standards below apply."
                        : $"Nothing found for {field} = {value}.");
                }
            }
        }

        return gaps.ToArray();
    }

    private static string[]? ProductOf(StandardTopic topic) =>
        topic.Tags.FirstOrDefault(tag => tag.Key.Equals(StandardFields.Overlay, StringComparison.OrdinalIgnoreCase)).Value is { Length: > 0 } products
            ? products
            : null;

    private static string[] Suggest(string value, string[] known)
    {
        var wanted = Normalize(value);
        return known
            .Select(candidate => (Candidate: candidate, Normalized: Normalize(candidate)))
            .Where(c => c.Normalized.Contains(wanted) || wanted.Contains(c.Normalized) ||
                        Distance(c.Normalized, wanted) <= Math.Max(2, wanted.Length / 4))
            .OrderBy(c => Distance(c.Normalized, wanted))
            .Take(3)
            .Select(c => c.Candidate)
            .ToArray();
    }

    private static string Normalize(string value) =>
        new(value.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());

    private static int Distance(string a, string b)
    {
        var previous = Enumerable.Range(0, b.Length + 1).ToArray();
        for (var i = 1; i <= a.Length; i++)
        {
            var current = new int[b.Length + 1];
            current[0] = i;
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), previous[j - 1] + cost);
            }
            previous = current;
        }
        return previous[b.Length];
    }
}
