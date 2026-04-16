namespace HelpLine.Markdown.Topics;

/// <summary>
/// Represents an embedded Markdown help topic.
/// </summary>
public sealed record HelpTopic(string Name, string DisplayName, string Description, string ResourceName);
