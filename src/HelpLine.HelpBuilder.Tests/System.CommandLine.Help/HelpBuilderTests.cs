// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using System.CommandLine.Help;
using System.IO;
using System.Linq;
using AwesomeAssertions;
using Xunit;
using static System.Environment;

namespace HelpLine.HelpBuilderTests.Help;

public partial class HelpBuilderTests
{
    private const int SmallMaxWidth = 70;
    private const int LargeMaxWidth = 200;
    private const int ColumnGutterWidth = 2;
    private const int IndentationWidth = 2;

    private readonly HelpBuilder _helpBuilder;
    private readonly StringWriter _console;
    private readonly string _executableName;
    private readonly string _columnPadding;
    private readonly string _indentation;

    public HelpBuilderTests()
    {
        _console = new();
        _helpBuilder = GetHelpBuilder(LargeMaxWidth);
        _columnPadding = new string(' ', ColumnGutterWidth);
        _indentation = new string(' ', IndentationWidth);
        _executableName = RootCommand.ExecutableName;
    }

    private static HelpBuilder GetHelpBuilder(int maxWidth = SmallMaxWidth) => new(maxWidth);

    [Fact]
    public void Synopsis_section_keeps_added_newlines()
    {
        RootCommand command = new($"test{NewLine}\r\ndescription with\nline breaks");

        _helpBuilder.Write(command, _console);

        var expected =
            $"{_indentation}test{NewLine}" +
            $"{_indentation}{NewLine}" +
            $"{_indentation}description with{NewLine}" +
            $"{_indentation}line breaks{NewLine}{NewLine}";

        _console.ToString().Should().Contain(expected);
    }

    [Fact]
    public void Synopsis_section_properly_wraps_description()
    {
        var longSynopsisText =
            "test\t" +
            "description with some tabs that is long enough to wrap to a\t" +
            "new line";

        RootCommand command = new(description: longSynopsisText);

        HelpBuilder helpBuilder = GetHelpBuilder(SmallMaxWidth);
        helpBuilder.Write(command, _console);

        var expected =
            $"{_indentation}test\tdescription with some tabs that is long enough to wrap to a\t{NewLine}" +
            $"{_indentation}new line{NewLine}{NewLine}";

        _console.ToString().Should().Contain(expected);
    }

    [Theory]
    [InlineData(1, 1, "<the-args>")]
    [InlineData(1, 2, "<the-args>...")]
    [InlineData(0, 2, "[<the-args>...]")]
    public void Usage_section_shows_arguments_if_there_are_arguments_for_command_when_there_is_one_argument(
        int minArity,
        int maxArity,
        string expectedArgsUsage)
    {
        Argument<string> argument = new("the-args")
        {
            Arity = new ArgumentArity(minArity, maxArity)
        };

        Command command = new("the-command", "command help")
        {
            argument,
            new Option<string>("--verbosity", "-v")
            {
                Description = "Sets the verbosity"
            }
        };

        RootCommand rootCommand = new();
        rootCommand.Subcommands.Add(command);

        new HelpBuilder(LargeMaxWidth).Write(command, _console);

        var expected =
            $"Usage:{NewLine}" +
            $"{_indentation}{_executableName} the-command {expectedArgsUsage} [options]";

        _console.ToString().Should().Contain(expected);
    }

    [Fact]
    public void Usage_section_for_subcommand_shows_names_of_parent_commands()
    {
        Command outer = new("outer", "the outer command");
        Command inner = new("inner", "the inner command");
        outer.Subcommands.Add(inner);
        Command innerEr = new("inner-er", "the inner-er command");
        inner.Subcommands.Add(innerEr);
        innerEr.Options.Add(new Option<string>("--some-option") { Description = "some option" });
        RootCommand rootCommand = new();
        rootCommand.Add(outer);

        _helpBuilder.Write(innerEr, _console);

        var expected =
            $"Usage:{NewLine}" +
            $"{_indentation}{_executableName} outer inner inner-er [options]";

        _console.ToString().Should().Contain(expected);
    }

    [Fact]
    public void Usage_section_shows_additional_arguments_when_TreatUnmatchedTokensAsErrors_is_false()
    {
        RootCommand command = new();
        Command subcommand = new("some-command", "Does something");
        command.Subcommands.Add(subcommand);
        subcommand.Options.Add(new Option<string>("-x") { Description = "Indicates whether x" });
        subcommand.TreatUnmatchedTokensAsErrors = false;

        _helpBuilder.Write(subcommand, _console);

        _console.ToString().Should().Contain("<additional arguments>");
    }

    [Fact]
    public void Arguments_section_is_included_if_there_are_commands_with_arguments_configured()
    {
        Command command = new("the-command", "command help")
        {
            new Argument<string>("arg command name")
            {
                Description = "test"
            }
        };

        _helpBuilder.Write(command, _console);

        _console.ToString().Should().Contain("Arguments:");
    }

