using System.CommandLine;
using AwesomeAssertions;
using Spectre.Console;

namespace HelpLine.FancyHelp.Tests;

public class FancyHelpTests
{
    [Fact]
    public void UseFancyHelp_produces_colorized_output_when_help_is_invoked()
    {
        var root = new RootCommand("A CLI tool for managing widgets")
        {
            new Option<string>("--name") { Description = "Your name" },
            new Argument<string>("file") { Description = "The file to process" },
            new Command("serve", "Start the server"),
        };

        root.UseFancyHelp();

        var output = new StringWriter();
        root.Parse("--help").Invoke(new() { Output = output });

        var text = output.ToString();
        text.Should().Contain("\x1b[",
                              "output should contain ANSI escape sequences for color");
        text.Should().Contain("Description:");
        text.Should().Contain("A CLI tool for managing widgets");
        text.Should().Contain("Usage:");
        text.Should().Contain("Options:");
        text.Should().Contain("--name");
        text.Should().Contain("Arguments:");
        text.Should().Contain("file");
        text.Should().Contain("Commands:");
        text.Should().Contain("serve");
    }

    [Fact]
    public void CustomizeLayout_allows_user_to_control_sections_and_colors()
    {
        var root = new RootCommand("Widget manager")
        {
            new Option<string>("--output") { Description = "Output path" },
            new Command("build", "Build the project"),
        };

        var builder = new FancyHelpBuilder();
        builder.CustomizeLayout(_ =>
        [
            ctx =>
            {
                if (string.IsNullOrWhiteSpace(ctx.Command.Description))
                {
                    return false;
                }

                ctx.Console.MarkupLine($"[bold magenta]About:[/]");
                ctx.Console.MarkupLineInterpolated($"  [italic]{ctx.Command.Description}[/]");
                return true;
            },

            DefaultLayout.OptionsSection(),
            
            ctx =>
            {
                ctx.Console.MarkupLine("[dim]Run with --help on any subcommand for more info.[/]");
                return true;
            },
        ]);

        root.UseFancyHelp(builder);

        var output = new StringWriter();
        root.Parse("--help").Invoke(new() { Output = output });

        var text = output.ToString();
        text.Should().Contain("About:");
        text.Should().Contain("Widget manager");
        text.Should().Contain("Options:");
        text.Should().Contain("--output");
        text.Should().Contain("Run with --help on any subcommand for more info.");

        text.Should().NotContain("Usage:");
        text.Should().NotContain("Commands:");
    }
}