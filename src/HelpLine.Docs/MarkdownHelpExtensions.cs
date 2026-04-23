using System.CommandLine;
using System.Reflection;

namespace HelpLine.Docs;

/// <summary>
/// Registers Markdown-backed help experiences for System.CommandLine commands.
/// </summary>
public static class MarkdownHelpExtensions
{
    /// <summary>
    /// Adds the <c>docs</c> subcommand for browsing embedded Markdown help topics.
    /// </summary>
    public static HelpCommand AddMarkdownHelp(this Command command, Assembly? assembly = null, MarkdownHelpRenderer? renderer = null)
    {
        ArgumentNullException.ThrowIfNull(command);

        var targetAssembly = assembly ?? Assembly.GetEntryAssembly() ?? command.GetType().Assembly;
        return command.AddMarkdownHelp(HelpTopicCatalog.FromAssembly(targetAssembly), renderer);
    }

    /// <summary>
    /// Adds the <c>docs</c> subcommand for browsing embedded Markdown help topics.
    /// </summary>
    public static HelpCommand AddMarkdownHelp(this Command command, HelpTopicCatalog catalog, MarkdownHelpRenderer? renderer = null)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(catalog);

        renderer ??= new MarkdownHelpRenderer();

        var helpCommand = new HelpCommand(catalog, renderer);

        if (!command.Subcommands.Any(existing => string.Equals(existing.Name, helpCommand.Name, StringComparison.OrdinalIgnoreCase)))
        {
            command.Subcommands.Add(helpCommand);
        }

        return helpCommand;
    }
}
