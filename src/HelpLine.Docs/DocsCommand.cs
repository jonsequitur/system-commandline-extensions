using System.CommandLine;
using System.CommandLine.Invocation;
using Spectre.Console;

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
        AllOption = new Option<bool>("--all")
        {
            Description = "Displays all documentation topics."
        };

        Options.Add(TopicOption);
        Options.Add(AllOption);
        Action = new ShowDocsTopicAction(Catalog, Renderer, TopicOption, AllOption);
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

    /// <summary>
    /// Displays all topics.
    /// </summary>
    internal Option<bool> AllOption { get; }

    private static void RenderAllTopics(TextWriter output, DocsTopicCatalog catalog, MarkdownHelpRenderer renderer)
    {
        var first = true;

        foreach (var topic in catalog.Topics)
        {
            if (!catalog.TryReadTopicText(topic, out var markdown))
            {
                continue;
            }

            if (!first)
            {
                output.WriteLine();
            }

            renderer.Render(markdown ?? string.Empty, output);
            first = false;
        }
    }

    private sealed class ShowDocsTopicAction(
        DocsTopicCatalog catalog,
        MarkdownHelpRenderer renderer,
        DocsTopicOption topicOption,
        Option<bool> allOption) : SynchronousCommandLineAction
    {
        private readonly DocsTopicCatalog _catalog = catalog;
        private readonly MarkdownHelpRenderer _renderer = renderer;
        private readonly DocsTopicOption _topicOption = topicOption;
        private readonly Option<bool> _allOption = allOption;
        private const string AllTopicsLabel = "[bold yellow]all topics[/]";

        public override int Invoke(ParseResult parseResult)
        {
            var output = parseResult.InvocationConfiguration.Output;
            var requestedTopic = parseResult.GetValue<string?>(_topicOption.Name);
            var showAll = parseResult.GetValue(_allOption);

            if (showAll)
            {
                RenderAllTopics(output, _catalog, _renderer);
                return 0;
            }

            if (string.IsNullOrWhiteSpace(requestedTopic))
            {
                if (_catalog.Topics.Count == 1 && _catalog.TryReadTopicText(_catalog.Topics[0], out var singleTopicMarkdown))
                {
                    _renderer.Render(singleTopicMarkdown ?? string.Empty, output);
                    return 0;
                }

                if (ReferenceEquals(output, Console.Out) && !Console.IsOutputRedirected)
                {
                    var prompt = new SelectionPrompt<string>();
                    prompt.Title = "[bold]Select a topic:[/]";
                    prompt.AddChoices([AllTopicsLabel, .. _catalog.Topics.Select(static t => t.Name)]);

                    var selected = AnsiConsole.Prompt(prompt);

                    if (string.Equals(selected, AllTopicsLabel, StringComparison.Ordinal))
                    {
                        RenderAllTopics(output, _catalog, _renderer);
                    }
                    else if (_catalog.TryGetTopic(selected, out var selectedTopic) &&
                        _catalog.TryReadTopicText(selectedTopic, out var selectedMarkdown))
                    {
                        _renderer.Render(selectedMarkdown ?? string.Empty, output);
                    }
                }
                else
                {
                    ListDocsTopicsCommand.WriteTopicList(output, _catalog);
                }

                return 0;
            }

            if (_catalog.TryGetTopic(requestedTopic, out var topic) && 
                _catalog.TryReadTopicText(topic, out var markdown))
            {
                _renderer.Render(markdown ?? string.Empty, output);
            }
            return 0;
        }
    }
}
