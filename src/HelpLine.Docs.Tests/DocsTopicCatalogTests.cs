using AwesomeAssertions;
using AwesomeAssertions.Execution;

namespace HelpLine.Docs.Tests;

public class DocsTopicCatalogTests
{
    [Fact]
    public void FromMarkdown_ByLevel_creates_one_topic_per_heading()
    {
        var markdown = "# Getting Started\n\nInstall the tool.\n\n# Advanced Usage\n\nUse flags.\n";
        var catalog = DocsTopicCatalog.FromMarkdownByHeadingLevel(markdown, 1);

        using var scope = new AssertionScope();
        catalog.Topics.Select(t => t.Name).Should().BeEquivalentTo(["getting-started", "advanced-usage"]);
        catalog.TryReadTopicText(catalog.Topics.Single(t => t.Name == "getting-started"), out var text).Should().BeTrue();
        text.Should().Contain("Install the tool.");
        catalog.TryReadTopicText(catalog.Topics.Single(t => t.Name == "advanced-usage"), out var text2).Should().BeTrue();
        text2.Should().Contain("Use flags.");
    }

    [Fact]
    public void FromMarkdown_custom_mapper_assigns_heading_to_named_topic()
    {
        var markdown = "# Setup\n\nInstall the tool.\n\n# Teardown\n\nRemove the tool.\n";

        var catalog = DocsTopicCatalog.FromMarkdown(markdown, context =>
        {
            if (context.HeadingText == "Setup")
            {
                context.AppendToTopic("setup");
            }
        });

        using var scope = new AssertionScope();
        catalog.Topics.Should().ContainSingle(t => t.Name == "setup");
        catalog.Topics.Should().NotContain(t => t.Name == "teardown");
        catalog.TryReadTopicText(catalog.Topics.Single(), out var text).Should().BeTrue();
        text.Should().Contain("Install the tool.");
        text.Should().Contain("Remove the tool.");
    }

    [Fact]
    public void FromMarkdown_heading_can_map_to_multiple_topics()
    {
        var markdown = "# Shared\n\nShared content.\n";

        var catalog = DocsTopicCatalog.FromMarkdown(markdown, context =>
        {
            context.AppendToTopic("topic-a");
            context.AppendToTopic("topic-b");
        });

        using var scope = new AssertionScope();
        catalog.Topics.Select(t => t.Name).Should().BeEquivalentTo(["topic-a", "topic-b"]);
        catalog.TryReadTopicText(catalog.Topics.Single(t => t.Name == "topic-a"), out var ta).Should().BeTrue();
        catalog.TryReadTopicText(catalog.Topics.Single(t => t.Name == "topic-b"), out var tb).Should().BeTrue();
        ta.Should().Be(tb);
    }

    [Fact]
    public void Merge_combines_catalogs_with_same_topic_name()
    {
        var part1 = DocsTopicCatalog.FromMarkdownByHeadingLevel("# guide\n\nPart one.\n", 1);
        var part2 = DocsTopicCatalog.FromMarkdownByHeadingLevel("# guide\n\nPart two.\n", 1);

        var merged = DocsTopicCatalog.Merge(part1, part2);

        using var scope = new AssertionScope();
        merged.Topics.Should().ContainSingle(t => t.Name == "guide");
        merged.TryReadTopicText(merged.Topics.Single(), out var text).Should().BeTrue();
        text.Should().Contain("Part one.");
        text.Should().Contain("Part two.");
    }

    [Fact]
    public void Merge_keeps_distinct_topics_from_both_catalogs()
    {
        var cat1 = DocsTopicCatalog.FromMarkdownByHeadingLevel("# alpha\n\nA.\n", 1);
        var cat2 = DocsTopicCatalog.FromMarkdownByHeadingLevel("# beta\n\nB.\n", 1);

        var merged = DocsTopicCatalog.Merge(cat1, cat2);

        merged.Topics.Select(t => t.Name).Should().BeEquivalentTo(["alpha", "beta"]);
    }

    [Fact]
    public void FromMarkdown_ByLevel_includes_sub_headings_in_parent_topic()
    {
        // H2 sub-headings should be included in the H1 topic, not treated as boundaries
        var markdown = "# Guide\n\n## Install\n\nRun the installer.\n\n## Next Steps\n\nUse flags.\n";
        var catalog = DocsTopicCatalog.FromMarkdownByHeadingLevel(markdown, 1);

        using var scope = new AssertionScope();
        catalog.Topics.Should().ContainSingle(t => t.Name == "guide");
        catalog.TryReadTopicText(catalog.Topics.Single(), out var text).Should().BeTrue();
        text.Should().Contain("Run the installer.");
        text.Should().Contain("Use flags.");
    }
}