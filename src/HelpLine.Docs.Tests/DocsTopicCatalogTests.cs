using AwesomeAssertions;
using AwesomeAssertions.Execution;

namespace HelpLine.Docs.Tests;

public class DocsTopicCatalogTests
{
    [Fact]
    public void FromMarkdown_ByLevel_creates_one_topic_per_heading()
    {
        var markdown = "# Getting Started\n\nInstall the tool.\n\n# Advanced Usage\n\nUse flags.\n";
        var catalog = DocsTopicCatalog.FromMarkdown(markdown);

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
    public void FromMarkdown_custom_mapper_normalizes_topic_names_to_cli_format()
    {
        var markdown = "# Commands\n\nDetails.\n\n# Quick Start\n\nStart here.\n";

        var catalog = DocsTopicCatalog.FromMarkdown(markdown, context =>
        {
            context.AppendToTopic(context.HeadingText);
        });

        catalog.Topics.Select(t => t.Name).Should().BeEquivalentTo(["commands", "quick-start"]);
    }

    [Fact]
    public void Merge_combines_catalogs_with_same_topic_name()
    {
        var part1 = DocsTopicCatalog.FromMarkdown("# guide\n\nPart one.\n");
        var part2 = DocsTopicCatalog.FromMarkdown("# guide\n\nPart two.\n");

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
        var cat1 = DocsTopicCatalog.FromMarkdown("# alpha\n\nA.\n");
        var cat2 = DocsTopicCatalog.FromMarkdown("# beta\n\nB.\n");

        var merged = DocsTopicCatalog.Merge(cat1, cat2);

        merged.Topics.Select(t => t.Name).Should().BeEquivalentTo(["alpha", "beta"]);
    }

    [Fact]
    public void FromMarkdown_ByLevel_includes_sub_headings_in_parent_topic()
    {
        // H2 sub-headings should be included in the H1 topic, not treated as boundaries
        var markdown = "# Guide\n\n## Install\n\nRun the installer.\n\n## Next Steps\n\nUse flags.\n";
        var catalog = DocsTopicCatalog.FromMarkdown(markdown);

        using var scope = new AssertionScope();
        catalog.Topics.Should().ContainSingle(t => t.Name == "guide");
        catalog.TryReadTopicText(catalog.Topics.Single(t => t.Name == "guide"), out var text).Should().BeTrue();
        text.Should().Contain("Run the installer.");
        text.Should().Contain("Use flags.");
    }

    [Fact]
    public void HeadingContext_ParentHeadingText_is_null_when_no_document_name()
    {
        var markdown = "# Top Level\n\nContent.\n";
        string? observedParent = "not-set";

        DocsTopicCatalog.FromMarkdown(markdown, context =>
        {
            observedParent = context.ParentHeadingText;
            context.AppendToTopic("t");
        });

        observedParent.Should().BeNull();
    }

    [Fact]
    public void HeadingContext_ParentHeadingText_is_document_name_for_H1()
    {
        var markdown = "# Top Level\n\nContent.\n";
        string? observedParent = null;

        DocsTopicCatalog.FromMarkdown(markdown, context =>
        {
            observedParent = context.ParentHeadingText;
            context.AppendToTopic("t");
        }, documentName: "my-doc");

        observedParent.Should().Be("my-doc");
    }

    [Fact]
    public void HeadingContext_ParentHeadingText_is_H1_text_for_H2()
    {
        var markdown = "# Document Title\n\n## Section\n\nContent.\n";
        string? observedParent = null;

        DocsTopicCatalog.FromMarkdown(markdown, context =>
        {
            if (context.HeadingLevel == 2)
            {
                observedParent = context.ParentHeadingText;
            }
            context.AppendToTopic(context.HeadingText);
        });

        observedParent.Should().Be("Document Title");
    }

    [Fact]
    public void HeadingContext_ParentHeadingText_tracks_nearest_ancestor()
    {
        var markdown = "# Root\n\n## Parent\n\n### Child\n\nContent.\n";
        string? h2Parent = null;
        string? h3Parent = null;

        DocsTopicCatalog.FromMarkdown(markdown, context =>
        {
            if (context.HeadingLevel == 2)
            {
                h2Parent = context.ParentHeadingText;
            }
            else if (context.HeadingLevel == 3)
            {
                h3Parent = context.ParentHeadingText;
            }
            context.AppendToTopic(context.HeadingText);
        });

        using var scope = new AssertionScope();
        h2Parent.Should().Be("Root");
        h3Parent.Should().Be("Parent");
    }

    [Fact]
    public void HeadingContext_ParentHeadingText_resets_when_sibling_heading_appears()
    {
        var markdown = "# Doc\n\n## First\n\n### Deep\n\n## Second\n\nContent.\n";
        string? secondParent = null;

        DocsTopicCatalog.FromMarkdown(markdown, context =>
        {
            if (context.HeadingText == "Second")
            {
                secondParent = context.ParentHeadingText;
            }
            context.AppendToTopic(context.HeadingText);
        });

        secondParent.Should().Be("Doc");
    }

    [Fact]
    public void FromMarkdown_default_creates_a_topic_for_every_heading_with_hierarchy()
    {
        var markdown = "# Quick Start\n\nGo.\n\n# Concepts\n\nIntro.\n\n## Measuring\n\nDetails.\n";

        var catalog = DocsTopicCatalog.FromMarkdown(markdown);

        using var scope = new AssertionScope();
        catalog.Topics.Select(t => t.Name).Should().BeEquivalentTo(["quick-start", "concepts", "measuring"]);
        catalog.Topics.Single(t => t.Name == "quick-start").Level.Should().Be(1);
        catalog.Topics.Single(t => t.Name == "quick-start").ParentName.Should().BeNull();
        catalog.Topics.Single(t => t.Name == "concepts").Level.Should().Be(1);
        catalog.Topics.Single(t => t.Name == "measuring").Level.Should().Be(2);
        catalog.Topics.Single(t => t.Name == "measuring").ParentName.Should().Be("concepts");
        catalog.Topics.Single(t => t.Name == "measuring").ShortName.Should().Be("measuring");
    }

    [Fact]
    public void FromMarkdown_default_parent_topic_content_includes_descendant_sections()
    {
        var markdown = "# Concepts\n\nIntro.\n\n## Measuring\n\nMeasure details.\n\n# Other\n\nElsewhere.\n";

        var catalog = DocsTopicCatalog.FromMarkdown(markdown);

        using var scope = new AssertionScope();
        catalog.TryReadTopicText(catalog.Topics.Single(t => t.Name == "concepts"), out var concepts).Should().BeTrue();
        concepts.Should().Contain("Intro.");
        concepts.Should().Contain("Measure details.");
        concepts.Should().NotContain("Elsewhere.");

        catalog.TryReadTopicText(catalog.Topics.Single(t => t.Name == "measuring"), out var measuring).Should().BeTrue();
        measuring.Should().Contain("Measure details.");
        measuring.Should().NotContain("Intro.");
    }

    [Fact]
    public void FromMarkdown_default_qualifies_names_when_short_names_collide()
    {
        var markdown = "# Cars\n\n## Overview\n\nCar overview.\n\n# Boats\n\n## Overview\n\nBoat overview.\n";

        var catalog = DocsTopicCatalog.FromMarkdown(markdown);

        using var scope = new AssertionScope();
        catalog.Topics.Select(t => t.Name).Should().BeEquivalentTo(["cars", "cars-overview", "boats", "boats-overview"]);

        var carsOverview = catalog.Topics.Single(t => t.Name == "cars-overview");
        carsOverview.ShortName.Should().Be("overview");
        carsOverview.ParentName.Should().Be("cars");

        var boatsOverview = catalog.Topics.Single(t => t.Name == "boats-overview");
        boatsOverview.ShortName.Should().Be("overview");
        boatsOverview.ParentName.Should().Be("boats");
    }

    [Fact]
    public void FromMarkdown_default_does_not_qualify_when_short_name_is_unique()
    {
        var markdown = "# Cars\n\n## Wheels\n\nDetails.\n\n# Boats\n\n## Sails\n\nDetails.\n";

        var catalog = DocsTopicCatalog.FromMarkdown(markdown);

        catalog.Topics.Select(t => t.Name).Should().BeEquivalentTo(["cars", "wheels", "boats", "sails"]);
    }
}