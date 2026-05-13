# HelpLine.Docs

Provide rich documentation directly in your CLI app.

## 1. Add Markdown files to your source project

Create a `Docs\` folder in your project and add one or more Markdown files:

```text
MyCli/
  Docs/
    getting-started.md
    advanced-usage.md
```

## 2. Build-time embedding

When your project references the HelpLine.Docs package, its transitive MSBuild targets automatically embed all `*.md` files under `Docs\` as assembly resources. No `.csproj` changes are needed for the default setup.

To use a different source folder, set the MSBuild property:

```xml
<PropertyGroup>
  <HelpLineMarkdownTopicsRoot>MyCustomFolder</HelpLineMarkdownTopicsRoot>
</PropertyGroup>
```

## 3. Register the docs command

```csharp
using HelpLine.Docs;
using System.CommandLine;

var catalog = DocsTopicCatalog.FromAssemblyResources(typeof(Program).Assembly);

var rootCommand = new RootCommand("sample");
rootCommand.Add(new DocsCommand(catalog));
```

This discovers embedded Markdown topics from the specified assembly and adds a `docs` subcommand. Every Markdown heading becomes a topic. Sub-topics are nested under their enclosing heading and shown indented in the interactive topic chooser:

```text
> all topics
  quick-start
  concepts
    measuring
```

Topic content for a parent topic includes all of its descendant sections, so `--topic concepts` displays the `concepts` heading and everything below it (including `measuring`).

When two topics share the same short name (e.g., a `## Overview` under both `# Cars` and `# Boats`), each `--topic` value is qualified with its parent — `cars-overview` and `boats-overview`. The chooser still shows the unqualified `overview` under each parent.

## 4. Use at runtime

```powershell
sample docs list
sample docs --topic getting-started
```

## 5. Advanced usage

For full control over how headings map to topics, pass a heading mapper:

```csharp
var catalog = DocsTopicCatalog.FromAssemblyResources(typeof(Program).Assembly, context =>
{
    if (context.HeadingLevel == 2)
    {
        context.AppendToTopic(context.HeadingText);
    }
});

rootCommand.Add(new DocsCommand(catalog));
```

A mapped topic's content runs from its heading until the next *mapped* heading; unmapped headings are folded into the surrounding topic.
