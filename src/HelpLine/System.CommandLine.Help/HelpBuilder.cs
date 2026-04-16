// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace System.CommandLine.Help;

/// <summary>
/// Formats output to be shown to users to describe how to use a command line tool.
/// </summary>
public partial class HelpBuilder
{
    private const string Indent = "  ";

    private Dictionary<Symbol, Customization>? _customizationsBySymbol;
    private Func<HelpContext, IEnumerable<Func<HelpContext, bool>>>? _getLayout;

    /// <summary>
    /// Initializes a new instance of the <see cref="HelpBuilder"/> class.
    /// </summary>
    /// <param name="maxWidth">The maximum width in characters after which help output is wrapped.</param>
    public HelpBuilder(int maxWidth = int.MaxValue)
    {
        MaxWidth = maxWidth > 0 ? maxWidth : int.MaxValue;
    }

    /// <summary>
    /// The maximum width for which to format help output.
    /// </summary>
    public int MaxWidth { get; }

    /// <summary>
    /// Writes help output for the specified command.
    /// </summary>
    public virtual void Write(HelpContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (context.Command.Hidden)
        {
            return;
        }

        foreach (var writeSection in GetLayout(context))
        {
            if (!writeSection(context))
            {
                continue;
            }

            context.Output.WriteLine();
        }
    }

    /// <summary>
    /// Specifies custom help details for a specific symbol.
    /// </summary>
    public void CustomizeSymbol(
        Symbol symbol,
        Func<HelpContext, string?>? firstColumnText = null,
        Func<HelpContext, string?>? secondColumnText = null,
        Func<HelpContext, string?>? defaultValue = null)
    {
        if (symbol is null)
        {
            throw new ArgumentNullException(nameof(symbol));
        }

        _customizationsBySymbol ??= [];
        _customizationsBySymbol[symbol] = new Customization(firstColumnText, secondColumnText, defaultValue);
    }

    /// <summary>
    /// Customizes the help sections that will be displayed.
    /// </summary>
    public void CustomizeLayout(Func<HelpContext, IEnumerable<Func<HelpContext, bool>>> getLayout)
    {
        _getLayout = getLayout ?? throw new ArgumentNullException(nameof(getLayout));
    }

    /// <summary>
    /// Specifies custom help details for a specific symbol using simple string values.
    /// </summary>
    public void CustomizeSymbol(
        Symbol symbol,
        string? firstColumnText = null,
        string? secondColumnText = null,
        string? defaultValue = null)
    {
        CustomizeSymbol(symbol, _ => firstColumnText, _ => secondColumnText, _ => defaultValue);
    }

    /// <summary>
    /// Writes help output for the specified command.
    /// </summary>
    public void Write(Command command, TextWriter writer)
    {
        Write(new HelpContext(this, command, writer));
    }

    private string GetUsage(Command command)
    {
        return string.Join(" ", GetUsageParts().Where(static value => !string.IsNullOrWhiteSpace(value)));

        IEnumerable<string> GetUsageParts()
        {
            var displayOptionTitle = false;

            var parentCommands = command
                .RecurseWhileNotNull(static current => current.Parents.OfType<Command>().FirstOrDefault())
                .Reverse();

            foreach (var parentCommand in parentCommands)
            {
                if (!displayOptionTitle)
                {
                    displayOptionTitle = parentCommand.Options.Any(static option => option.Recursive && !option.Hidden);
                }

                yield return (parentCommand is RootCommand ? RootCommand.ExecutableName : null) ?? parentCommand.Name;

                if (parentCommand.Arguments.Any())
                {
                    yield return FormatArgumentUsage(parentCommand.Arguments);
                }
            }

            if (command.Subcommands.Any(static subcommand => !subcommand.Hidden))
            {
                yield return LocalizationResources.HelpUsageCommand();
            }

            displayOptionTitle = displayOptionTitle || command.Options.Any(static option => !option.Hidden);

            if (displayOptionTitle)
            {
                yield return LocalizationResources.HelpUsageOptions();
            }

            if (!command.TreatUnmatchedTokensAsErrors)
            {
                yield return LocalizationResources.HelpUsageAdditionalArguments();
            }
        }
    }

    private IEnumerable<TwoColumnHelpRow> GetCommandArgumentRows(Command command, HelpContext context) =>
        command
            .RecurseWhileNotNull(static current => current.Parents.OfType<Command>().FirstOrDefault())
            .Reverse()
            .SelectMany(static current => current.Arguments.Where(argument => !argument.Hidden))
            .Select(argument => GetTwoColumnRow(argument, context))
            .Distinct();

    private bool WriteSubcommands(HelpContext context)
    {
        var subcommands = context.Command.Subcommands
            .Where(static command => !command.Hidden)
            .Select(command => GetTwoColumnRow(command, context))
            .ToArray();

        if (subcommands.Length == 0)
        {
            return false;
        }

        WriteHeading(LocalizationResources.HelpCommandsTitle(), null, context.Output);
        WriteColumns(subcommands, context);
        return true;
    }

