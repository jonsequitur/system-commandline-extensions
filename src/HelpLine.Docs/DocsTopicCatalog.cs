using System.Diagnostics.CodeAnalysis;
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
        Topics = topics.ToArray();
        _topicsByName = Topics.ToDictionary(static topic => topic.Name, StringComparer.OrdinalIgnoreCase);
    }

    private DocsTopicCatalog(IEnumerable<DocsTopic> topics, IReadOnlyDictionary<string, string> inMemoryContent)
    {
        _assembly = null;
        _inMemoryContent = inMemoryContent;
        Topics = topics.ToArray();
        _topicsByName = Topics.ToDictionary(static topic => topic.Name, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// The discovered documentation topics.
    /// </summary>
    public IReadOnlyList<DocsTopic> Topics { get; }

    private const string ResourceInfix = ".HelpLine.Docs.Topics.";

    /// <summary>
    /// Discovers documentation topics in the provided assembly by parsing embedded resources as Markdown.
    /// Each heading in each document becomes a topic, and the heading hierarchy is preserved on the resulting <see cref="DocsTopic"/> records.
    /// When two topics share a short name, both are renamed to <c>{parent.Name}-{shortName}</c> so they can be selected unambiguously via <c>--topic</c>.
    /// </summary>
    public static DocsTopicCatalog FromAssemblyResources(Assembly assembly)
        => FromAssemblyResourcesCore(assembly, mapHeading: null);

    /// <summary>
    /// Discovers documentation topics in the provided assembly by parsing embedded resources as Markdown.
    /// Use the overload without <paramref name="mapHeading"/> for the default hierarchical behavior.
    /// </summary>
    public static DocsTopicCatalog FromAssemblyResources(Assembly assembly, Action<HeadingContext> mapHeading)
    {
        ArgumentNullException.ThrowIfNull(mapHeading);
        return FromAssemblyResourcesCore(assembly, mapHeading);
    }

    private static DocsTopicCatalog FromAssemblyResourcesCore(Assembly assembly, Action<HeadingContext>? mapHeading)
    {
        ArgumentNullException.ThrowIfNull(assembly);

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
    /// Builds a catalog from a Markdown document using the default hierarchical mapping: every heading becomes a topic,
    /// nested under its enclosing heading. When two topics share a short name, their <see cref="DocsTopic.Name"/> values are
    /// qualified with parent names to disambiguate.
    /// </summary>
    public static DocsTopicCatalog FromMarkdown(string markdown, string? documentName = null)
        => FromMarkdownCore(markdown, mapHeading: null, documentName);

    /// <summary>
    /// Builds a catalog by slicing a Markdown document at headings identified by <paramref name="mapHeading"/>.
    /// Each mapped topic's content runs from its heading until the next heading at the same or shallower level.
    /// A heading may map to multiple topic names, causing the same section to appear under each.
    /// </summary>
    public static DocsTopicCatalog FromMarkdown(string markdown, Action<HeadingContext> mapHeading, string? documentName = null)
    {
        ArgumentNullException.ThrowIfNull(mapHeading);
        return FromMarkdownCore(markdown, mapHeading, documentName);
    }

    private static DocsTopicCatalog FromMarkdownCore(string markdown, Action<HeadingContext>? mapHeading, string? documentName)
    {
        ArgumentNullException.ThrowIfNull(markdown);

        var document = Markdown.Parse(markdown, MarkdownHelpRenderer.Pipeline);
        return SliceMarkdown(markdown, document, mapHeading, documentName);
    }

    /// <summary>
    /// Tries to find a topic by name.
    /// </summary>
    public bool TryGetTopic(string? name, [NotNullWhen(true)] out DocsTopic? topic)
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

    private sealed class TopicEntry
    {
        public string ShortName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Level { get; set; }
        public int? ParentEntryIndex { get; set; }
        public string? ParentName { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public Block FirstBlock { get; set; } = null!;
        public Block LastBlock { get; set; } = null!;
    }

    private static DocsTopicCatalog SliceMarkdown(
        string source,
        MarkdownDocument document,
        Action<HeadingContext>? mapHeading,
        string? documentName)
    {
        var entries = new List<TopicEntry>();
        var openStack = new List<int>(); // indices into entries currently open

        foreach (var block in document)
        {
            if (block is HeadingBlock heading)
            {
                var headingText = GetHeadingText(heading);

                // Compute "would-be parent" by peeking past topics that this heading would close,
                // for the HeadingContext.ParentHeadingText callback. The actual openStack is only
                // mutated when the heading produces topics, so unmapped headings don't close prior
                // topics in the custom-mapper path (preserving the prior "until next mapped" semantics).
                var peekParentIndex = openStack.Count - 1;
                while (peekParentIndex >= 0 && entries[openStack[peekParentIndex]].Level >= heading.Level)
                {
                    peekParentIndex--;
                }
                var peekParentText = peekParentIndex >= 0 ? entries[openStack[peekParentIndex]].DisplayName : documentName;

                List<string> mappedNames;
                if (mapHeading is null)
                {
                    var normalized = NormalizeName(headingText);
                    mappedNames = normalized.Length == 0 ? [] : [normalized];
                }
                else
                {
                    var context = new HeadingContext(headingText, heading.Level, peekParentText);
                    mapHeading(context);
                    mappedNames = context.MappedTopicNames
                                         .Distinct(StringComparer.OrdinalIgnoreCase)
                                         .ToList();
                }

                if (mappedNames.Count > 0)
                {
                    while (openStack.Count > 0 && entries[openStack[^1]].Level >= heading.Level)
                    {
                        openStack.RemoveAt(openStack.Count - 1);
                    }

                    var parentIndex = openStack.Count > 0 ? openStack[^1] : (int?)null;

                    foreach (var name in mappedNames)
                    {
                        var entry = new TopicEntry
                        {
                            ShortName = name,
                            Name = name,
                            Level = heading.Level,
                            ParentEntryIndex = parentIndex,
                            ParentName = parentIndex.HasValue ? entries[parentIndex.Value].Name : null,
                            DisplayName = headingText,
                            FirstBlock = block,
                            LastBlock = block,
                        };
                        entries.Add(entry);
                        openStack.Add(entries.Count - 1);
                    }
                }
            }

            foreach (var idx in openStack)
            {
                entries[idx].LastBlock = block;
            }
        }

        // Collision handling: any short name shared across multiple entries from different headings
        // gets qualified using the parent topic chain. Entries are in document order so parents are
        // qualified before their children, ensuring children inherit the qualified parent name.
        var shortNameCounts = entries
            .GroupBy(e => e.ShortName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

        foreach (var entry in entries)
        {
            if (shortNameCounts[entry.ShortName] > 1 && entry.ParentEntryIndex.HasValue)
            {
                var parent = entries[entry.ParentEntryIndex.Value];
                entry.Name = $"{parent.Name}-{entry.ShortName}";
            }
        }

        // Update ParentName to reflect any qualification of the parent's Name.
        foreach (var entry in entries)
        {
            if (entry.ParentEntryIndex.HasValue)
            {
                entry.ParentName = entries[entry.ParentEntryIndex.Value].Name;
            }
        }

        var topics = new List<DocsTopic>();
        var content = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in entries)
        {
            var topicContent = source[entry.FirstBlock.Span.Start..(entry.LastBlock.Span.End + 1)].TrimEnd();
            var description = ExtractDescription(topicContent, entry.DisplayName);

            if (seenNames.Add(entry.Name))
            {
                content[entry.Name] = topicContent;
                topics.Add(new DocsTopic(
                    entry.Name,
                    entry.DisplayName,
                    description,
                    ResourceName: null,
                    Level: entry.Level,
                    ParentName: entry.ParentName)
                {
                    ShortName = entry.ShortName,
                });
            }
            else
            {
                // Same Name appearing twice (custom mapper assigning same name to multiple headings):
                // concatenate content into the existing topic, matching the prior merge behavior.
                content[entry.Name] = content[entry.Name] + Environment.NewLine + topicContent;
            }
        }

        return new DocsTopicCatalog(topics, content);
    }

    private static string NormalizeName(string text)
    {
        var trimmed = text.Trim();
        return trimmed.Length == 0 ? string.Empty : trimmed.ToLowerInvariant().Replace(' ', '-');
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

        var mergedTopicsByName = new Dictionary<string, DocsTopic>(StringComparer.OrdinalIgnoreCase);
        var mergedTopics = new List<DocsTopic>();
        var mergedContent = new Dictionary<string, StringBuilder>(StringComparer.OrdinalIgnoreCase);

        foreach (var catalog in catalogs)
        {
            foreach (var topic in catalog.Topics)
            {
                if (mergedTopicsByName.TryAdd(topic.Name, topic))
                {
                    mergedTopics.Add(topic);
                }

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

        return new DocsTopicCatalog(mergedTopics, content);
    }

    private static DocsTopicCatalog LoadResourceAndCreateCatalog(Assembly assembly, string resourceName, Action<HeadingContext>? mapHeading)
    {
        using var stream = assembly.GetManifestResourceStream(resourceName) ?? throw new InvalidOperationException($"Resource '{resourceName}' not found.");
        using var reader = new StreamReader(stream, leaveOpen: false);
        var markdown = reader.ReadToEnd();

        var docName = ExtractDocumentName(resourceName, assembly.GetName().Name);
        return FromMarkdownCore(markdown, mapHeading, docName);
    }

    private static string ExtractDocumentName(string resourceName, string? assemblyName)
    {
        var name = resourceName;

        if (assemblyName is not null)
        {
            var prefix = assemblyName + ResourceInfix;
            if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                name = name[prefix.Length..];
            }
        }

        if (name.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
        {
            name = name[..^3];
        }

        return name.Replace('.', ' ').Replace('-', ' ').Replace('_', ' ');
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
