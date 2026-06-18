using System.CommandLine;
using System.CommandLine.Help;

namespace HelpLine.FancyHelp;

/// <summary>
/// Extension methods for applying <see cref="FancyHelpBuilder"/> to commands.
/// </summary>
public static class FancyHelpExtensions
{
    /// <summary>
    /// Applies the <see cref="FancyHelpBuilder"/> to the specified command and all its descendants
    /// by replacing the action on each <see cref="HelpOption"/>.
    /// </summary>
    public static void UseFancyHelp(this Command command, FancyHelpBuilder? builder = null)
    {
        ArgumentNullException.ThrowIfNull(command);

        ApplyRecursive(command, builder ?? new FancyHelpBuilder());
    }

    private static void ApplyRecursive(Command command, FancyHelpBuilder builder)
    {
        foreach (var option in command.Options)
        {
            if (option is HelpOption helpOption)
            {
                helpOption.Action = new FancyHelpAction { Builder = builder };
            }
        }

        foreach (var sub in command.Subcommands)
        {
            ApplyRecursive(sub, builder);
        }
    }
}
