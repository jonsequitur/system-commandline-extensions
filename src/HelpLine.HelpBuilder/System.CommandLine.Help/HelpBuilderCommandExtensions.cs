// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.CommandLine.Help;

/// <summary>
/// Extension methods for configuring <see cref="HelpBuilder"/> on System.CommandLine commands.
/// </summary>
public static class HelpBuilderCommandExtensions
{
    /// <summary>
    /// Configures the command and all of its subcommands to use the specified <see cref="HelpBuilder"/>
    /// when help is requested via <see cref="HelpOption"/>.
    /// </summary>
    public static void UseHelpBuilder(this Command command, HelpBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(builder);

        var action = new CustomHelpAction { Builder = builder };

        foreach (var current in EnumerateCommands(command))
        {
            foreach (var helpOption in current.Options.OfType<HelpOption>())
            {
                helpOption.Action = action;
            }
        }
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
}
