# HelpLine.HelpBuilder

Source-compatible adapter for the System.CommandLine `HelpBuilder` API.

## Background

In System.CommandLine 2.0.0, `HelpBuilder` and associated types were made internal, but during the long beta period these types had been public and were widely used. This library re-publishes them in the `System.CommandLine.Help` namespace so that you can migrate from beta4 to 2.0.0+ with almost no code changes to your help customization.

Over time, this library will be updated to incorporate bug fixes from the internal System.CommandLine `HelpBuilder`. Additional functionality and improvements will be introduced that do not break source code compatibility.

## Usage

```csharp
using System.CommandLine;
using System.CommandLine.Help;

var rootCommand = new RootCommand("sample");

var helpBuilder = new HelpBuilder();
helpBuilder.CustomizeLayout(_ =>
[
    HelpBuilder.Default.SynopsisSection(),
    HelpBuilder.Default.CommandUsageSection(),
    HelpBuilder.Default.OptionsSection(),
    HelpBuilder.Default.SubcommandsSection(),
]);

rootCommand.UseHelpBuilder(helpBuilder);
```
