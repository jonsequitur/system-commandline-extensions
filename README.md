# system-commandline-extensions

[![CI](https://github.com/jonsequitur/system-commandline-extensions/actions/workflows/ci.yml/badge.svg)](https://github.com/jonsequitur/system-commandline-extensions/actions/workflows/ci.yml)

Extensions for [System.CommandLine](https://github.com/dotnet/command-line-api) help.

| Package | Description |
|---------|-------------|
| [HelpLine.Docs](src/HelpLine.Docs) | Provide rich documentation directly in your CLI app using Markdown |
| [HelpLine.HelpBuilder](src/HelpLine.HelpBuilder) | Source-compatible adapter for the System.CommandLine `HelpBuilder` APIs |

## Preview packages

Preview packages are published to GitHub Packages here:

```
https://nuget.pkg.github.com/jonsequitur/index.json
```

# Developer Guide


Run `eng/Update-HelpCompatFromUpstream.ps1` to refresh the vendored `HelpCompat` files **and the copied `HelpBuilder` test coverage** from the latest `dotnet/command-line-api` source. The API compatibility snapshot tests are intentionally maintained locally in this repo.

## Build

```powershell
dotnet build .\system-commandline-extensions.slnx
dotnet test .\system-commandline-extensions.slnx
```

