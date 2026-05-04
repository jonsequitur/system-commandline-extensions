using System.CommandLine;
using System.CommandLine.Invocation;

namespace HelpLine.Docs;

/// <summary>
/// Lists available documentation topics.
/// </summary>
public sealed class ListDocsTopicsCommand : Command
{
    public ListDocsTopicsCommand(DocsTopicCatalog catalog)
        : base("list", "Lists available documentation topics.")
    {
        Action = new ListAction(catalog);
    }

    internal static void WriteTopicList(TextWriter output, DocsTopicCatalog catalog)
    {
        output.WriteLine("Available documentation topics:");
        output.WriteLine();

        foreach (var topic in catalog.Topics)
        {
            output.WriteLine($"  {topic.Name}");
        }

        output.WriteLine();
        output.WriteLine("Usage example:");
        output.WriteLine($"  {RootCommand.ExecutableName} docs --topic <topic-name>");
    }

    private sealed class ListAction(DocsTopicCatalog catalog) : SynchronousCommandLineAction
    {
        private readonly DocsTopicCatalog _catalog = catalog;

        public override int Invoke(ParseResult parseResult)
        {
            WriteTopicList(parseResult.InvocationConfiguration.Output, _catalog);
            return 0;
        }
    }
}
