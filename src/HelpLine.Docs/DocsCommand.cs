using System.CommandLine;
using System.CommandLine.Invocation;

namespace HelpLine.Docs;

/// <summary>
/// Displays embedded Markdown documentation topics.
/// </summary>
public sealed class DocsCommand : Command
{
    public DocsCommand(DocsTopicCatalog catalog, MarkdownHelpRenderer? renderer = null)
        : base("docs", "Displays Markdown documentation topics packaged with the application.")
    {
        Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        Renderer = renderer ?? new MarkdownHelpRenderer();
        TopicOption = new DocsTopicOption(Catalog);

        Options.Add(TopicOption);
        Action = new ShowDocsTopicAction(Catalog, Renderer, TopicOption);
        Subcommands.Add(new ListDocsTopicsCommand(Catalog));
    }

    /// <summary>
    /// The topic catalog used by the command.
    /// </summary>
    internal DocsTopicCatalog Catalog { get; }

    /// <summary>
    /// The renderer used when writing Markdown to the console.
    /// </summary>
    internal MarkdownHelpRenderer Renderer { get; }

    /// <summary>
    /// The topic-selection option.
    /// </summary>
    internal DocsTopicOption TopicOption { get; }

    private sealed class ShowDocsTopicAction(
        DocsTopicCatalog catalog,
        MarkdownHelpRenderer renderer,
        DocsTopicOption topicOption) : SynchronousCommandLineAction
    {
        private readonly DocsTopicCatalog _catalog = catalog;
        private readonly MarkdownHelpRenderer _renderer = renderer;
        private readonly DocsTopicOption _topicOption = topicOption;

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

                ListDocsTopicsCommand.WriteTopicList(output, _catalog);
                return 0;
            }

            _catalog.TryGetTopic(requestedTopic, out var topic);
            _catalog.TryReadTopicText(topic!, out var markdown);

            _renderer.Render(markdown ?? string.Empty, output);
            return 0;
        }
    }
}
