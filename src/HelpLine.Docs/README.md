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

var rootCommand = new RootCommand("sample");
rootCommand.AddDocsCommand();
```

This discovers embedded Markdown topics from the calling assembly and adds a `docs` subcommand.

## 4. Use at runtime

```powershell
sample docs list
sample docs --topic getting-started
```

Topic names come from the Markdown file names. For example, `Docs/getting-started.md` becomes the `getting-started` topic.

## 5. Advanced usage

To discover topics from a specific assembly:

```csharp
rootCommand.AddDocsCommand(typeof(Program).Assembly);
```

For full control over topic sources, construct a `DocsTopicCatalog` directly:

```csharp
var catalog = DocsTopicCatalog.FromAssemblyResources(typeof(Program).Assembly);
rootCommand.AddDocsCommand(catalog);
```
