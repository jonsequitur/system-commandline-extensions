using System.Text.RegularExpressions;

namespace HelpLine.Markdown.Rendering;

/// <summary>
/// Renders Markdown content in a console-friendly form.
/// </summary>
public sealed class MarkdownHelpRenderer
{
    private static readonly Regex BoldExpression = new(@"(\*\*|__)(.+?)(\*\*|__)", RegexOptions.Compiled);

    /// <summary>
    /// Additional heading levels to offset when rendering.
    /// </summary>
    public int HeadingLevelOffset { get; init; } = 1;

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

        foreach (var rawLine in markdown.Split(["\r\n", "\n"], StringSplitOptions.None))
        {
            var line = rawLine.TrimEnd();

            if (string.IsNullOrWhiteSpace(line))
            {
                writer.WriteLine();
                continue;
            }

            var headingLevel = CountHeadingLevel(line);

            if (headingLevel > 0)
            {
                var title = line[headingLevel..].Trim();
                WriteHeading(writer, title, headingLevel + HeadingLevelOffset);
                continue;
            }

            if (line.StartsWith("- ") || line.StartsWith("* "))
            {
                writer.Write(" • ");
                writer.WriteLine(ApplyInlineFormatting(line[2..], writer));
                continue;
            }

            if (line.StartsWith("> "))
            {
                writer.Write("   ");
                writer.WriteLine(ApplyInlineFormatting(line[2..], writer));
                continue;
            }

            writer.WriteLine(ApplyInlineFormatting(line, writer));
        }
    }

    private static string ApplyInlineFormatting(string value, TextWriter writer)
    {
        return BoldExpression.Replace(value, match => Emphasize(match.Groups[2].Value, writer));
    }

    private static int CountHeadingLevel(string line)
    {
        var count = 0;

        while (count < line.Length && line[count] == '#')
        {
            count++;
        }

        return count > 0 && count < line.Length && char.IsWhiteSpace(line[count])
            ? count
            : 0;
    }

    private static void WriteHeading(TextWriter writer, string title, int level)
    {
        var prefix = new string('#', Math.Clamp(level, 1, 6));
        writer.WriteLine(Colorize($"{prefix} {title}", writer, bold: true, colorCode: "96"));
    }

    private static string Emphasize(string text, TextWriter writer) => Colorize(text, writer, bold: true, colorCode: "93");

    private static string Colorize(string text, TextWriter writer, bool bold, string colorCode)
    {
        if (!SupportsAnsi(writer))
        {
            return text;
        }

        var boldCode = bold ? "1;" : string.Empty;
        return $"\u001b[{boldCode}{colorCode}m{text}\u001b[0m";
    }

    private static bool SupportsAnsi(TextWriter writer)
    {
        return ReferenceEquals(writer, Console.Out) && !Console.IsOutputRedirected;
    }
}
