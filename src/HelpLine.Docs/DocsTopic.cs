namespace HelpLine.Docs;

/// <summary>
/// Represents an embedded Markdown documentation topic.
/// </summary>
public sealed record DocsTopic(string Name, string DisplayName, string Description, string? ResourceName);
