using System.CommandLine;
using Spectre.Console;

namespace HelpLine.FancyHelp;

/// <summary>
/// Provides Spectre.Console-powered colorized help output for System.CommandLine commands.
/// Uses a composable section-based layout.
/// </summary>
public class FancyHelpBuilder
{
    private Func<FancyHelpContext, IEnumerable<Func<FancyHelpContext, bool>>>? _getLayout;

    /// <summary>
    /// Customizes the help sections that will be displayed.
    /// </summary>
    public void CustomizeLayout(Func<FancyHelpContext, IEnumerable<Func<FancyHelpContext, bool>>> getLayout)
    {
        _getLayout = getLayout ?? throw new ArgumentNullException(nameof(getLayout));
    }

    /// <summary>
    /// Writes colorized help output for the specified command.
    /// </summary>
    public virtual void Write(FancyHelpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Command.Hidden)
        {
            return;
        }

        foreach (var writeSection in GetLayout(context))
        {
            if (writeSection(context))
            {
                context.Console.WriteLine();
            }
        }
    }

    /// <summary>
    /// Gets the layout sections to render.
    /// </summary>
    protected IEnumerable<Func<FancyHelpContext, bool>> GetLayout(FancyHelpContext context) =>
        _getLayout?.Invoke(context) ?? DefaultLayout.GetLayout();

    /// <summary>
    /// Writes help for the specified command using the default or customized layout.
    /// </summary>
    public void Write(Command command, IAnsiConsole? console = null)
    {
        console ??= AnsiConsole.Console;
        var context = new FancyHelpContext(this, command, console);
        Write(context);
    }
}
