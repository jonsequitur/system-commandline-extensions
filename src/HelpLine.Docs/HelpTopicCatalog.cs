using System.Reflection;

namespace HelpLine.Docs;

/// <summary>
/// Discovers and reads embedded Markdown help topics from an assembly.
/// </summary>
public sealed class HelpTopicCatalog
{
    private const string DefaultResourceMarker = ".HelpLine.Docs.Topics.";
    private readonly Assembly _assembly;
    private readonly Dictionary<string, HelpTopic> _topicsByName;

    public HelpTopicCatalog(Assembly assembly, IEnumerable<HelpTopic> topics)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentNullException.ThrowIfNull(topics);

        _assembly = assembly;
        Topics = topics.OrderBy(static topic => topic.Name, StringComparer.OrdinalIgnoreCase).ToArray();
        _topicsByName = Topics.ToDictionary(static topic => topic.Name, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// The discovered help topics.
    /// </summary>
    public IReadOnlyList<HelpTopic> Topics { get; }

    /// <summary>
    /// Discovers help topics in the provided assembly.
    /// </summary>
    public static HelpTopicCatalog FromAssembly(Assembly assembly, string resourceMarker = DefaultResourceMarker)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var topics = assembly
            .GetManifestResourceNames()
            .Where(resourceName => resourceName.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            .Where(resourceName => resourceName.Contains(resourceMarker, StringComparison.Ordinal))
            .Select(resourceName => CreateTopic(assembly, resourceName, resourceMarker))
            .ToArray();

        return new HelpTopicCatalog(assembly, topics);
    }

    /// <summary>
    /// Tries to find a topic by name.
    /// </summary>
    public bool TryGetTopic(string? name, out HelpTopic? topic)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            topic = null;
            return false;
        }

        return _topicsByName.TryGetValue(name.Trim(), out topic);
    }

    /// <summary>
    /// Tries to read the Markdown content for the provided topic.
    /// </summary>
    public bool TryReadTopicText(HelpTopic topic, out string? markdown)
    {
        ArgumentNullException.ThrowIfNull(topic);

        using var stream = _assembly.GetManifestResourceStream(topic.ResourceName);

        if (stream is null)
        {
            markdown = null;
            return false;
        }

        using var reader = new StreamReader(stream);
        markdown = reader.ReadToEnd();
        return true;
    }

    private static HelpTopic CreateTopic(Assembly assembly, string resourceName, string resourceMarker)
    {
        var topicName = resourceName[(resourceName.IndexOf(resourceMarker, StringComparison.Ordinal) + resourceMarker.Length)..];
        topicName = topicName[..^3];
        topicName = topicName.Replace('.', '-').Trim('-');

        var displayName = topicName.Replace('-', ' ');
        var description = "Embedded Markdown help topic.";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is not null)
        {
            using var reader = new StreamReader(stream, leaveOpen: false);
            var markdown = reader.ReadToEnd();
            description = ExtractDescription(markdown, displayName);
        }

        return new HelpTopic(topicName, displayName, description, resourceName);
    }

    private static string ExtractDescription(string markdown, string fallbackDisplayName)
    {
        var lines = markdown
            .Split(["\r\n", "\n"], StringSplitOptions.None)
            .Select(static line => line.Trim())
            .Where(static line => !string.IsNullOrWhiteSpace(line))
            .ToArray();

        foreach (var line in lines)
        {
            if (line.StartsWith('#'))
            {
                continue;
            }

            return line;
        }

        return $"Display the {fallbackDisplayName} topic.";
    }
}
