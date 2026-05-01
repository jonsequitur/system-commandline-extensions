using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Syntax;

namespace HelpLine.Docs;

/// <summary>
/// Renders Markdown content in a console-friendly form.
/// </summary>
public sealed class MarkdownHelpRenderer
{
    internal static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAutoLinks()
        .UsePipeTables()
        .Build();

    /// <summary>
    /// Renders Markdown content to the provided text writer.
    /// </summary>
    public void Render(string markdown, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        if (string.IsNullOrWhiteSpace(markdown))
        {
            writer.WriteLine("No help content is available.");
            return;
        }

        Render(Markdown.Parse(markdown, Pipeline), writer);
    }

    /// <summary>
    /// Renders an already-parsed Markdown document to the provided text writer.
    /// </summary>
    public void Render(MarkdownDocument document, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(writer);

        var visitor = new PlainTextMarkdownVisitor(writer);

        Walk(document, visitor);
    }

    private static void Walk(MarkdownDocument document, IMarkdownVisitor visitor)
    {
        foreach (var block in document)
        {
            VisitBlock(block, visitor);
        }
    }

    internal static void VisitBlock(Block block, IMarkdownVisitor visitor)
    {
        switch (block)
        {
            case HeadingBlock heading:
                visitor.VisitHeading(heading);
                break;
            case ParagraphBlock paragraph:
                visitor.VisitParagraph(paragraph);
                break;
            case FencedCodeBlock fenced:
                visitor.VisitFencedCode(fenced);
                break;
            case CodeBlock code:
                visitor.VisitCode(code);
                break;
            case ListBlock list:
                foreach (var item in list.OfType<ListItemBlock>())
                {
                    visitor.VisitListItem(item, list.IsOrdered);
                }
                break;
            case QuoteBlock quote:
                visitor.VisitQuote(quote);
                break;
            case ThematicBreakBlock rule:
                visitor.VisitThematicBreak(rule);
                break;
            case Table table:
                visitor.VisitTable(table);
                break;
        }
    }
}
