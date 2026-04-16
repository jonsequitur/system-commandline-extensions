// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.CommandLine;

internal static class LocalizationResources
{
    internal static string HelpOptionDescription() => "Show help and usage information.";

    internal static string HelpUsageTitle() => "Usage:";

    internal static string HelpDescriptionTitle() => "Description:";

    internal static string HelpUsageOptions() => "[options]";

    internal static string HelpUsageCommand() => "[command]";

    internal static string HelpUsageAdditionalArguments() => "[[--] <additional arguments>...]";

    internal static string HelpArgumentsTitle() => "Arguments:";

    internal static string HelpOptionsTitle() => "Options:";

    internal static string HelpOptionsRequiredLabel() => "(REQUIRED)";

    internal static string HelpArgumentDefaultValueLabel() => "default";

    internal static string HelpCommandsTitle() => "Commands:";

    internal static string HelpAdditionalArgumentsTitle() => "Additional Arguments:";

    internal static string HelpAdditionalArgumentsDescription() => "Arguments passed to the application that is being run.";
}
