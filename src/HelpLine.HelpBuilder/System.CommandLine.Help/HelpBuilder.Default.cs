// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.CommandLine.Completions;
using System.Linq;

namespace System.CommandLine.Help;

public partial class HelpBuilder
{
    /// <summary>
    /// Provides default formatting for help output.
    /// </summary>
    public static class Default
    {
        /// <summary>
        /// Gets an argument's default value to be displayed in help.
        /// </summary>
        public static string GetArgumentDefaultValue(Symbol symbol)
        {
            return symbol switch
            {
                Argument argument => ShouldShowDefaultValue(argument)
                    ? ToDisplayString(argument.GetDefaultValue(), argument.ValueType)
                    : string.Empty,
                Option option => ShouldShowDefaultValue(option)
                    ? ToDisplayString(option.GetDefaultValue(), option.ValueType)
                    : string.Empty,
                _ => throw new InvalidOperationException("Symbol must be an Argument or Option."),
            };
        }

        public static bool ShouldShowDefaultValue(Symbol symbol) =>
            symbol switch
            {
                Option option => ShouldShowDefaultValue(option),
                Argument argument => ShouldShowDefaultValue(argument),
                _ => false,
            };

        public static bool ShouldShowDefaultValue(Option option) => option.HasDefaultValue;

        public static bool ShouldShowDefaultValue(Argument argument) => argument.HasDefaultValue;

        /// <summary>
        /// Gets the description for an argument.
        /// </summary>
        public static string GetArgumentDescription(Argument argument) => argument.Description ?? string.Empty;

        /// <summary>
        /// Gets the usage title for an argument.
        /// </summary>
        public static string GetArgumentUsageLabel(Symbol parameter)
        {
            return parameter switch
            {
                Argument argument => GetUsageLabel(argument.HelpName, argument.ValueType, argument.CompletionSources, argument, argument.Arity) ?? $"<{argument.Name}>",
                Option option => GetUsageLabel(option.HelpName, option.ValueType, option.CompletionSources, option, option.Arity) ?? string.Empty,
                _ => throw new InvalidOperationException(),
            };
        }

        /// <summary>
        /// Gets the usage label for the specified command.
        /// </summary>
        public static string GetCommandUsageLabel(Command symbol) => GetIdentifierSymbolUsageLabel(symbol, symbol.Aliases);

        /// <summary>
        /// Gets the usage label for the specified option.
        /// </summary>
        public static string GetOptionUsageLabel(Option symbol) => GetIdentifierSymbolUsageLabel(symbol, symbol.Aliases);

        /// <summary>
        /// Gets the default sections to be written for command line help.
        /// </summary>
        public static IEnumerable<Func<HelpContext, bool>> GetLayout()
        {
            yield return SynopsisSection();
            yield return CommandUsageSection();
            yield return CommandArgumentsSection();
            yield return OptionsSection();
            yield return SubcommandsSection();
            yield return AdditionalArgumentsSection();
        }

        /// <summary>
        /// Writes a help section describing a command's synopsis.
        /// </summary>
        public static Func<HelpContext, bool> SynopsisSection() =>
            ctx =>
            {
                ctx.HelpBuilder.WriteHeading(LocalizationResources.HelpDescriptionTitle(), ctx.Command.Description, ctx.Output);
                return true;
            };

        /// <summary>
        /// Writes a help section describing a command's usage.
        /// </summary>
        public static Func<HelpContext, bool> CommandUsageSection() =>
            ctx =>
            {
                ctx.HelpBuilder.WriteHeading(LocalizationResources.HelpUsageTitle(), ctx.HelpBuilder.GetUsage(ctx.Command), ctx.Output);
                return true;
            };

        /// <summary>
        /// Writes a help section describing a command's arguments.
        /// </summary>
        public static Func<HelpContext, bool> CommandArgumentsSection() =>
            ctx =>
            {
                var commandArguments = ctx.HelpBuilder.GetCommandArgumentRows(ctx.Command, ctx).ToArray();

                if (commandArguments.Length == 0)
                {
                    return false;
                }

                ctx.HelpBuilder.WriteHeading(LocalizationResources.HelpArgumentsTitle(), null, ctx.Output);
                ctx.HelpBuilder.WriteColumns(commandArguments, ctx);
                return true;
            };

        /// <summary>
        /// Writes a help section describing a command's subcommands.
        /// </summary>
        public static Func<HelpContext, bool> SubcommandsSection() => ctx => ctx.HelpBuilder.WriteSubcommands(ctx);

