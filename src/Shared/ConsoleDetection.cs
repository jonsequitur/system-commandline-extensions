namespace HelpLine;

/// <summary>
/// Shared utilities for detecting console capabilities.
/// </summary>
internal static class ConsoleDetection
{
    /// <summary>
    /// Returns true when Spectre.Console rich output is appropriate
    /// (i.e. writing to an interactive console, not redirected).
    /// </summary>
    internal static bool ShouldUseSpectre(TextWriter writer) =>
        ReferenceEquals(writer, Console.Out) && !Console.IsOutputRedirected;

    /// <summary>
    /// Returns true when the writer supports ANSI escape sequences.
    /// </summary>
    internal static bool SupportsAnsi(TextWriter writer) =>
        ReferenceEquals(writer, Console.Out) && !Console.IsOutputRedirected;
}