    private bool WriteAdditionalArguments(HelpContext context)
    {
        if (context.Command.TreatUnmatchedTokensAsErrors)
        {
            return false;
        }

        WriteHeading(
            LocalizationResources.HelpAdditionalArgumentsTitle(),
            LocalizationResources.HelpAdditionalArgumentsDescription(),
            context.Output);

        return true;
    }

    private void WriteHeading(string? heading, string? description, TextWriter writer)
    {
        if (!string.IsNullOrWhiteSpace(heading))
        {
            writer.WriteLine(heading);
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            return;
        }

        var maxWidth = Math.Max(1, MaxWidth - Indent.Length);

        foreach (var part in WrapText(description!, maxWidth))
        {
            writer.Write(Indent);
            writer.WriteLine(part);
        }
    }

    /// <summary>
    /// Writes the specified help rows, aligning output in columns.
    /// </summary>
    public void WriteColumns(IReadOnlyList<TwoColumnHelpRow> items, HelpContext context)
    {
        if (items.Count == 0)
        {
            return;
        }

        var windowWidth = Math.Max(MaxWidth, 20);
        var firstColumnWidth = items.Max(static item => item.FirstColumnText.Length);
        var secondColumnWidth = items.Max(static item => item.SecondColumnText.Length);

        if (firstColumnWidth + secondColumnWidth + (Indent.Length * 2) > windowWidth)
        {
            var firstColumnMaxWidth = Math.Max(1, (windowWidth / 2) - Indent.Length);

            if (firstColumnWidth > firstColumnMaxWidth)
            {
                firstColumnWidth = items
                    .SelectMany(item => WrapText(item.FirstColumnText, firstColumnMaxWidth).Select(static part => part.Length))
                    .DefaultIfEmpty(firstColumnMaxWidth)
                    .Max();
            }

            secondColumnWidth = Math.Max(1, windowWidth - firstColumnWidth - (Indent.Length * 2));
        }

        foreach (var helpItem in items)
        {
            var firstColumnParts = WrapText(helpItem.FirstColumnText, firstColumnWidth);
            var secondColumnParts = WrapText(helpItem.SecondColumnText, secondColumnWidth);

            foreach (var (first, second) in ZipWithEmpty(firstColumnParts, secondColumnParts))
            {
                context.Output.Write($"{Indent}{first}");

                if (!string.IsNullOrWhiteSpace(second))
                {
                    var padSize = firstColumnWidth - first.Length;

                    if (padSize > 0)
                    {
                        context.Output.Write(new string(' ', padSize));
                    }

                    context.Output.Write($"{Indent}{second}");
                }

                context.Output.WriteLine();
            }
        }

        static IEnumerable<(string First, string Second)> ZipWithEmpty(IEnumerable<string> first, IEnumerable<string> second)
        {
            using var firstEnumerator = first.GetEnumerator();
            using var secondEnumerator = second.GetEnumerator();

            var hasFirst = false;
            var hasSecond = false;

            while ((hasFirst = firstEnumerator.MoveNext()) | (hasSecond = secondEnumerator.MoveNext()))
            {
                yield return (hasFirst ? firstEnumerator.Current : string.Empty, hasSecond ? secondEnumerator.Current : string.Empty);
            }
        }
    }

    /// <summary>
    /// Gets a help item for the specified symbol.
    /// </summary>
    public TwoColumnHelpRow GetTwoColumnRow(Symbol symbol, HelpContext context)
    {
        if (symbol is null)
        {
            throw new ArgumentNullException(nameof(symbol));
        }

        Customization? customization = null;

        if (_customizationsBySymbol is not null)
        {
            _customizationsBySymbol.TryGetValue(symbol, out customization);
        }

        return symbol switch
        {
            Option or Command => GetOptionOrCommandRow(),
            Argument argument => GetCommandArgumentRow(argument),
            _ => throw new NotSupportedException($"Symbol type {symbol.GetType()} is not supported."),
        };

        TwoColumnHelpRow GetOptionOrCommandRow()
        {
            var firstColumnText = customization?.GetFirstColumn?.Invoke(context)
                                  ?? (symbol is Option option
                                      ? Default.GetOptionUsageLabel(option)
                                      : Default.GetCommandUsageLabel((Command)symbol));

            var customizedSymbolDescription = customization?.GetSecondColumn?.Invoke(context);
            var symbolDescription = customizedSymbolDescription ?? symbol.Description ?? string.Empty;
            var defaultValueDescription = customizedSymbolDescription is null
                ? GetOptionOrCommandDefaultValue()
                : string.Empty;

            return new TwoColumnHelpRow(firstColumnText, $"{symbolDescription} {defaultValueDescription}".Trim());
        }

        TwoColumnHelpRow GetCommandArgumentRow(Argument argument)
        {
            var firstColumnText = customization?.GetFirstColumn?.Invoke(context) ?? Default.GetArgumentUsageLabel(argument);
            var argumentDescription = customization?.GetSecondColumn?.Invoke(context) ?? Default.GetArgumentDescription(argument);
            var defaultValue = Default.ShouldShowDefaultValue(argument)
                ? GetArgumentDefaultValue(context.Command, argument, true, context)
                : string.Empty;
            var defaultValueDescription = string.IsNullOrWhiteSpace(defaultValue)
                ? string.Empty
                : $"[{defaultValue}]";

            return new TwoColumnHelpRow(firstColumnText, $"{argumentDescription} {defaultValueDescription}".Trim());
        }

        string GetOptionOrCommandDefaultValue()
        {
            var defaultArguments = symbol
                .GetParameters()
                .Where(static argument => !argument.Hidden)
                .Where(Default.ShouldShowDefaultValue)
                .ToArray();

            if (defaultArguments.Length == 0)
            {
                return string.Empty;
            }

            var isSingleArgument = defaultArguments.Length == 1;
            var argumentDefaultValues = string.Join(", ", defaultArguments
                .Select(argument => GetArgumentDefaultValue(symbol, argument, isSingleArgument, context))
                .Where(static value => !string.IsNullOrWhiteSpace(value)));

            return string.IsNullOrWhiteSpace(argumentDefaultValues)
                ? string.Empty
                : $"[{argumentDefaultValues}]";
        }
    }

