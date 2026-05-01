using AwesomeAssertions;
using AwesomeAssertions.Execution;
using HelpLine.Docs;
using Markdig;

namespace HelpLine.Docs.Tests;

public class MarkdownHelpRendererTests
{
    [Fact]
    public void Inline_bold_is_rendered_from_parsed_markdown()
    {
        var renderer = new MarkdownHelpRenderer();
        var output = new StringWriter();

        renderer.Render("Use **sample help** to view topics.", output);

        output.ToString().Should().Contain("sample help");
    }

    [Fact]
    public void Inline_code_span_is_rendered_from_parsed_markdown()
    {
        var renderer = new MarkdownHelpRenderer();
        var output = new StringWriter();

        renderer.Render("Run `dotnet tool install` to get started.", output);

        output.ToString().Should().Contain("dotnet tool install");
    }

    [Fact]
    public void Render_accepts_already_parsed_MarkdownDocument()
    {
        var markdown = "# My Topic\n\nThis is a paragraph.\n";
        var document = Markdown.Parse(markdown);

        var renderer = new MarkdownHelpRenderer();
        var output = new StringWriter();

        renderer.Render(document, output);

        using var scope = new AssertionScope();
        output.ToString().Should().Contain("My Topic");
        output.ToString().Should().Contain("This is a paragraph.");
    }

    [Fact]
    public void Render_of_pre_parsed_document_matches_render_of_string()
    {
        var markdown = "# Title\n\nSome **bold** text and a `code span`.\n";
        var renderer = new MarkdownHelpRenderer();

        var fromString = new StringWriter();
        renderer.Render(markdown, fromString);

        var fromDocument = new StringWriter();
        renderer.Render(Markdown.Parse(markdown), fromDocument);

        fromDocument.ToString().Should().Be(fromString.ToString());
    }
}
