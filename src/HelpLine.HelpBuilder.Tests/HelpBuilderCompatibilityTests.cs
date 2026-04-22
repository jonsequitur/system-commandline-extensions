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
}
