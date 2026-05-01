using System.CommandLine;
using AwesomeAssertions;
using AwesomeAssertions.Execution;

namespace HelpLine.Docs.Tests;

public class DocsCommandTests
{
    [Fact]
    public void Assembly_overload_discovers_topics_from_embedded_resources()
    {
        RootCommand rootCommand = new("sample");
        rootCommand.AddDocsCommand(typeof(DocsCommandTests).Assembly);

        var output = new StringWriter();
        var exitCode = rootCommand.Parse("docs --topic getting-started").Invoke(new() { Output = output });

        using var scope = new AssertionScope();
        exitCode.Should().Be(0);
        output.ToString().Should().Contain("Getting Started");
        output.ToString().Should().Contain("install the tool");
    }

    [Fact]
    public void Assembly_without_docs_throws()
    {
        var assemblyWithoutDocs = typeof(object).Assembly;

        var act = () => DocsTopicCatalog.FromAssemblyResources(assemblyWithoutDocs);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*does not contain any embedded Markdown*");
    }

    [Fact]
    public void Docs_command_lists_and_renders_embedded_topics()
    {
        var catalog = DocsTopicCatalog.FromAssemblyResources(typeof(DocsCommandTests).Assembly);

        RootCommand rootCommand = new("sample");
        rootCommand.AddDocsCommand(catalog);

        var listOutput = new StringWriter();
        var listExitCode = rootCommand.Parse("docs").Invoke(new() { Output = listOutput });

        var topicOutput = new StringWriter();
        var topicExitCode = rootCommand.Parse("docs --topic getting-started").Invoke(new() { Output = topicOutput });

        using var scope = new AssertionScope();
        catalog.Topics.Should().NotBeEmpty();
        listExitCode.Should().Be(0);
        listOutput.ToString().Should().Contain("Available documentation topics:");
        listOutput.ToString().Should().Contain("getting-started");
        topicExitCode.Should().Be(0);
        topicOutput.ToString().Should().Contain("Getting Started");
        topicOutput.ToString().Should().Contain("install the tool");
    }

    [Fact]
    public void FromMarkdown_registers_topics_with_docs_command()
    {
        var markdown = "# Getting Started\n\nInstall the tool.\n\n# Advanced Usage\n\nUse flags.\n";
        var catalog = DocsTopicCatalog.FromMarkdownByHeadingLevel(markdown, 1);

        var rootCommand = new RootCommand("sample");
        rootCommand.AddDocsCommand(catalog);

        var output = new StringWriter();
        var exitCode = rootCommand.Parse("docs --topic getting-started").Invoke(new() { Output = output });

        using var scope = new AssertionScope();
        exitCode.Should().Be(0);
        output.ToString().Should().Contain("Getting Started");
        output.ToString().Should().Contain("Install the tool.");
    }

    [Fact]
    public void Merged_catalogs_register_combined_topics_with_docs_command()
    {
        var part1 = DocsTopicCatalog.FromMarkdownByHeadingLevel("# Installation\n\nStep 1: Download.\n", 1);
        var part2 = DocsTopicCatalog.FromMarkdownByHeadingLevel("# Installation\n\nStep 2: Configure.\n", 1);
        var merged = DocsTopicCatalog.Merge(part1, part2);

        var rootCommand = new RootCommand("sample");
        rootCommand.AddDocsCommand(merged);

        var output = new StringWriter();
        var exitCode = rootCommand.Parse("docs --topic installation").Invoke(new() { Output = output });

        using var scope = new AssertionScope();
        exitCode.Should().Be(0);
        output.ToString().Should().Contain("Step 1: Download.");
        output.ToString().Should().Contain("Step 2: Configure.");
    }

    [Fact]
    public void Shared_heading_topics_register_in_docs_command()
    {
        // A heading that maps to multiple topics
        var markdown = "# Common Setup\n\nDo this first.\n";

        var catalog = DocsTopicCatalog.FromMarkdown(markdown, context =>
        {
            context.AppendToTopic("installation");
            context.AppendToTopic("troubleshooting");
        });

        var rootCommand = new RootCommand("sample");
        rootCommand.AddDocsCommand(catalog);

        var installOutput = new StringWriter();
        var troubleshootOutput = new StringWriter();

        var installExitCode = rootCommand.Parse("docs --topic installation").Invoke(new() { Output = installOutput });
        var troubleshootExitCode = rootCommand.Parse("docs --topic troubleshooting").Invoke(new() { Output = troubleshootOutput });

        using var scope = new AssertionScope();
        installExitCode.Should().Be(0);
        troubleshootExitCode.Should().Be(0);
        installOutput.ToString().Should().Contain("Do this first.");
        troubleshootOutput.ToString().Should().Contain("Do this first.");
    }

    [Fact]
    public void Custom_mapper_selective_includes_appear_in_docs_command()
    {
        var markdown = "# Setup\n\nInstall.\n\n# Ignored Section\n\nThis won't be a topic.\n\n# Teardown\n\nUninstall.\n";

        var catalog = DocsTopicCatalog.FromMarkdown(markdown, context =>
        {
            if (context.HeadingText == "Setup")
            {
                context.AppendToTopic("setup");
                return;
            }

            if (context.HeadingText == "Teardown")
            {
                context.AppendToTopic("teardown");
            }
        });

        var rootCommand = new RootCommand("sample");
        rootCommand.AddDocsCommand(catalog);

        var docsOutput = new StringWriter();
        var docsExitCode = rootCommand.Parse("docs").Invoke(new() { Output = docsOutput });

        using var scope = new AssertionScope();
        docsExitCode.Should().Be(0);
        docsOutput.ToString().Should().Contain("setup");
        docsOutput.ToString().Should().Contain("teardown");
        docsOutput.ToString().Should().NotContain("Ignored Section");
    }

    [Fact]
    public void Docs_command_lists_all_topics_from_custom_catalog()
    {
        var markdown = "# First\n\nContent.\n\n# Second\n\nMore content.\n";
        var catalog = DocsTopicCatalog.FromMarkdownByHeadingLevel(markdown, 1);

        var rootCommand = new RootCommand("sample");
        rootCommand.AddDocsCommand(catalog);

        var output = new StringWriter();
        var exitCode = rootCommand.Parse("docs").Invoke(new() { Output = output });

        using var scope = new AssertionScope();
        exitCode.Should().Be(0);
        output.ToString().Should().Contain("first");
        output.ToString().Should().Contain("second");
    }
}