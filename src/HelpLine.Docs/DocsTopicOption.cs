using System.CommandLine;
using System.CommandLine.Completions;

namespace HelpLine.Docs;

/// <summary>
/// Selects a Markdown documentation topic to display.
/// </summary>
internal sealed class DocsTopicOption : Option<string?>
{
    public DocsTopicOption(DocsTopicCatalog? catalog = null)
        : base("--topic", ["-t"])
    {
        Description = "Displays detailed documentation for a specific topic";
        HelpName = "topic";

        if (catalog is null)
        {
            return;
        }

        CompletionSources.Add(_ => catalog.Topics.Select(static topic => new CompletionItem(topic.Name)));
    }
}
