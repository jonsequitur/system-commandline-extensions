// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace System.CommandLine;

/// <summary>
/// Provides API compatibility with upstream System.CommandLine properties
/// not yet present in the referenced package version.
/// </summary>
public static class RootCommandExtensions
{
    private static readonly ConditionalWeakTable<RootCommand, StrongBox<string?>> HelpNames = new();

    extension(RootCommand root)
    {
        public string? HelpName
        {
            get => HelpNames.TryGetValue(root, out var box) ? box.Value : RootCommand.ExecutableName;
            set
            {
                if (HelpNames.TryGetValue(root, out var box))
                    box.Value = value;
                else
                    HelpNames.Add(root, new StrongBox<string?>(value));
            }
        }
    }
}
