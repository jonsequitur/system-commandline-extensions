// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine.Invocation;
using System.CommandLine.Parsing;

namespace System.CommandLine.Help;

/// <summary>
/// Provides command line help using the compatibility <see cref="HelpBuilder"/> surface.
/// </summary>
public sealed class CustomHelpAction : SynchronousCommandLineAction
{
    private HelpBuilder? _builder;

    /// <summary>
    /// Specifies the <see cref="HelpBuilder"/> used to format help output when help is requested.
    /// </summary>
    public HelpBuilder Builder
    {
        get => _builder ??= new HelpBuilder(Console.IsOutputRedirected ? int.MaxValue : Console.WindowWidth);
        set => _builder = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <inheritdoc />
    public override int Invoke(ParseResult parseResult)
    {
        var output = parseResult.InvocationConfiguration.Output;
        var helpContext = new HelpContext(Builder, parseResult.CommandResult.Command, output);

        Builder.Write(helpContext);

        return 0;
    }

    /// <inheritdoc />
    public override bool ClearsParseErrors => true;
}
