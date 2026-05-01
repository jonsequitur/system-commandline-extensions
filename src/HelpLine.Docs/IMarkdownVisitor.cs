using Markdig.Extensions.Tables;
using Markdig.Syntax;

namespace HelpLine.Docs;

/// <summary>
/// Visits parsed Markdown block elements for rendering purposes.
/// </summary>
public interface IMarkdownVisitor
{
    /// <summary>
    /// Visits a heading block.
    /// </summary>
    void VisitHeading(HeadingBlock heading);

    /// <summary>
    /// Visits a paragraph block.
    /// </summary>
    void VisitParagraph(ParagraphBlock paragraph);

    /// <summary>
    /// Visits an indented code block.
    /// </summary>
    void VisitCode(CodeBlock code);

    /// <summary>
    /// Visits a fenced code block (e.g. ``` ... ```).
    /// </summary>
    void VisitFencedCode(FencedCodeBlock code);

    /// <summary>
    /// Visits a list item within a list block.
    /// </summary>
    void VisitListItem(ListItemBlock item, bool isOrdered);

    /// <summary>
    /// Visits a block quote.
    /// </summary>
    void VisitQuote(QuoteBlock quote);

    /// <summary>
    /// Visits a thematic break (horizontal rule).
    /// </summary>
    void VisitThematicBreak(ThematicBreakBlock rule);

    /// <summary>
    /// Visits a GFM-style pipe table.
    /// </summary>
    void VisitTable(Table table);
}
