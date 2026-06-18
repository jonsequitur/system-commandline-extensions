using System.CommandLine;
using Spectre.Console;

namespace HelpLine.FancyHelp;

/// <summary>
/// Context passed to each help section during rendering.
/// </summary>
public sealed class FancyHelpContext
{
    public FancyHelpContext(FancyHelpBuilder builder, Command command, IAnsiConsole console)
    {
        Builder = builder ?? throw new ArgumentNullException(nameof(builder));
        Command = command ?? throw new ArgumentNullException(nameof(command));
        Console = console ?? throw new ArgumentNullException(nameof(console));
    }

    /// <summary>
    /// The help builder performing the rendering.
    /// </summary>
    public FancyHelpBuilder Builder { get; }

    /// <summary>
    /// The command being documented.
    /// </summary>
    public Command Command { get; }

    /// <summary>
    /// The Spectre.Console instance to write to.
    /// </summary>
    public IAnsiConsole Console { get; }
}
