namespace HelpLine.Docs;

/// <summary>
/// Represents an embedded Markdown documentation topic.
/// </summary>
/// <param name="Name">The fully-qualified topic identifier passed to <c>--topic</c>. May be hyphen-joined with ancestor names when short names collide.</param>
/// <param name="DisplayName">The original heading text.</param>
/// <param name="Description">A short description used in topic listings.</param>
/// <param name="ResourceName">The embedded resource name, when the topic was loaded from an assembly resource.</param>
/// <param name="Level">The Markdown heading level (1 = H1) the topic was created from. Defaults to 1.</param>
/// <param name="ParentName">The <see cref="Name"/> of the nearest ancestor topic in the hierarchy, or <c>null</c> if this is a top-level topic.</param>
public sealed record DocsTopic(
    string Name,
    string DisplayName,
    string Description,
    string? ResourceName,
    int Level = 1,
    string? ParentName = null)
{
    /// <summary>
    /// The unqualified topic name (typically the normalized heading text). Used for indented display in topic chooser/list output.
    /// Defaults to <see cref="Name"/> when not specified.
    /// </summary>
    public string ShortName { get; init; } = Name;
}
