namespace HelpLine.Docs;

/// <summary>
/// Provides information about a Markdown heading during topic mapping.
/// </summary>
public sealed class HeadingContext
{
    private readonly HashSet<string> _topicNames = [];

    internal HeadingContext(string headingText, int headingLevel, string? parentHeadingText)
    {
        HeadingText = headingText;
        HeadingLevel = headingLevel;
        ParentHeadingText = parentHeadingText;
    }

    public string HeadingText { get; }

    public int HeadingLevel { get; }

    /// <summary>
    /// The text of the nearest ancestor heading (the most recent heading at a lower level),
    /// or <c>null</c> if this is the top-level heading in the document.
    /// </summary>
    public string? ParentHeadingText { get; }

    public void AppendToTopic(string topicName)
    {
        if (string.IsNullOrWhiteSpace(topicName))
        {
            throw new ArgumentException("Topic names must be non-empty.", nameof(topicName));
        }

        var normalized = topicName.Trim().ToLowerInvariant().Replace(' ', '-');
        _topicNames.Add(normalized);
    }

    internal IReadOnlyList<string> MappedTopicNames => _topicNames.ToList();
}