    [Fact]
    public void Arguments_section_includes_configured_argument_aliases()
    {
        Command command = new("the-command", "command help")
        {
            new Option<string>("--verbosity", "-v")
            {
                HelpName = "LEVEL",
                Description = "Sets the verbosity."
            }
        };

        _helpBuilder.Write(command, _console);

        var help = _console.ToString();
        help.Should().Contain("-v, --verbosity <LEVEL>");
        help.Should().Contain("Sets the verbosity.");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Command_argument_usage_indicates_enums_values(bool nullable)
    {
        var description = "This is the argument description";

        Argument argument = nullable
            ? new Argument<FileAccess?>("arg")
            : new Argument<FileAccess>("arg");

        argument.Description = description;

        Command command = new("outer", "Help text for the outer command")
        {
            argument
        };

        HelpBuilder helpBuilder = GetHelpBuilder(SmallMaxWidth);
        helpBuilder.Write(command, _console);

        var expected =
            $"Arguments:{NewLine}" +
            $"{_indentation}<Read|ReadWrite|Write>{_columnPadding}{description}";

        _console.ToString().Should().Contain(expected);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Option_argument_usage_is_empty_for_boolean_values(bool nullable)
    {
        var description = "This is the option description";

        Option option = nullable
            ? new Option<bool?>("--opt") { Description = description }
            : new Option<bool>("--opt") { Description = description };

        Command command = new("outer", "Help text for the outer command")
        {
            option
        };

        HelpBuilder helpBuilder = GetHelpBuilder(SmallMaxWidth);
        helpBuilder.Write(command, _console);

        _console.ToString().Should().Contain($"--opt{_columnPadding}{description}");
    }

    [Fact]
    public void Help_describes_default_value_for_argument()
    {
        Argument<string> argument = new("the-arg")
        {
            Description = "Help text from HelpDetail",
            DefaultValueFactory = _ => "the-arg-value"
        };

        Command command = new("the-command", "Help text from description")
        {
            argument
        };

        HelpBuilder helpBuilder = GetHelpBuilder(SmallMaxWidth);
        helpBuilder.Write(command, _console);

        _console.ToString().Should().Contain("[default: the-arg-value]");
    }

    [Fact]
    public void Options_section_aligns_options_on_new_lines()
    {
        Command command = new("the-command", "Help text for the command")
        {
            new Option<string>("--aaa", "-a")
            {
                Description = "An option with 8 characters",
            },
            new Option<string>("--bbbbbbbbbb", "-b")
            {
                Description = "An option with 15 characters"
            }
        };

        _helpBuilder.Write(command, _console);

        var lines = _console.ToString()
                            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

        var optionA = lines.Last(line => line.Contains("-a", StringComparison.Ordinal));
        var optionB = lines.Last(line => line.Contains("-b", StringComparison.Ordinal));

        optionA.IndexOf("An option", StringComparison.Ordinal)
               .Should()
               .Be(optionB.IndexOf("An option", StringComparison.Ordinal));
    }

    [Fact]
    public void Required_options_are_indicated_when_argument_is_named()
    {
        RootCommand command = new()
        {
            new Option<string>("--required", "-r")
            {
                Required = true,
                HelpName = "ARG"
            }
        };

        _helpBuilder.Write(command, _console);

        _console.ToString().Should().Contain("-r, --required <ARG> (REQUIRED)");
    }

    [Fact]
    public void Help_option_is_shown_in_help()
    {
        RootCommand rootCommand = new();

        _helpBuilder.Write(rootCommand, _console);

        _console.ToString().Should().Contain($"-?, -h, --help{_columnPadding}Show help and usage information");
    }

    [Fact]
    public void Options_aliases_differing_only_by_prefix_are_deduplicated_favoring_dashed_prefixes()
    {
        RootCommand command = new()
        {
            new Option<string>("-x", "/x")
        };

        _helpBuilder.Write(command, _console);

        _console.ToString().Should().NotContain("/x");
    }

    [Fact]
    public void Option_aliases_are_shown_before_long_names_regardless_of_alphabetical_order()
    {
        RootCommand command = new()
        {
            new Option<string>("-z", "-a", "--zzz", "--aaa")
        };

        _helpBuilder.Write(command, _console);

        _console.ToString().Should().Contain("-a, -z, --aaa, --zzz");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void Constructor_ignores_non_positive_max_width(int maxWidth)
    {
        HelpBuilder helpBuilder = new(maxWidth);

        helpBuilder.MaxWidth.Should().Be(int.MaxValue);
    }

    [Fact]
    public void Commands_without_arguments_do_not_produce_extra_newlines_between_usage_and_options_sections()
    {
        RootCommand command = new()
        {
            new Option<string>("-x") { Description = "the-option-description" }
        };

        HelpBuilder helpBuilder = GetHelpBuilder();

        using StringWriter writer = new();
        helpBuilder.Write(command, writer);

        writer.ToString().Should().Contain($"[options]{NewLine}{NewLine}Options:");
    }
}
