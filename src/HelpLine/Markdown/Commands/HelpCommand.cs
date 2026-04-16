using System.CommandLine;
using System.CommandLine.Invocation;
using HelpLine.Markdown.Options;
using HelpLine.Markdown.Rendering;
using HelpLine.Markdown.Topics;

namespace HelpLine.Markdown.Commands;

/// <summary>
/// Displays embedded Markdown help topics.
/// </summary>
public sealed class HelpCommand : Command
{
    public HelpCommand(HelpTopicCatalog catalog, MarkdownHelpRenderer? renderer = null)
        : base("help", "Displays Markdown help topics packaged with the application.")
    {
        Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        Renderer = renderer ?? new MarkdownHelpRenderer();
        TopicOption = new HelpTopicOption(Catalog);

        Options.Add(TopicOption);
        Action = new ShowHelpTopicAction(Catalog, Renderer, TopicOption);
    }

    /// <summary>
    /// The topic catalog used by the command.
    /// </summary>
    public HelpTopicCatalog Catalog { get; }

    /// <summary>
    /// The renderer used when writing Markdown to the console.
    /// </summary>
    public MarkdownHelpRenderer Renderer { get; }

    /// <summary>
    /// The topic-selection option.
    /// </summary>
    public HelpTopicOption TopicOption { get; }

    private sealed class ShowHelpTopicAction(
        HelpTopicCatalog catalog,
        MarkdownHelpRenderer renderer,
        HelpTopicOption topicOption) : SynchronousCommandLineAction
    {
        private readonly HelpTopicCatalog _catalog = catalog;
        private readonly MarkdownHelpRenderer _renderer = renderer;
        private readonly HelpTopicOption _topicOption = topicOption;

        public override int Invoke(ParseResult parseResult)
        {
            var output = parseResult.InvocationConfiguration.Output;
            var requestedTopic = parseResult.GetValue<string?>(_topicOption.Name);

            if (string.IsNullOrWhiteSpace(requestedTopic))
            {
                if (_catalog.Topics.Count == 1 && _catalog.TryReadTopicText(_catalog.Topics[0], out var singleTopicMarkdown))
                {
                    _renderer.Render(singleTopicMarkdown ?? string.Empty, output);
                    return 0;
                }

                WriteTopicList(output, _catalog);
                return 0;
            }

            if (!_catalog.TryGetTopic(requestedTopic, out var topic) || topic is null)
            {
                output.WriteLine($"Unknown help topic '{requestedTopic}'.");
                output.WriteLine();
                WriteTopicList(output, _catalog);
                return 1;
            }

            if (!_catalog.TryReadTopicText(topic, out var markdown))
            {
                output.WriteLine($"The help topic '{topic.Name}' could not be loaded.");
                return 1;
            }

            _renderer.Render(markdown ?? string.Empty, output);
            return 0;
        }

        private static void WriteTopicList(TextWriter output, HelpTopicCatalog catalog)
        {
            if (catalog.Topics.Count == 0)
            {
                output.WriteLine("No Markdown help topics were found in the target assembly.");
                return;
            }

            output.WriteLine("Available help topics:");

            foreach (var topic in catalog.Topics)
            {
                output.WriteLine($"  {topic.Name} - {topic.Description}");
            }

            output.WriteLine();
            output.WriteLine("Use --topic <name> to display a specific topic.");
        }
    }
}
