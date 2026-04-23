# HelpLine.HelpBuilder

Source-compatible adapter for the System.CommandLine `HelpBuilder` API.

## Background

The `HelpBuilder` and associated types were made internal in System.CommandLine 2.0.0, but during the long beta4 period these types had been public and are wiely used. This library re-publishes them in the `System.CommandLine.Help` namespace so that you can migrate from beta4 to 2.0.0+ with almost no code changes to your help customization.

Over time, this library will be updated to incorporate bug fixes from the internal System.CommandLine `HelpBuilder`. Additional functionality and improvements will be introduced that do not break source code compatibility.

## Usage

```csharp
using System.CommandLine;
using System.CommandLine.Help;

var rootCommand = new RootCommand("sample");

var helpBuilder = new HelpBuilder(120);
helpBuilder.CustomizeLayout(_ =>
[
    HelpBuilder.Default.SynopsisSection(),
    HelpBuilder.Default.CommandUsageSection(),
    HelpBuilder.Default.OptionsSection(),
    HelpBuilder.Default.SubcommandsSection(),
]);

rootCommand.UseHelpBuilder(helpBuilder);
```

`UseHelpBuilder` applies the builder by relpacing the `HelpOption.Action` on all instances of `HelpOption` found in the target command or its descendants.
