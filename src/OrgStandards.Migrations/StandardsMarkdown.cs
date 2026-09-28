using OrgStandards.Contracts;
using OrgStandards.Data;
using YamlDotNet.Serialization;

namespace OrgStandards.Migrations;

// Reads a standards document written by an owner.
//
//   ---                         YAML frontmatter: title, version, and any other key is a tag
//   title: Web UI               (a single value or a list). Every topic in the file gets the tags.
//   version: 2.0
//   technology: [web-api, ui]
//   ---
//   ## Colors                   "##" starts a topic. The heading is the topic's name.
//   - MUST use tokens ...       The text under it is the topic's body: rules, and a "Why:" line.
//   ### Tokens                  "###" starts a detail (example, reference table), fetched on demand.
//
// Headings inside ``` or ~~~ fences don't count, so code samples can contain "#" lines.
public static class StandardsMarkdown
{
    private static readonly IDeserializer Yaml = new DeserializerBuilder().Build();

    public static List<Topic> Parse(string path)
    {
        var text = File.ReadAllText(path).Replace("\r\n", "\n");
        var (frontmatter, markdown) = SplitFrontmatter(text);

        var title = Scalar(frontmatter, "title") ?? Path.GetFileNameWithoutExtension(path);
        var version = Scalar(frontmatter, "version") ?? "unversioned";
        var tags = frontmatter
            .Where(entry => entry.Key is not ("title" or "version"))
            .SelectMany(entry => Values(entry.Value).Select(value => (Field: entry.Key, Value: value)))
            .ToList();

        var topics = new List<Topic>();
        Topic? topic = null;
        TopicDetail? detail = null;
        var buffer = new List<string>();
        var inFence = false;

        void Flush()
        {
            var content = string.Join('\n', buffer).Trim();
            buffer.Clear();
            if (topic is null)
            {
                return;
            }

            if (detail is null)
            {
                topic.Body = content;
            }
            else
            {
                topic.Details.Add(detail with { Markdown = content });
            }
        }

        foreach (var line in markdown.Split('\n'))
        {
            var trimmed = line.TrimStart();
            if (trimmed.StartsWith("```") || trimmed.StartsWith("~~~"))
            {
                inFence = !inFence;
            }

            if (!inFence && line.StartsWith("## "))
            {
                Flush();
                detail = null;
                topic = new Topic
                {
                    Name = line[3..].Trim(),
                    Document = title,
                    Version = version,
                    Body = "",
                    Tags = tags.Select(tag => new TopicTag { Field = tag.Field, Value = tag.Value }).ToList(),
                };
                topics.Add(topic);
            }
            else if (!inFence && topic is not null && line.StartsWith("### "))
            {
                Flush();
                detail = new TopicDetail(line[4..].Trim(), "");
            }
            else
            {
                buffer.Add(line);
            }
        }

        Flush();
        return topics;
    }

    private static (Dictionary<string, object> Frontmatter, string Markdown) SplitFrontmatter(string text)
    {
        if (!text.StartsWith("---\n"))
        {
            return (new Dictionary<string, object>(), text);
        }

        var end = text.IndexOf("\n---", 4, StringComparison.Ordinal);
        if (end < 0)
        {
            return (new Dictionary<string, object>(), text);
        }

        var yaml = text[4..end];
        var rest = text[(end + 4)..];
        var frontmatter = Yaml.Deserialize<Dictionary<string, object>>(yaml) ?? new Dictionary<string, object>();
        return (new Dictionary<string, object>(frontmatter, StringComparer.OrdinalIgnoreCase), rest);
    }

    private static string? Scalar(Dictionary<string, object> frontmatter, string key) =>
        frontmatter.TryGetValue(key, out var value) ? Values(value).FirstOrDefault() : null;

    private static IEnumerable<string> Values(object? value) => value switch
    {
        null => [],
        string single => [single],
        IEnumerable<object> list => list.Select(item => item?.ToString() ?? "").Where(item => item.Length > 0),
        _ => [value.ToString() ?? ""],
    };
}
