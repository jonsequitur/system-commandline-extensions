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

var catalog = DocsTopicCatalog.FromAssemblyResourcesByHeadingLevel(typeof(Program).Assembly, 1);

var rootCommand = new RootCommand("sample");
rootCommand.Add(new DocsCommand(catalog));
```

This discovers embedded Markdown topics from the specified assembly and adds a `docs` subcommand.

## 4. Use at runtime

```powershell
sample docs list
sample docs --topic getting-started
```

Topic names come from the Markdown file names. For example, `Docs/getting-started.md` becomes the `getting-started` topic.

## 5. Advanced usage

For full control over how headings map to topics, use the `FromAssemblyResources` overload with a heading mapper:

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
