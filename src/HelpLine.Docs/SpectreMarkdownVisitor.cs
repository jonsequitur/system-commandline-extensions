using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Spectre.Console;
using Table = Markdig.Extensions.Tables.Table;
using TableCell = Markdig.Extensions.Tables.TableCell;
using TableRow = Markdig.Extensions.Tables.TableRow;

namespace HelpLine.Docs;

/// <summary>
/// Renders a parsed Markdown AST using Spectre.Console primitives.
/// Uses only stable markup-based APIs to avoid version compatibility issues.
/// </summary>
public sealed class SpectreMarkdownVisitor : IMarkdownVisitor
{
    /// <inheritdoc/>
    public void VisitHeading(HeadingBlock heading)
    {
        var title = RenderInlines(heading.Inline);

        if (heading.Level == 1)
        {
            AnsiConsole.MarkupLine($"[bold deepskyblue1]━━━━━━━━━━━━━━━━━━━━━━[/]");
            AnsiConsole.MarkupLine($"[bold deepskyblue1]{Markup.Escape(title)}[/]");
            AnsiConsole.MarkupLine($"[bold deepskyblue1]━━━━━━━━━━━━━━━━━━━━━━[/]");
            AnsiConsole.WriteLine();
        }
        else if (heading.Level == 2)
        {
            AnsiConsole.MarkupLine($"[bold yellow]→ {Markup.Escape(title)}[/]");
            AnsiConsole.WriteLine();
        }
        else
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[bold cyan]{Markup.Escape(title)}[/]");
            AnsiConsole.WriteLine();
        }
    }

    /// <inheritdoc/>
    public void VisitParagraph(ParagraphBlock paragraph)
    {
        AnsiConsole.MarkupLine(RenderInlinesMarkup(paragraph.Inline));
        AnsiConsole.WriteLine();
    }

    /// <inheritdoc/>
    public void VisitCode(CodeBlock code)
    {
        WriteCodeLines(code);
    }

    /// <inheritdoc/>
    public void VisitFencedCode(FencedCodeBlock code)
    {
        WriteCodeLines(code);
    }

    private static void WriteCodeLines(LeafBlock code)
    {
        var allLines = code.Lines.Lines;
        var lastNonEmpty = allLines.Length - 1;
        while (lastNonEmpty >= 0 && string.IsNullOrWhiteSpace(allLines[lastNonEmpty].Slice.ToString()))
        {
            lastNonEmpty--;
        }

        for (var i = 0; i <= lastNonEmpty; i++)
        {
            AnsiConsole.MarkupLine($"[dim]│[/] [grey]{Markup.Escape(allLines[i].Slice.ToString())}[/]");
        }
        AnsiConsole.WriteLine();
    }

    /// <inheritdoc/>
    public void VisitListItem(ListItemBlock item, bool isOrdered)
    {
        var bullet = isOrdered ? "◆" : "◇";
        foreach (var block in item)
        {
            if (block is ParagraphBlock paragraph)
            {
                AnsiConsole.MarkupLine($"[dim]{bullet}[/] {RenderInlinesMarkup(paragraph.Inline)}");
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
                AnsiConsole.MarkupLine($"[dim]┃[/] [italic]{RenderInlinesMarkup(paragraph.Inline)}[/]");
            }
        }
        AnsiConsole.WriteLine();
    }

    /// <inheritdoc/>
    public void VisitThematicBreak(ThematicBreakBlock rule)
    {
        AnsiConsole.MarkupLine("[dim]────────────────────────────────────────[/]");
        AnsiConsole.WriteLine();
    }

    /// <inheritdoc/>
    public void VisitTable(Table table)
    {
        var spectreTable = new Spectre.Console.Table();
        spectreTable.Border(TableBorder.Rounded);

        var rows = table.OfType<TableRow>().ToList();

        // First row becomes columns
        if (rows.Count > 0)
        {
            foreach (var cell in rows[0].OfType<TableCell>())
            {
                var text = RenderInlines(cell.Descendants<ParagraphBlock>().FirstOrDefault()?.Inline);
                spectreTable.AddColumn(new TableColumn($"[bold]{Markup.Escape(text)}[/]"));
            }
        }

        // Remaining rows become data
        foreach (var row in rows.Skip(1))
        {
            var cells = row.OfType<TableCell>()
                .Select(cell => Markup.Escape(RenderInlines(cell.Descendants<ParagraphBlock>().FirstOrDefault()?.Inline)))
                .ToArray();
            spectreTable.AddRow(cells);
        }

        AnsiConsole.Write(spectreTable);
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Renders an inline container (bold, emphasis, code spans, links, literal text) to a plain string.
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

    /// <summary>
    /// Renders an inline element to a Spectre markup string.
    /// </summary>
    private string RenderInlinesMarkup(ContainerInline? inlines)
    {
        if (inlines is null)
        {
            return string.Empty;
        }

        var sb = new System.Text.StringBuilder();

        foreach (var inline in inlines)
        {
            sb.Append(RenderInlineMarkup(inline));
        }

        return sb.ToString();
    }

    private string RenderInline(Inline inline) => inline switch
    {
        LiteralInline literal => literal.Content.ToString(),
        CodeInline code => Markup.Escape(code.Content),
        EmphasisInline emphasis when emphasis.DelimiterCount >= 2 => RenderInlines(emphasis),
        EmphasisInline emphasis => RenderInlines(emphasis),
        LinkInline link => RenderInlines(link),
        LineBreakInline => Environment.NewLine,
        ContainerInline container => RenderInlines(container),
        _ => string.Empty
    };

    private string RenderInlineMarkup(Inline inline) => inline switch
    {
        LiteralInline literal => Markup.Escape(literal.Content.ToString()),
        CodeInline code => $"[grey]{Markup.Escape(code.Content)}[/]",
        EmphasisInline emphasis when emphasis.DelimiterCount >= 2 => $"[bold]{RenderInlinesMarkup(emphasis)}[/]",
        EmphasisInline emphasis => $"[italic]{RenderInlinesMarkup(emphasis)}[/]",
        LinkInline link => RenderInlinesMarkup(link),
        LineBreakInline => Environment.NewLine,
        ContainerInline container => RenderInlinesMarkup(container),
        _ => Markup.Escape(inline.ToString() ?? string.Empty)
    };
}
