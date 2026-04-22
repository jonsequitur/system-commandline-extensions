using System.CommandLine;
using System.CommandLine.Completions;

namespace HelpLine.Docs;

/// <summary>
/// Selects a Markdown help topic to display.
/// </summary>
public sealed class HelpTopicOption : Option<string?>
{
    public HelpTopicOption(HelpTopicCatalog? catalog = null)
        : base("--topic", ["-t"])
    {
        Description = "Displays a specific help topic.";
        HelpName = "topic";

        if (catalog is null)
        {
            return;
        }

        CompletionSources.Add(_ => catalog.Topics.Select(static topic => new CompletionItem(topic.Name)));
    }
}
