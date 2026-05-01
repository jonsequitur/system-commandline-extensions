using System.CommandLine;
using System.Reflection;

namespace HelpLine.Docs;

/// <summary>
/// Registers Markdown-backed documentation experiences for System.CommandLine commands.
/// </summary>
public static class CommandExtensions
{
    /// <summary>
    /// Adds the <c>docs</c> subcommand for browsing embedded Markdown documentation topics.
    /// </summary>
    public static void AddDocsCommand(this Command command, Assembly? assembly = null, MarkdownHelpRenderer? renderer = null)
    {
        ArgumentNullException.ThrowIfNull(command);

        var targetAssembly = assembly ?? Assembly.GetCallingAssembly();
        command.AddDocsCommand(DocsTopicCatalog.FromAssemblyResources(targetAssembly), renderer);
    }

    /// <summary>
    /// Adds the <c>docs</c> subcommand for browsing embedded Markdown documentation topics.
    /// </summary>
    public static void AddDocsCommand(this Command command, DocsTopicCatalog catalog, MarkdownHelpRenderer? renderer = null)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(catalog);

        renderer ??= new MarkdownHelpRenderer();

        var docsCommand = new DocsCommand(catalog, renderer);

        if (!command.Subcommands.Any(existing => string.Equals(existing.Name, docsCommand.Name, StringComparison.OrdinalIgnoreCase)))
        {
            command.Subcommands.Add(docsCommand);
        }
        else
        {
            throw new InvalidOperationException("Command `docs` already present");
        }
    }
}
