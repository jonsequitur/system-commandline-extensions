using System.CommandLine;
using System.IO;
using AwesomeAssertions;
using AwesomeAssertions.Execution;
using System.CommandLine.Help;
using Xunit;

namespace HelpLine.HelpBuilderTests;

public class HelpBuilderCompatibilityTests
{
    [Fact]
    public void Customized_help_builder_can_drive_help_output()
    {
        Option<string> option = new("--name")
        {
            Description = "The original description."
        };

        HelpBuilder helpBuilder = new(120);
        helpBuilder.CustomizeSymbol(option, secondColumnText: "The customized description.");
        helpBuilder.CustomizeLayout(_ =>
        [
            ctx =>
            {
                ctx.Output.WriteLine("Compatibility header");
                return true;
            },
            HelpBuilder.Default.OptionsSection()
        ]);

        Command command = new("hello")
        {
            option,
            new HelpOption
            {
                Action = new CustomHelpAction
                {
                    Builder = helpBuilder
                }
            }
        };

        var output = new StringWriter();
        var exitCode = command.Parse("-h").Invoke(new() { Output = output });

        using var scope = new AssertionScope();
        exitCode.Should().Be(0);
        output.ToString().Should().Contain("Compatibility header");
        output.ToString().Should().Contain("The customized description.");
    }

    [Fact]
    public void UseHelpBuilder_applies_builder_to_root_command()
    {
        HelpBuilder helpBuilder = new(120);
        helpBuilder.CustomizeLayout(_ =>
        [
            ctx =>
            {
                ctx.Output.WriteLine("Custom help");
                return true;
            }
        ]);

        RootCommand command = new("test app");
        command.UseHelpBuilder(helpBuilder);

        var output = new StringWriter();
        command.Parse("-h").Invoke(new() { Output = output });

        output.ToString().Should().Contain("Custom help");
    }

    [Fact]
    public void UseHelpBuilder_applies_builder_to_subcommands()
    {
        HelpBuilder helpBuilder = new(120);
        helpBuilder.CustomizeLayout(_ =>
        [
            ctx =>
            {
                ctx.Output.WriteLine("Custom help");
                return true;
            }
        ]);

        Command sub = new("sub", "a subcommand");
        RootCommand root = new("test app") { sub };
        root.UseHelpBuilder(helpBuilder);

        var output = new StringWriter();
        root.Parse("sub -h").Invoke(new() { Output = output });

        output.ToString().Should().Contain("Custom help");
    }

    [Fact]
    public void UseHelpBuilder_applies_builder_to_deeply_nested_subcommands()
    {
        HelpBuilder helpBuilder = new(120);
        helpBuilder.CustomizeLayout(_ =>
        [
            ctx =>
            {
                ctx.Output.WriteLine("Deep help");
                return true;
            }
        ]);

        Command deep = new("deep", "deeply nested");
        Command mid = new("mid", "middle") { deep };
        RootCommand root = new("test app") { mid };
        root.UseHelpBuilder(helpBuilder);

        var output = new StringWriter();
        root.Parse("mid deep -h").Invoke(new() { Output = output });

        output.ToString().Should().Contain("Deep help");
    }
}
