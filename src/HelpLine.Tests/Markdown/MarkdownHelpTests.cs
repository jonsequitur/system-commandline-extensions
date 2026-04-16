using System.CommandLine;
using System.IO;
using AwesomeAssertions;
using AwesomeAssertions.Execution;
using HelpLine.Markdown;
using HelpLine.Markdown.Rendering;
using HelpLine.Markdown.Topics;
using Xunit;

namespace HelpLine.Tests;

public class MarkdownHelpTests
{
    [Fact]
    public void Help_command_lists_and_renders_embedded_topics()
    {
        var catalog = HelpTopicCatalog.FromAssembly(typeof(MarkdownHelpTests).Assembly);

        RootCommand rootCommand = new("sample");
        rootCommand.AddMarkdownHelp(catalog, new MarkdownHelpRenderer { HeadingLevelOffset = 0 });

        var listOutput = new StringWriter();
        var listExitCode = rootCommand.Parse("help").Invoke(new() { Output = listOutput });

        var topicOutput = new StringWriter();
        var topicExitCode = rootCommand.Parse("help --topic getting-started").Invoke(new() { Output = topicOutput });

        using var scope = new AssertionScope();
        catalog.Topics.Should().NotBeEmpty();
        listExitCode.Should().Be(0);
        listOutput.ToString().Should().Contain("Available help topics:");
        listOutput.ToString().Should().Contain("getting-started");
        topicExitCode.Should().Be(0);
        topicOutput.ToString().Should().Contain("Getting Started");
        topicOutput.ToString().Should().Contain("install the tool");
    }

    [Fact]
    public void Top_level_help_advertises_topics()
    {
        var catalog = HelpTopicCatalog.FromAssembly(typeof(MarkdownHelpTests).Assembly);

        RootCommand rootCommand = new("sample");
        rootCommand.AddMarkdownHelp(catalog);

        var output = new StringWriter();
        var exitCode = rootCommand.Parse("-h").Invoke(new() { Output = output });

        using var scope = new AssertionScope();
        exitCode.Should().Be(0);
        output.ToString().Should().Contain("Help topics:");
        output.ToString().Should().Contain("getting-started");
    }
}