    private string GetArgumentDefaultValue(Symbol parent, Symbol parameter, bool useDefaultLabel, HelpContext context)
    {
        string? displayedDefaultValue = null;

        if (_customizationsBySymbol is not null)
        {
            if (_customizationsBySymbol.TryGetValue(parent, out var parentCustomization) &&
                parentCustomization.GetDefaultValue?.Invoke(context) is { } parentDefaultValue)
            {
                displayedDefaultValue = parentDefaultValue;
            }
            else if (_customizationsBySymbol.TryGetValue(parameter, out var parameterCustomization) &&
                     parameterCustomization.GetDefaultValue?.Invoke(context) is { } ownDefaultValue)
            {
                displayedDefaultValue = ownDefaultValue;
            }
        }

        displayedDefaultValue ??= Default.GetArgumentDefaultValue(parameter);

        if (string.IsNullOrWhiteSpace(displayedDefaultValue))
        {
            return string.Empty;
        }

        var label = useDefaultLabel
            ? LocalizationResources.HelpArgumentDefaultValueLabel()
            : parameter.Name;

        return $"{label}: {displayedDefaultValue}";
    }

    private string FormatArgumentUsage(IList<Argument> arguments)
    {
        var builder = new StringBuilder(arguments.Count * 16);
        List<char>? closingCharacters = null;

        foreach (var argument in arguments)
        {
            if (argument.Hidden)
            {
                continue;
            }

            var arityIndicator = argument.Arity.MaximumNumberOfValues > 1 ? "..." : string.Empty;
            var isOptional = argument.Arity.MinimumNumberOfValues == 0;

            if (isOptional)
            {
                builder.Append($"[<{argument.Name}>{arityIndicator}");
                (closingCharacters ??= []).Add(']');
            }
            else
            {
                builder.Append($"<{argument.Name}>{arityIndicator}");
            }

            builder.Append(' ');
        }

        if (builder.Length == 0)
        {
            return string.Empty;
        }

        builder.Length--;

        if (closingCharacters is not null)
        {
            while (closingCharacters.Count > 0)
            {
                builder.Append(closingCharacters[^1]);
                closingCharacters.RemoveAt(closingCharacters.Count - 1);
            }
        }

        return builder.ToString();
    }

    private IEnumerable<Func<HelpContext, bool>> GetLayout(HelpContext context)
    {
        _getLayout ??= static _ => Default.GetLayout();
        return _getLayout(context);
    }

    private static IEnumerable<string> WrapText(string text, int maxWidth)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            yield break;
        }

        if (maxWidth <= 0)
        {
            yield return text;
            yield break;
        }

        var parts = text.Split(["\r\n", "\n"], StringSplitOptions.None);

        foreach (var part in parts)
        {
            if (part.Length <= maxWidth)
            {
                yield return part;
                continue;
            }

            for (var index = 0; index < part.Length;)
            {
                if (part.Length - index <= maxWidth)
                {
                    yield return part[index..];
                    break;
                }

                var length = -1;

                for (var offset = 0; index + offset < part.Length && offset < maxWidth; offset++)
                {
                    if (char.IsWhiteSpace(part[index + offset]))
                    {
                        length = offset + 1;
                    }
                }

                if (length == -1)
                {
                    length = maxWidth;
                }

                yield return part.Substring(index, length);
                index += length;
            }
        }
    }

    private sealed class Customization(
        Func<HelpContext, string?>? getFirstColumn,
        Func<HelpContext, string?>? getSecondColumn,
        Func<HelpContext, string?>? getDefaultValue)
    {
        public Func<HelpContext, string?>? GetFirstColumn { get; } = getFirstColumn;

        public Func<HelpContext, string?>? GetSecondColumn { get; } = getSecondColumn;

        public Func<HelpContext, string?>? GetDefaultValue { get; } = getDefaultValue;
    }
}
