using System.CommandLine;
using Spectre.Console;

namespace HelpLine.FancyHelp;

/// <summary>
/// Default help sections rendered with Spectre.Console markup.
/// </summary>
public static class DefaultLayout
{
    /// <summary>
    /// Gets the default set of sections for colorized help output.
    /// </summary>
    public static IEnumerable<Func<FancyHelpContext, bool>> GetLayout()
    {
        yield return SynopsisSection();
        yield return UsageSection();
        yield return ArgumentsSection();
        yield return OptionsSection();
        yield return SubcommandsSection();
    }

    /// <summary>
    /// Writes the command description.
    /// </summary>
    public static Func<FancyHelpContext, bool> SynopsisSection() => ctx =>
    {
        if (string.IsNullOrWhiteSpace(ctx.Command.Description))
        {
            return false;
        }

        ctx.Console.MarkupLine($"[bold]Description:[/]");
        ctx.Console.MarkupLineInterpolated($"  {ctx.Command.Description}");
        return true;
    };

    /// <summary>
    /// Writes the usage pattern.
    /// </summary>
    public static Func<FancyHelpContext, bool> UsageSection() => ctx =>
    {
        ctx.Console.MarkupLine("[bold]Usage:[/]");
        ctx.Console.MarkupLineInterpolated($"  [deepskyblue1]{GetUsage(ctx.Command)}[/]");
        return true;
    };

    /// <summary>
    /// Writes the arguments section as a Spectre table.
    /// </summary>
    public static Func<FancyHelpContext, bool> ArgumentsSection() => ctx =>
    {
        var arguments = ctx.Command.Arguments
            .Where(a => !a.Hidden)
            .ToList();

        if (arguments.Count == 0)
        {
            return false;
        }

        ctx.Console.MarkupLine("[bold]Arguments:[/]");

        var table = new Table();
        table.Border(TableBorder.None);
        table.HideHeaders();
        table.AddColumn(new TableColumn("Name").PadRight(2));
        table.AddColumn(new TableColumn("Description"));

        foreach (var arg in arguments)
        {
            table.AddRow(
                $"  [deepskyblue1]<{Markup.Escape(arg.Name)}>[/]",
                Markup.Escape(arg.Description ?? ""));
        }

        ctx.Console.Write(table);
        return true;
    };

    /// <summary>
    /// Writes the options section as a Spectre table.
    /// </summary>
    public static Func<FancyHelpContext, bool> OptionsSection() => ctx =>
    {
        var options = ctx.Command.Options
            .Where(o => !o.Hidden)
            .ToList();

        if (options.Count == 0)
        {
            return false;
        }

        ctx.Console.MarkupLine("[bold]Options:[/]");

        var table = new Table();
        table.Border(TableBorder.None);
        table.HideHeaders();
        table.AddColumn(new TableColumn("Name").PadRight(2));
        table.AddColumn(new TableColumn("Description"));

        foreach (var option in options)
        {
            var aliases = new[] { option.Name }
                .Concat(option.Aliases)
                .Distinct()
                .OrderBy(a => a.Length);
            var label = string.Join(", ", aliases);

            table.AddRow(
                $"  [green]{Markup.Escape(label)}[/]",
                Markup.Escape(option.Description ?? ""));
        }

        ctx.Console.Write(table);
        return true;
    };

    /// <summary>
    /// Writes the subcommands section as a Spectre table.
    /// </summary>
    public static Func<FancyHelpContext, bool> SubcommandsSection() => ctx =>
    {
        var subcommands = ctx.Command.Subcommands
            .Where(c => !c.Hidden)
            .ToList();

        if (subcommands.Count == 0)
        {
            return false;
        }

        ctx.Console.MarkupLine("[bold]Commands:[/]");

        var table = new Table();
        table.Border(TableBorder.None);
        table.HideHeaders();
        table.AddColumn(new TableColumn("Name").PadRight(2));
        table.AddColumn(new TableColumn("Description"));

        foreach (var cmd in subcommands)
        {
            table.AddRow(
                $"  [yellow]{Markup.Escape(cmd.Name)}[/]",
                Markup.Escape(cmd.Description ?? ""));
        }

        ctx.Console.Write(table);
        return true;
    };

    private static string GetUsage(Command command)
    {
        var parts = new List<string>();

        // Walk up to root to get full command path
        var chain = new List<string>();
        Command? current = command;
        while (current is not null)
        {
            chain.Insert(0, current.Name);
            current = current.Parents.OfType<Command>().FirstOrDefault();
        }
        parts.Add(string.Join(" ", chain));

        if (command.Options.Any(o => !o.Hidden))
        {
            parts.Add("[options]");
        }

        foreach (var arg in command.Arguments.Where(a => !a.Hidden))
        {
            parts.Add($"<{arg.Name}>");
        }

        if (command.Subcommands.Any(c => !c.Hidden))
        {
            parts.Add("[command]");
        }

        return string.Join(" ", parts);
    }
}
