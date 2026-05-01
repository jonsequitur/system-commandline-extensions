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

        foreach (var topic in catalog.Topics)
        {
            output.WriteLine($"  {topic.Name} - {topic.Description}");
        }

        output.WriteLine();
        output.WriteLine("Use --topic <name> to display a specific topic.");
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
