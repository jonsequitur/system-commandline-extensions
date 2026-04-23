# HelpLine.Docs

Provide rich documentation directly in your CLI app.

## 1. Add Markdown files to your source project

Create a `Docs\` folder in the application project that references `HelpLine.Docs`, then add one or more Markdown files:

```text
MyCli/
  Docs/
    getting-started.md
    advanced-usage.md
```

By default, all `*.md` files under `Docs\` are included recursively.

## 2. Build-time embedding

When your project references HelpLine.Docs, its build-transitive targets automatically embed those Markdown files as assembly resources during the normal build. You do not need to add explicit EmbeddedResource items yourself for the default setup.

If you want to use a different source folder, set the MSBuild property in your app project:

```xml
<PropertyGroup>
  <HelpLineMarkdownTopicsRoot>MyCustomFolder</HelpLineMarkdownTopicsRoot>
</PropertyGroup>
```

## 3. Register the docs command

Call `AddMarkdownHelp()` on your root command during startup:

```csharp
using HelpLine.Docs;
using System.CommandLine;

var rootCommand = new RootCommand("sample");
rootCommand.AddMarkdownHelp();
```

This discovers embedded Markdown topics from the target assembly and adds a `docs` subcommand with topic selection support.

## 4. Use at runtime

Once registered, users can browse topics from the command line:

```powershell
sample docs
sample docs --topic getting-started
```

Topic names come from the Markdown file names. For example, `Docs/getting-started.md` becomes the `getting-started` topic.

## 5. Advanced control

If the default entry-assembly discovery is not what you want, you can point HelpLine.Docs at a specific assembly:

```csharp
rootCommand.AddMarkdownHelp(typeof(Program).Assembly);
```

You can also construct and pass a `HelpTopicCatalog` yourself when you want complete control over the topic source and discovery behavior.
