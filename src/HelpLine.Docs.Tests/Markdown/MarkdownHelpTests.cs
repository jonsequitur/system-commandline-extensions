using System.CommandLine;
using AwesomeAssertions;
using AwesomeAssertions.Execution;
using HelpLine.Docs;

namespace HelpLine.Docs.Tests;

public class MarkdownHelpTests
{
    [Fact]
    public void Assembly_overload_discovers_topics_from_embedded_resources()
    {
        RootCommand rootCommand = new("sample");
        var helpCommand = rootCommand.AddMarkdownHelp(typeof(MarkdownHelpTests).Assembly, new MarkdownHelpRenderer { HeadingLevelOffset = 0 });

        var output = new StringWriter();
        var exitCode = rootCommand.Parse("docs --topic getting-started").Invoke(new() { Output = output });

        using var scope = new AssertionScope();
        helpCommand.Should().NotBeNull();
        exitCode.Should().Be(0);
        output.ToString().Should().Contain("Getting Started");
        output.ToString().Should().Contain("install the tool");
    }

    [Fact]
    public void Help_command_lists_and_renders_embedded_topics()
    {
        var catalog = HelpTopicCatalog.FromAssembly(typeof(MarkdownHelpTests).Assembly);

        RootCommand rootCommand = new("sample");
        rootCommand.AddMarkdownHelp(catalog, new MarkdownHelpRenderer { HeadingLevelOffset = 0 });

        var listOutput = new StringWriter();
        var listExitCode = rootCommand.Parse("docs").Invoke(new() { Output = listOutput });

        var topicOutput = new StringWriter();
        var topicExitCode = rootCommand.Parse("docs --topic getting-started").Invoke(new() { Output = topicOutput });

        using var scope = new AssertionScope();
        catalog.Topics.Should().NotBeEmpty();
        listExitCode.Should().Be(0);
        listOutput.ToString().Should().Contain("Available help topics:");
        listOutput.ToString().Should().Contain("getting-started");
        topicExitCode.Should().Be(0);
        topicOutput.ToString().Should().Contain("Getting Started");
        topicOutput.ToString().Should().Contain("install the tool");
    }

}