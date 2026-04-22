# system-commandline-extensions

[![CI](https://github.com/jonsequitur/system-commandline-extensions/actions/workflows/ci.yml/badge.svg)](https://github.com/jonsequitur/system-commandline-extensions/actions/workflows/ci.yml)

This repo now contains libraries for extending System.CommandLine help:

- HelpLine.Docs: Provide rich documentation directly in your CLI app using Markdown
- HelpLine.HelpBuilder: Source code-compatible adapter for the System.CommandLine `HelpBuilder`

## Markdown-based rich help topics with HelpLine.Docs

HelpLine.Docs is designed to let you author help topics as Markdown files in your CLI app project, have them embedded at build time, and then expose them through System.CommandLine with one API call.

### 1. Add Markdown files to your source project

Create a `Docs\` folder in the application project that references `HelpLine`, then add one or more Markdown files:

```text
MyCli/
  Docs/
    getting-started.md
    advanced-usage.md
```

By default, all `*.md` files under `Docs\` are included recursively.

### 2. Build-time embedding

When your project references HelpLine.Docs, its build-transitive targets automatically embed those Markdown files as assembly resources during the normal build. You do not need to add explicit EmbeddedResource items yourself for the default setup.

If you want to use a different source folder, set the MSBuild property in your app project:

```xml
<PropertyGroup>
  <HelpLineMarkdownTopicsRoot>MyCustomFolder</HelpLineMarkdownTopicsRoot>
</PropertyGroup>
```

### 3. Register the runtime help command

Call `AddMarkdownHelp()` on your root command during startup:

```csharp
using HelpLine.Docs;
using System.CommandLine;

var rootCommand = new RootCommand("sample");
rootCommand.AddMarkdownHelp();
```

This does three things:

- discovers embedded Markdown topics from the target assembly
- adds a `help` command with topic selection support
- extends normal `-h` output to show the available help topics

### 4. Use the help at runtime

Once registered, users can browse topics from the command line:

```powershell
sample -h
sample help
sample help --topic getting-started
```

Topic names come from the Markdown file names. For example, `Help\getting-started.md` becomes the `getting-started` topic.

### 5. Advanced control

If the default entry-assembly discovery is not what you want, you can point HelpLine.Docs at a specific assembly:

```csharp
rootCommand.AddMarkdownHelp(typeof(Program).Assembly);
```

You can also construct and pass a `HelpTopicCatalog` yourself when you want complete control over the topic source and discovery behavior.

## Legacy System.CommandLine HelpBuilder

The HelpBuilder and associated types were made internal in System.CommandLine 2.0.0 but for the long beta4 period, these types had been public. They are now shipped in the HelpLine.HelpBuilder library while preserving the System.CommandLine.Help namespace. If you are migrating from System.CommandLine beta 4 to 2.0.0+ and want to keep using the old HelpBuilder to customize your CLI app's help, this library allows you to do so with no code changes.

# Developer Guide


Run `eng/Update-HelpCompatFromUpstream.ps1` to refresh the vendored `HelpCompat` files **and the copied `HelpBuilder` test coverage** from the latest `dotnet/command-line-api` source. The API compatibility snapshot tests are intentionally maintained locally in this repo.

## Build

```powershell
dotnet build .\system-commandline-extensions.slnx
dotnet test .\system-commandline-extensions.slnx
```

