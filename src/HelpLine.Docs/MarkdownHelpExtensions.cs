using System.CommandLine;
using System.Reflection;
using System.CommandLine.Help;

namespace HelpLine.Docs;

/// <summary>
/// Registers Markdown-backed help experiences for System.CommandLine commands.
/// </summary>
public static class MarkdownHelpExtensions
{
    /// <summary>
    /// Adds the Markdown help command and integrates topic discovery into <c>-h</c> output.
    /// </summary>
    public static HelpCommand AddMarkdownHelp(this Command command, Assembly? assembly = null, MarkdownHelpRenderer? renderer = null)
    {
        ArgumentNullException.ThrowIfNull(command);

        var targetAssembly = assembly ?? Assembly.GetEntryAssembly() ?? command.GetType().Assembly;
        return command.AddMarkdownHelp(HelpTopicCatalog.FromAssembly(targetAssembly), renderer);
    }

    /// <summary>
    /// Adds the Markdown help command and integrates topic discovery into <c>-h</c> output.
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

        var helpBuilder = new HelpBuilder(Console.IsOutputRedirected ? int.MaxValue : Console.WindowWidth);
        helpBuilder.CustomizeLayout(_ => CreateLayout(catalog));

        foreach (var current in EnumerateCommands(command))
        {
            foreach (var helpOption in current.Options.OfType<HelpOption>())
            {
                helpOption.Action = new CustomHelpAction { Builder = helpBuilder };
            }
        }

        return helpCommand;
    }

    private static IEnumerable<Command> EnumerateCommands(Command root)
    {
        yield return root;

        foreach (var subcommand in root.Subcommands)
        {
            foreach (var child in EnumerateCommands(subcommand))
            {
                yield return child;
            }
        }
    }

    private static IEnumerable<Func<HelpContext, bool>> CreateLayout(HelpTopicCatalog catalog)
    {
        yield return HelpBuilder.Default.SynopsisSection();
        yield return HelpBuilder.Default.CommandUsageSection();
        yield return HelpBuilder.Default.CommandArgumentsSection();
        yield return HelpBuilder.Default.OptionsSection();
        yield return ctx => WriteTopicsSection(ctx, catalog);
        yield return HelpBuilder.Default.SubcommandsSection();
        yield return HelpBuilder.Default.AdditionalArgumentsSection();
    }

    private static bool WriteTopicsSection(HelpContext context, HelpTopicCatalog catalog)
    {
        if (catalog.Topics.Count == 0 || context.Command.Parents.OfType<Command>().Any())
        {
            return false;
        }

        var rows = catalog.Topics
            .Select(topic => new TwoColumnHelpRow(topic.Name, topic.Description))
            .ToArray();

        if (rows.Length == 0)
        {
            return false;
        }

        context.Output.WriteLine("Help topics:");
        context.HelpBuilder.WriteColumns(rows, context);
        return true;
    }
}
