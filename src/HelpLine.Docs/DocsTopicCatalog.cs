using System.Reflection;
using System.Text;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace HelpLine.Docs;

/// <summary>
/// Discovers and reads embedded Markdown documentation topics from an assembly.
/// </summary>
public sealed class DocsTopicCatalog
{
    private readonly Assembly? _assembly;
    private readonly Dictionary<string, DocsTopic> _topicsByName;
    private readonly IReadOnlyDictionary<string, string> _inMemoryContent;

    public DocsTopicCatalog(Assembly assembly, IEnumerable<DocsTopic> topics)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentNullException.ThrowIfNull(topics);

        _assembly = assembly;
        _inMemoryContent = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        Topics = topics.OrderBy(static topic => topic.Name, StringComparer.OrdinalIgnoreCase).ToArray();
        _topicsByName = Topics.ToDictionary(static topic => topic.Name, StringComparer.OrdinalIgnoreCase);
    }

    private DocsTopicCatalog(IEnumerable<DocsTopic> topics, IReadOnlyDictionary<string, string> inMemoryContent)
    {
        _assembly = null;
        _inMemoryContent = inMemoryContent;
        Topics = topics.OrderBy(static topic => topic.Name, StringComparer.OrdinalIgnoreCase).ToArray();
        _topicsByName = Topics.ToDictionary(static topic => topic.Name, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// The discovered documentation topics.
    /// </summary>
    public IReadOnlyList<DocsTopic> Topics { get; }

    private const string ResourceInfix = ".HelpLine.Docs.Topics.";

    /// <summary>
    /// Discovers documentation topics in the provided assembly by parsing embedded resources as Markdown.
    /// Finds embedded resources matching the HelpLine.Docs.Topics convention.
    /// Each resource is parsed as Markdown, with topics created based on the provided heading mapper.
    /// </summary>
    public static DocsTopicCatalog FromAssemblyResources(Assembly assembly, Action<HeadingContext> mapHeading)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentNullException.ThrowIfNull(mapHeading);

        var prefix = assembly.GetName().Name + ResourceInfix;
        var resourceNames = assembly
                           .GetManifestResourceNames()
                           .Where(resourceName => resourceName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                                                  && resourceName.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                           .ToArray();

        if (resourceNames.Length == 0)
        {
            throw new InvalidOperationException(
                $"Assembly '{assembly.GetName().Name}' does not contain any embedded Markdown documentation resources. " +
                $"Expected resources with prefix '{prefix}'.");
        }

        var mergedCatalogs = resourceNames
                            .Select(resourceName => LoadResourceAndCreateCatalog(assembly, resourceName, mapHeading))
                            .Where(catalog => catalog.Topics.Count > 0)
                            .ToArray();

        if (mergedCatalogs.Length == 0)
        {
            throw new InvalidOperationException(
                $"No documentation topics found in assembly '{assembly.GetName().Name}'.");
        }

        return mergedCatalogs.Length == 1 ? mergedCatalogs[0] : Merge(mergedCatalogs);
    }

    /// <summary>
    /// Discovers documentation topics in the provided assembly by parsing embedded resources as Markdown.
    /// Finds embedded resources matching the HelpLine.Docs.Topics convention.
    /// Each resource is parsed as Markdown, with topics created from headings at the specified level.
    /// </summary>
    public static DocsTopicCatalog FromAssemblyResourcesByHeadingLevel(Assembly assembly, int topicHeadingLevel)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        if (topicHeadingLevel < 1 || topicHeadingLevel > 6)
            throw new ArgumentOutOfRangeException(nameof(topicHeadingLevel), "Heading level must be between 1 and 6.");

        return FromAssemblyResources(assembly, context =>
        {
            if (context.HeadingLevel != topicHeadingLevel)
            {
                return;
            }

            var trimmed = context.HeadingText.Trim();
            if (!string.IsNullOrEmpty(trimmed))
            {
                context.AppendToTopic(trimmed.ToLowerInvariant().Replace(' ', '-'));
            }
        });
    }

    /// <summary>
    /// Builds a catalog by slicing a Markdown document at headings of the specified level.
    /// Each heading at the target level begins a new topic section; its content runs until the next heading.
    /// Sub-headings are included in the parent topic's content. Topic names are derived from heading text
    /// (lowercase, spaces replaced with hyphens).
    /// </summary>
    public static DocsTopicCatalog FromMarkdownByHeadingLevel(string markdown, int topicHeadingLevel)
    {
        ArgumentNullException.ThrowIfNull(markdown);
        if (topicHeadingLevel < 1 || topicHeadingLevel > 6)
            throw new ArgumentOutOfRangeException(nameof(topicHeadingLevel), "Heading level must be between 1 and 6.");

        return FromMarkdown(markdown, context =>
        {
            if (context.HeadingLevel != topicHeadingLevel)
            {
                return;
            }

            var trimmed = context.HeadingText.Trim();
            if (!string.IsNullOrEmpty(trimmed))
            {
                context.AppendToTopic(trimmed.ToLowerInvariant().Replace(' ', '-'));
            }
        });
    }

    /// <summary>
    /// Builds a catalog by slicing a Markdown document at headings identified by <paramref name="mapHeading"/>.
    /// Each mapped heading begins a new topic section; its content runs until the next mapped heading.
    /// A heading may map to multiple topic names, causing the same section to appear under each.
    /// </summary>
    public static DocsTopicCatalog FromMarkdown(string markdown, Action<HeadingContext> mapHeading)
    {
        ArgumentNullException.ThrowIfNull(markdown);
        ArgumentNullException.ThrowIfNull(mapHeading);

        var document = Markdown.Parse(markdown, MarkdownHelpRenderer.Pipeline);
        return FromMarkdown(markdown, document, mapHeading);
    }

    /// <summary>
    /// Tries to find a topic by name.
    /// </summary>
    public bool TryGetTopic(string? name, out DocsTopic? topic)
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
    public bool TryReadTopicText(DocsTopic topic, out string? markdown)
    {
        ArgumentNullException.ThrowIfNull(topic);

        if (_inMemoryContent.TryGetValue(topic.Name, out markdown))
        {
            return true;
        }

        if (_assembly is null || string.IsNullOrEmpty(topic.ResourceName))
        {
            markdown = null;
            return false;
        }

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

    /// <summary>
    /// Internal method for slicing Markdown with a custom mapper. Use <see cref="FromMarkdownByHeadingLevel"/> for the public API.
    /// </summary>
    private static DocsTopicCatalog FromMarkdown(
        string source, 
        MarkdownDocument document, 
        Action<HeadingContext> mapHeading)
    {
        var sectionBlocks = new Dictionary<string, List<Block>>(StringComparer.OrdinalIgnoreCase);
        var currentTopicNames = new List<string>();
        var pendingBlocks = new List<Block>();

        foreach (var block in document)
        {
            if (block is HeadingBlock heading)
            {
                var context = new HeadingContext(GetHeadingText(heading), heading.Level);
                mapHeading(context);

                if (context.MappedTopicNames.Count > 0)
                {
                    FlushPending(sectionBlocks, currentTopicNames, pendingBlocks);
                    currentTopicNames = context.MappedTopicNames
                                               .Distinct(StringComparer.OrdinalIgnoreCase)
                                               .ToList();
                    pendingBlocks = [block];
                    continue;
                }
            }

            pendingBlocks.Add(block);
        }

        FlushPending(sectionBlocks, currentTopicNames, pendingBlocks);

        var topics = new List<DocsTopic>();
        var content = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (topicName, blocks) in sectionBlocks)
        {
            if (blocks.Count == 0)
            {
                continue;
            }

            var topicContent = source[blocks[0].Span.Start..(blocks[^1].Span.End + 1)].TrimEnd();
            var displayName = blocks[0] is HeadingBlock h
                                  ? GetHeadingText(h)
                                  : topicName.Replace('-', ' ');
            var description = ExtractDescription(topicContent, displayName);

            content[topicName] = topicContent;
            topics.Add(new DocsTopic(topicName, displayName, description, ResourceName: null));
        }

        return new DocsTopicCatalog(topics, content);
    }

    private static void FlushPending(
        Dictionary<string, List<Block>> sectionBlocks,
        List<string> topicNames,
        List<Block> pending)
    {
        foreach (var topicName in topicNames)
        {
            if (!sectionBlocks.TryGetValue(topicName, out var blocks))
            {
                sectionBlocks[topicName] = blocks = [];
            }

            blocks.AddRange(pending);
        }
    }

    private static string GetHeadingText(HeadingBlock heading)
    {
        if (heading.Inline is null)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        AppendInlineText(sb, heading.Inline);
        return sb.ToString();
    }

    private static void AppendInlineText(StringBuilder sb, ContainerInline container)
    {
        foreach (var inline in container)
        {
            switch (inline)
            {
                case LiteralInline literal:
                    sb.Append(literal.Content.ToString());
                    break;
                case CodeInline code:
                    sb.Append(code.Content);
                    break;
                case ContainerInline child:
                    AppendInlineText(sb, child);
                    break;
            }
        }
    }

    /// <summary>
    /// Merges multiple catalogs into one. Topics with the same name have their content concatenated.
    /// </summary>
    public static DocsTopicCatalog Merge(params DocsTopicCatalog[] catalogs)
    {
        ArgumentNullException.ThrowIfNull(catalogs);

        var mergedTopics = new Dictionary<string, DocsTopic>(StringComparer.OrdinalIgnoreCase);
        var mergedContent = new Dictionary<string, StringBuilder>(StringComparer.OrdinalIgnoreCase);

        foreach (var catalog in catalogs)
        {
            foreach (var topic in catalog.Topics)
            {
                mergedTopics.TryAdd(topic.Name, topic);

                if (catalog.TryReadTopicText(topic, out var text) && text is not null)
                {
                    if (!mergedContent.TryGetValue(topic.Name, out var sb))
                    {
                        mergedContent[topic.Name] = sb = new StringBuilder();
                    }

                    if (sb.Length > 0)
                    {
                        sb.AppendLine();
                    }

                    sb.Append(text);
                }
            }
        }

        var content = mergedContent.ToDictionary(
            kv => kv.Key,
            kv => kv.Value.ToString(),
            StringComparer.OrdinalIgnoreCase);

        return new DocsTopicCatalog(mergedTopics.Values, content);
    }

    private static DocsTopicCatalog LoadResourceAndCreateCatalog(Assembly assembly, string resourceName, Action<HeadingContext> mapHeading)
    {
        using var stream = assembly.GetManifestResourceStream(resourceName) ?? throw new InvalidOperationException($"Resource '{resourceName}' not found.");
        using var reader = new StreamReader(stream, leaveOpen: false);
        var markdown = reader.ReadToEnd();

        return FromMarkdown(markdown, mapHeading);
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