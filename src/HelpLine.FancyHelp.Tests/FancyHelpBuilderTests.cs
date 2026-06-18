using System.CommandLine;
using System.CommandLine.Help;
using AwesomeAssertions;
using Spectre.Console.Testing;

namespace HelpLine.FancyHelp.Tests;

public class FancyHelpBuilderTests
{
   
    [Fact]
    public void Write_renders_description_section()
    {
        var command = new RootCommand("A test CLI tool");
        var console = new TestConsole();
        var builder = new FancyHelpBuilder();

        builder.Write(command, console);

        var output = console.Output;
        output.Should().Contain("Description:");
        output.Should().Contain("A test CLI tool");
    }

    [Fact]
    public void Write_renders_options_section()
    {
        var command = new RootCommand("test");
        command.Options.Add(new Option<string>("--name") { Description = "Your name" });
        var console = new TestConsole();
        var builder = new FancyHelpBuilder();

        builder.Write(command, console);

        var output = console.Output;
        output.Should().Contain("Options:");
        output.Should().Contain("--name");
        output.Should().Contain("Your name");
    }

    [Fact]
    public void Write_renders_arguments_section()
    {
        var command = new RootCommand("test");
        command.Arguments.Add(new Argument<string>("file") { Description = "The file to process" });
        var console = new TestConsole();
        var builder = new FancyHelpBuilder();

        builder.Write(command, console);

        var output = console.Output;
        output.Should().Contain("Arguments:");
        output.Should().Contain("file");
        output.Should().Contain("The file to process");
    }

    [Fact]
    public void Write_renders_subcommands_section()
    {
        var command = new RootCommand("test");
        command.Subcommands.Add(new Command("serve", "Start the server"));
        command.Subcommands.Add(new Command("build", "Build the project"));
        var console = new TestConsole();
        var builder = new FancyHelpBuilder();

        builder.Write(command, console);

        var output = console.Output;
        output.Should().Contain("Commands:");
        output.Should().Contain("serve");
        output.Should().Contain("Start the server");
    }

    [Fact]
    public void Write_skips_hidden_command()
    {
        var command = new RootCommand("test") { Hidden = true };
        var console = new TestConsole();
        var builder = new FancyHelpBuilder();

        builder.Write(command, console);

        console.Output.Should().BeEmpty();
    }

    [Fact]
    public void CustomizeLayout_replaces_default_sections()
    {
        var command = new RootCommand("test");
        command.Options.Add(new Option<bool>("--verbose") { Description = "Be verbose" });
        var console = new TestConsole();
        var builder = new FancyHelpBuilder();

        builder.CustomizeLayout(_ => [DefaultLayout.OptionsSection()]);
        builder.Write(command, console);

        var output = console.Output;
        output.Should().Contain("Options:");
        output.Should().NotContain("Description:");
        output.Should().NotContain("Usage:");
    }

    [Fact]
    public void UseFancyHelp_applies_to_command()
    {
        var command = new RootCommand("test");
        command.UseFancyHelp();

        var helpOption = command.Options.OfType<HelpOption>().FirstOrDefault();
        helpOption.Should().NotBeNull();
        helpOption!.Action.Should().BeOfType<FancyHelpAction>();
    }
}