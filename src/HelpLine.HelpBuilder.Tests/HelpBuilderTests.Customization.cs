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
    public class Customization
    {
        private readonly HelpBuilder _helpBuilder;
        private readonly StringWriter _console;
        private readonly string _columnPadding;
        private readonly string _indentation;

        public Customization()
        {
            _console = new();
            _helpBuilder = GetHelpBuilder(LargeMaxWidth);
            _columnPadding = new string(' ', ColumnGutterWidth);
            _indentation = new string(' ', IndentationWidth);
        }

        [Fact]
        public void Option_can_customize_displayed_default_value()
        {
            Option<string> option = new("--the-option") { DefaultValueFactory = _ => "not 42" };
            Command command = new("the-command", "command help")
            {
                option
            };

            _helpBuilder.CustomizeSymbol(option, defaultValue: "42");

            _helpBuilder.Write(command, _console);

            var expected =
                $"Options:{NewLine}" +
                $"{_indentation}--the-option <the-option>{_columnPadding}[default: 42]{NewLine}{NewLine}";

            _console.ToString().Should().Contain(expected);
        }

        [Fact]
        public void Option_can_customize_first_column_text()
        {
            Option<string> option = new("--the-option") { Description = "option description" };
            Command command = new("the-command", "command help")
            {
                option
            };

            _helpBuilder.CustomizeSymbol(option, firstColumnText: "other-name");
            _helpBuilder.Write(command, _console);

            var expected =
                $"Options:{NewLine}" +
                $"{_indentation}other-name{_columnPadding}option description{NewLine}{NewLine}";

            _console.ToString().Should().Contain(expected);
        }

        [Fact]
        public void Option_can_customize_first_column_text_based_on_parse_result()
        {
            Option<bool> option = new("option");
            Command commandA = new("a", "a command help") { option };
            Command commandB = new("b", "b command help") { option };
            Command command = new("root", "root command help") { commandA, commandB };
            const string optionAFirstColumnText = "option a help";
            const string optionBFirstColumnText = "option b help";

            HelpBuilder helpBuilder = new(LargeMaxWidth);
            helpBuilder.CustomizeSymbol(option, firstColumnText: ctx =>
                ctx.Command.Equals(commandA)
                    ? optionAFirstColumnText
                    : optionBFirstColumnText);

            command.Options.Add(new HelpOption
            {
                Action = new CustomHelpAction
                {
                    Builder = helpBuilder
                }
            });

            StringWriter output = new();
            command.Parse("root a -h").Invoke(new() { Output = output });
            output.ToString().Should().Contain(optionAFirstColumnText);

            output = new StringWriter();
            command.Parse("root b -h").Invoke(new() { Output = output });
            output.ToString().Should().Contain(optionBFirstColumnText);
        }

        [Fact]
        public void Command_arguments_can_customize_default_value()
        {
            Argument<string> argument = new("some-arg")
            {
                DefaultValueFactory = _ => "not 42"
            };

            Command command = new("the-command", "command help")
            {
                argument
            };

            _helpBuilder.CustomizeSymbol(argument, defaultValue: "42");
            _helpBuilder.Write(command, _console);

            var expected =
                $"Arguments:{NewLine}" +
                $"{_indentation}<some-arg>{_columnPadding}[default: 42]{NewLine}{NewLine}";

            _console.ToString().Should().Contain(expected);
        }

        [Fact]
        public void Customize_throws_when_symbol_is_null()
        {
            Action action = () => new HelpBuilder().CustomizeSymbol(null!, string.Empty);

            action.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Help_sections_can_be_replaced()
        {
            CustomHelpAction helpAction = new();
            helpAction.Builder.CustomizeLayout(CustomLayout);

            Command command = new("name")
            {
                new HelpOption { Action = helpAction }
            };

            StringWriter output = new();
            command.Parse("-h").Invoke(new() { Output = output });

            output.ToString().Should().Be($"one{NewLine}{NewLine}two{NewLine}{NewLine}three{NewLine}{NewLine}");

            static IEnumerable<Func<HelpContext, bool>> CustomLayout(HelpContext _)
            {
                yield return ctx => { ctx.Output.WriteLine("one"); return true; };
                yield return ctx => { ctx.Output.WriteLine("two"); return true; };
                yield return ctx => { ctx.Output.WriteLine("three"); return true; };
            }
        }

        [Fact]
        public void Help_sections_can_be_supplemented()
        {
            CustomHelpAction helpAction = new();
            helpAction.Builder.CustomizeLayout(CustomLayout);

            Command command = new("hello")
            {
                new HelpOption { Action = helpAction }
            };

            var defaultHelp = GetDefaultHelp(new Command("hello"));

            StringWriter output = new();
            command.Parse("-h").Invoke(new() { Output = output });

            output.ToString().Should().Be($"first{NewLine}{NewLine}{defaultHelp}{NewLine}last{NewLine}{NewLine}");

            static IEnumerable<Func<HelpContext, bool>> CustomLayout(HelpContext _)
            {
                yield return ctx => { ctx.Output.WriteLine("first"); return true; };

                foreach (var section in HelpBuilder.Default.GetLayout())
                {
                    yield return section;
                }

                yield return ctx => { ctx.Output.WriteLine("last"); return true; };
            }
        }

        [Fact]
        public void Layout_can_be_composed_dynamically_based_on_context()
        {
            HelpBuilder helpBuilder = new();
            Command commandWithTypicalHelp = new("typical");
            Command commandWithCustomHelp = new("custom");
            RootCommand command = new()
            {
                commandWithTypicalHelp,
                commandWithCustomHelp
            };

            command.Options.OfType<HelpOption>().Single().Action = new CustomHelpAction
            {
                Builder = helpBuilder
            };

            helpBuilder.CustomizeLayout(c =>
                c.Command == commandWithTypicalHelp
                    ? HelpBuilder.Default.GetLayout()
                    : new Func<HelpContext, bool>[]
                    {
                        ctx =>
                        {
                            ctx.Output.WriteLine("Custom layout!");
                            return true;
                        }
                    }.Concat(HelpBuilder.Default.GetLayout()));

            StringWriter typicalOutput = new();
            command.Parse("typical -h").Invoke(new() { Output = typicalOutput });

            StringWriter customOutput = new();
            command.Parse("custom -h").Invoke(new() { Output = customOutput });

            typicalOutput.ToString().Should().Be(GetDefaultHelp(commandWithTypicalHelp, trimOneNewline: false));
            customOutput.ToString().Should().Be($"Custom layout!{NewLine}{NewLine}{GetDefaultHelp(commandWithCustomHelp, trimOneNewline: false)}");
        }

        [Fact]
        public void Help_default_sections_can_be_wrapped()
        {
            Command command = new("test")
            {
                new Option<string>("--option")
                {
                    Description = "option description",
                    HelpName = "option"
                },
                new HelpOption
                {
                    Action = new CustomHelpAction
                    {
                        Builder = new HelpBuilder(30)
                    }
                }
            };

            StringWriter output = new();
            command.Parse("test -h").Invoke(new() { Output = output });

            output.ToString().Should().Be(
                $"Description:{NewLine}{NewLine}" +
                $"Usage:{NewLine}  test [options]{NewLine}{NewLine}" +
                $"Options:{NewLine}" +
                $"  --option   option {NewLine}" +
                $"  <option>   description{NewLine}" +
                $"  -?, -h,    Show help and {NewLine}" +
                $"  --help     usage information{NewLine}{NewLine}");
        }

        private static string GetDefaultHelp(Command command, bool trimOneNewline = true)
        {
            HelpOption defaultHelp = new();
            var index = command.Options.IndexOf(defaultHelp);

            if (index >= 0)
            {
                command.Options[index] = defaultHelp;
            }
            else
            {
                command.Options.Add(defaultHelp);
            }

            StringWriter writer = new();
            InvocationConfiguration config = new()
            {
                Output = writer
            };

            command.Parse("-h").Invoke(config);

            var output = writer.ToString();

            if (trimOneNewline)
            {
                output = output[..^NewLine.Length];
            }

            return output;
        }
    }
}
