namespace HelpLine.Docs;

/// <summary>
/// Provides information about a Markdown heading during topic mapping.
/// </summary>
public sealed class HeadingContext
{
    private readonly HashSet<string> _topicNames = [];

    internal HeadingContext(string headingText, int headingLevel)
    {
        HeadingText = headingText;
        HeadingLevel = headingLevel;
    }

    public string HeadingText { get; }

    public int HeadingLevel { get; }

    public void AppendToTopic(string topicName)
    {
        if (string.IsNullOrWhiteSpace(topicName))
        {
            throw new ArgumentException("Topic names must be non-empty.", nameof(topicName));
        }

        _topicNames.Add(topicName.Trim());
    }

    internal IReadOnlyList<string> MappedTopicNames => _topicNames.ToList();
}