        /// <summary>
        /// Writes a help section describing a command's options.
        /// </summary>
        public static Func<HelpContext, bool> OptionsSection() =>
            ctx =>
            {
                List<TwoColumnHelpRow> optionRows = [];
                var addedHelpOption = false;

                foreach (var option in ctx.Command.Options.OrderBy(o => o is HelpOption))
                {
                    if (option.Hidden)
                    {
                        continue;
                    }

                    if (option is HelpOption)
                    {
                        addedHelpOption = true;
                    }

                    optionRows.Add(ctx.HelpBuilder.GetTwoColumnRow(option, ctx));
                }

                Command? current = ctx.Command;

                while (current is not null)
                {
                    Command? parentCommand = null;

                    foreach (var parent in current.Parents)
                    {
                        if (parent is not Command command)
                        {
                            continue;
                        }

                        parentCommand = command;

                        foreach (var option in parentCommand.Options.Where(option => option is { Recursive: true, Hidden: false }))
                        {
                            if (option is HelpOption && addedHelpOption)
                            {
                                continue;
                            }

                            optionRows.Add(ctx.HelpBuilder.GetTwoColumnRow(option, ctx));
                        }

                        break;
                    }

                    current = parentCommand;
                }

                if (optionRows.Count == 0)
                {
                    return false;
                }

                ctx.HelpBuilder.WriteHeading(LocalizationResources.HelpOptionsTitle(), null, ctx.Output);
                ctx.HelpBuilder.WriteColumns(optionRows, ctx);
                return true;
            };

        /// <summary>
        /// Writes a help section describing a command's additional arguments.
        /// </summary>
        public static Func<HelpContext, bool> AdditionalArgumentsSection() =>
            ctx => ctx.HelpBuilder.WriteAdditionalArguments(ctx);

        private static string ToDisplayString(object? value, Type valueType)
        {
            return value switch
            {
                _ when (valueType == typeof(bool) || valueType == typeof(bool?)) && value is not true => string.Empty,
                bool boolValue => boolValue ? "true" : "false",
                null => string.Empty,
                string text => text,
                IEnumerable sequence => string.Join("|", sequence.Cast<object>()),
                _ => value.ToString() ?? string.Empty,
            };
        }

        private static string? GetUsageLabel(
            string? helpName,
            Type valueType,
            IReadOnlyList<Func<CompletionContext, IEnumerable<CompletionItem>>> completionSources,
            Symbol symbol,
            ArgumentArity arity)
        {
            if (!string.IsNullOrWhiteSpace(helpName))
            {
                return $"<{helpName}>";
            }

            if (valueType == typeof(bool) || valueType == typeof(bool?) || arity.MaximumNumberOfValues <= 0)
            {
                return null;
            }

            if (completionSources.Count <= 0)
            {
                return symbol is Option ? $"<{symbol.Name.TrimStart('-', '/')}>" : null;
            }

            var joined = string.Join("|", symbol.GetCompletions(CompletionContext.Empty).Select(item => item.Label));

            return string.IsNullOrEmpty(joined)
                ? null
                : $"<{joined}>";
        }

        private static string GetIdentifierSymbolUsageLabel(Symbol symbol, ICollection<string>? aliasSet)
        {
            IEnumerable<string> aliases = aliasSet is null
                ? new[] { symbol.Name }
                : new[] { symbol.Name }.Concat(aliasSet)
                    .Select(static alias => alias.SplitPrefix())
                    .OrderBy(static value => value.Prefix, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(static value => value.Alias, StringComparer.OrdinalIgnoreCase)
                    .GroupBy(static value => value.Alias)
                    .Select(static group => group.First())
                    .Select(static value => $"{value.Prefix}{value.Alias}");

            var firstColumnText = string.Join(", ", aliases);

            foreach (var argument in symbol.GetParameters().Where(argument => !argument.Hidden))
            {
                var argumentUsageLabel = GetArgumentUsageLabel(argument);

                if (!string.IsNullOrWhiteSpace(argumentUsageLabel))
                {
                    firstColumnText += $" {argumentUsageLabel}";
                }
            }

            if (symbol is Option { Required: true })
            {
                firstColumnText += $" {LocalizationResources.HelpOptionsRequiredLabel()}";
            }

            return firstColumnText;
        }
    }
}
