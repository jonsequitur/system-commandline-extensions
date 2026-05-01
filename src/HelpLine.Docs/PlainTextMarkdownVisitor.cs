using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace HelpLine.Docs;

/// <summary>
/// Renders a parsed Markdown AST as plain text (with optional ANSI formatting) to a <see cref="TextWriter"/>.
/// </summary>
public sealed class PlainTextMarkdownVisitor : IMarkdownVisitor
{
    private readonly TextWriter _writer;

    public PlainTextMarkdownVisitor(TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        _writer = writer;
    }

    /// <inheritdoc/>
    public void VisitHeading(HeadingBlock heading)
    {
        var title = RenderInlines(heading.Inline);
        var prefix = new string('#', heading.Level);
        _writer.WriteLine(Colorize($"{prefix} {title}", bold: true, colorCode: "96"));
        _writer.WriteLine();
    }

    /// <inheritdoc/>
    public void VisitParagraph(ParagraphBlock paragraph)
    {
        _writer.WriteLine(RenderInlines(paragraph.Inline));
        _writer.WriteLine();
    }

    /// <inheritdoc/>
    public void VisitCode(CodeBlock code)
    {
        foreach (var line in code.Lines.Lines)
        {
            _writer.WriteLine("    " + line.Slice.ToString());
        }
        _writer.WriteLine();
    }

    /// <inheritdoc/>
    public void VisitFencedCode(FencedCodeBlock code)
    {
        foreach (var line in code.Lines.Lines)
        {
            _writer.WriteLine("    " + line.Slice.ToString());
        }
        _writer.WriteLine();
    }

    /// <inheritdoc/>
    public void VisitListItem(ListItemBlock item, bool isOrdered)
    {
        var bullet = isOrdered ? " • " : " • ";
        foreach (var block in item)
        {
            if (block is ParagraphBlock paragraph)
            {
                _writer.Write(bullet);
                _writer.WriteLine(RenderInlines(paragraph.Inline));
            }
        }
    }

    /// <inheritdoc/>
    public void VisitQuote(QuoteBlock quote)
    {
        foreach (var block in quote)
        {
            if (block is ParagraphBlock paragraph)
            {
                _writer.Write("   ");
                _writer.WriteLine(RenderInlines(paragraph.Inline));
            }
        }
        _writer.WriteLine();
    }

    /// <inheritdoc/>
    public void VisitThematicBreak(ThematicBreakBlock rule)
    {
        _writer.WriteLine(new string('-', 40));
        _writer.WriteLine();
    }

    /// <inheritdoc/>
    public void VisitTable(Table table)
    {
        foreach (var row in table.OfType<TableRow>())
        {
            var cells = row.OfType<TableCell>().Select(cell => RenderInlines(cell.Descendants<ParagraphBlock>().FirstOrDefault()?.Inline));
            _writer.WriteLine(string.Join(" | ", cells));
        }
        _writer.WriteLine();
    }

    /// <summary>
    /// Renders an inline container (bold, emphasis, code spans, links, literal text) to a string.
    /// </summary>
    private string RenderInlines(ContainerInline? inlines)
    {
        if (inlines is null)
        {
            return string.Empty;
        }

        var sb = new System.Text.StringBuilder();

        foreach (var inline in inlines)
        {
            sb.Append(RenderInline(inline));
        }

        return sb.ToString();
    }

    private string RenderInline(Inline inline) => inline switch
    {
        LiteralInline literal => literal.Content.ToString(),
        CodeInline code => Colorize(code.Content, bold: false, colorCode: "93"),
        EmphasisInline emphasis when emphasis.DelimiterCount >= 2 => Colorize(RenderInlines(emphasis), bold: true, colorCode: "93"),
        EmphasisInline emphasis => Colorize(RenderInlines(emphasis), bold: false, colorCode: "93"),
        LinkInline link => RenderInlines(link),
        LineBreakInline => Environment.NewLine,
        ContainerInline container => RenderInlines(container),
        _ => string.Empty
    };

    private bool SupportsAnsi() =>
        ReferenceEquals(_writer, Console.Out) && !Console.IsOutputRedirected;

    private string Colorize(string text, bool bold, string colorCode)
    {
        if (!SupportsAnsi())
        {
            return text;
        }

        var boldCode = bold ? "1;" : string.Empty;
        return $"\u001b[{boldCode}{colorCode}m{text}\u001b[0m";
    }
}
