using System.CommandLine;
using System.CommandLine.Invocation;
using Spectre.Console;

namespace HelpLine.FancyHelp;

/// <summary>
/// A command line action that displays colorized help using <see cref="FancyHelpBuilder"/>.
/// </summary>
public sealed class FancyHelpAction : SynchronousCommandLineAction
{
    private FancyHelpBuilder? _builder;

    /// <summary>
    /// The builder used to render help.
    /// </summary>
    public FancyHelpBuilder Builder
    {
        get => _builder ??= new FancyHelpBuilder();
        set => _builder = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <inheritdoc />
    public override int Invoke(ParseResult parseResult)
    {
        var command = parseResult.CommandResult.Command;
        var output = parseResult.InvocationConfiguration.Output;
        var console = ConsoleDetection.ShouldUseSpectre(output)
                          ? AnsiConsole.Console
                          : AnsiConsole.Create(new AnsiConsoleSettings
                          {
                              Ansi = AnsiSupport.Yes,
                              Out = new AnsiConsoleOutput(output),
                          });

        Builder.Write(command, console);
        return 0;
    }

    /// <inheritdoc />
    public override bool ClearsParseErrors => true;
}