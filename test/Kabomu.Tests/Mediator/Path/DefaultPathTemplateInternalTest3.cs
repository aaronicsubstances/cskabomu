using Kabomu.Mediator.Handling;
using Kabomu.Mediator.Path;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using static Kabomu.Mediator.Path.DefaultPathTemplateExampleInternal;

namespace Kabomu.Tests.Mediator.Path
{
    public class DefaultPathTemplateInternalTest3
    {
        [Fact]
        public void TestInterpolate1()
        {
            var instance = new DefaultPathTemplateInternal
            {
                ParsedExamples = new DefaultPathTemplateExampleInternal[]
                {
                    new DefaultPathTemplateExampleInternal
                    {
                        Tokens = new PathToken[]
                        {
                            new PathToken
                            {
                                Type = PathToken.TokenTypeLiteral,
                                Value = "a"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeLiteral,
                                Value = "b"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeLiteral,
                                Value = "c"
                            }
                        }
                    }
                }
            };
            instance.AllConstraints = new Dictionary<string, IList<(string, string[])>>
            {
                {
                    "a",
                    new List<(string, string[])>
                    {
                        ("non-existent", new string[0]),
                    }
                },
                {
                    "b",
                    new List<(string, string[])>
                    {
                        ("non-existent", new string[1]),
                    }
                },
                {
                    "c",
                    new List<(string, string[])>
                    {
                        ("non-existent2", new string[0]),
                        ("non-existent2", new string[2])
                    }
                }
            };
            instance.ConstraintFunctions = new Dictionary<string, IPathConstraint>();
            var expected = new List<string>
            {
                "/a/b/c"
            };
            IContext context = new DefaultContext();
            var pathValues = new Dictionary<string, string>();
            DefaultPathTemplateFormatOptions formatOptions = null;
            var actual = instance.InterpolateAll(context, pathValues, formatOptions);
            Assert.Equal(expected, actual);
            Assert.Equal(expected[0], instance.Interpolate(context, pathValues, formatOptions));
        }

        [Fact]
        public void TestInterpolate2()
        {
            var instance = new DefaultPathTemplateInternal
            {
                ParsedExamples = new DefaultPathTemplateExampleInternal[]
                {
                    new DefaultPathTemplateExampleInternal
                    {
                        Tokens = new PathToken[]
                        {
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "d"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "e"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "f"
                            }
                        }
                    }
                }
            };
            instance.AllConstraints = new Dictionary<string, IList<(string, string[])>>
            {
                {
                    "a",
                    new List<(string, string[])>
                    {
                        ("non-existent", new string[0]),
                    }
                },
                {
                    "b",
                    new List<(string, string[])>
                    {
                        ("non-existent", new string[1]),
                    }
                },
                {
                    "c",
                    new List<(string, string[])>
                    {
                        ("non-existent2", new string[0]),
                        ("non-existent2", new string[2])
                    }
                }
            };
            instance.ConstraintFunctions = new Dictionary<string, IPathConstraint>();
            var expected = new List<string>
            {
                "/a/b/c"
            };
            IContext context = new DefaultContext();
            var pathValues = new Dictionary<string, string>
            {
                { "d", "a" }, { "e", "b" }, { "f", "c" }
            };
            DefaultPathTemplateFormatOptions formatOptions = null;
            var actual = instance.InterpolateAll(context, pathValues, formatOptions);
            Assert.Equal(expected, actual);
            Assert.Equal(expected[0], instance.Interpolate(context, pathValues, formatOptions));
        }

        [Fact]
        public void TestInterpolate3()
        {
            var instance = new DefaultPathTemplateInternal
            {
                ParsedExamples = new DefaultPathTemplateExampleInternal[]
                {
                    new DefaultPathTemplateExampleInternal
                    {
                        Tokens = new PathToken[]
                        {
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "d"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "e"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "f"
                            }
                        }
                    }
                },
                AllConstraints = new Dictionary<string, IList<(string, string[])>>
                {
                    {
                        "e",
                        new List<(string, string[])>
                        {
                            ("ok", new string[0]),
                            ("ok", new string[]{ "done" }),
                        }
                    },
                    {
                        "f",
                        new List<(string, string[])>
                        {
                            ("ok", new string[]{ "done", "again" }),
                            ("scr", new string[]{ "quite satisfied" }),
                        }
                    }
                }
            };
            IContext context = new DefaultContext();
            var pathValues = new Dictionary<string, string>
            {
                { "d", "a" }, { "e", "b" }, { "f", "c" }
            };
            var actualConstraintLogs = new List<string>();
            var cf1 = new ConfigurablePathConstraint
            {
                ExpectedContext = context,
                ExpectedDirection = ContextUtils.PathConstraintMatchDirectionFormat,
                ExpectedPathTemplate = instance,
                ExpectedValues = pathValues,
                ConstraintLogs = actualConstraintLogs,
                ReturnValue = true
            };
            var cf2 = new ConfigurablePathConstraint
            {
                ExpectedContext = context,
                ExpectedDirection = ContextUtils.PathConstraintMatchDirectionFormat,
                ExpectedPathTemplate = instance,
                ExpectedValues = pathValues,
                ConstraintLogs = actualConstraintLogs,
                ReturnValue = true,
            };
            instance.ConstraintFunctions = new Dictionary<string, IPathConstraint>
            {
                { "ok", cf1 }, { "scr", cf2 }
            };
            var expectedConstraintLogs = new List<string>
            {
                "e", "e,done", "f,done,again", "f,quite satisfied"
            };
            var expected = new List<string>
            {
                "/a/b/c"
            };
            DefaultPathTemplateFormatOptions formatOptions = null;
            var actual = instance.InterpolateAll(context, pathValues, formatOptions);
            Assert.Equal(expectedConstraintLogs, actualConstraintLogs);
            Assert.Equal(expected, actual);
            Assert.Equal(expected[0], instance.Interpolate(context, pathValues, formatOptions));
        }

        [Fact]
        public void TestInterpolate4()
        {
            var instance = new DefaultPathTemplateInternal
            {
                ParsedExamples = new DefaultPathTemplateExampleInternal[]
                {
                    new DefaultPathTemplateExampleInternal
                    {
                        Tokens = new PathToken[]
                        {
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "d"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "e"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "f"
                            }
                        }
                    },
                    new DefaultPathTemplateExampleInternal
                    {
                        Tokens = new PathToken[]
                        {
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "d"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "e"
                            }
                        }
                    }
                },
                AllConstraints = new Dictionary<string, IList<(string, string[])>>
                {
                    {
                        "e",
                        new List<(string, string[])>
                        {
                            ("ok", new string[0]),
                            ("ok", new string[]{ "done" }),
                        }
                    },
                    {
                        "f",
                        new List<(string, string[])>
                        {
                            ("ok", new string[]{ "done", "again" }),
                            ("scr", new string[]{ "not satisfied" }),
                        }
                    }
                },
                DefaultValues = new Dictionary<string, string>
                {
                    { "f", "c" }
                }
            };
            IContext context = new DefaultContext();
            var pathValues = new Dictionary<string, string>
            {
                { "d", "a" }, { "e", "b" }, { "f", "c" }
            };
            var actualConstraintLogs = new List<string>();
            var cf1 = new ConfigurablePathConstraint
            {
                ExpectedContext = context,
                ExpectedDirection = ContextUtils.PathConstraintMatchDirectionFormat,
                ExpectedPathTemplate = instance,
                ExpectedValues = pathValues,
                ConstraintLogs = actualConstraintLogs,
                ReturnValue = true
            };
            var cf2 = new ConfigurablePathConstraint
            {
                ExpectedContext = context,
                ExpectedDirection = ContextUtils.PathConstraintMatchDirectionFormat,
                ExpectedPathTemplate = instance,
                ExpectedValues = pathValues,
                ConstraintLogs = actualConstraintLogs,
                ReturnValue = false,
            };
            instance.ConstraintFunctions = new Dictionary<string, IPathConstraint>
            {
                { "ok", cf1 }, { "scr", cf2 }
            };
            var expectedConstraintLogs = new List<string>
            {
                "e", "e,done", "f,done,again", "f,not satisfied",
                "e", "e,done"
            };
            var expected = new List<string>
            {
                "/a/b"
            };
            DefaultPathTemplateFormatOptions formatOptions = null;
            var actual = instance.InterpolateAll(context, pathValues, formatOptions);
            Assert.Equal(expectedConstraintLogs, actualConstraintLogs);
            Assert.Equal(expected, actual);
            Assert.Equal(expected[0], instance.Interpolate(context, pathValues, formatOptions));
        }

        [Fact]
        public void TestInterpolate5()
        {
            var instance = new DefaultPathTemplateInternal
            {
                ParsedExamples = new DefaultPathTemplateExampleInternal[]
                {
                    new DefaultPathTemplateExampleInternal
                    {
                        Tokens = new PathToken[]
                        {
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "d"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "e"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "f"
                            }
                        }
                    },
                    new DefaultPathTemplateExampleInternal
                    {
                        Tokens = new PathToken[]
                        {
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "d"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "e"
                            }
                        }
                    }
                },
                AllConstraints = new Dictionary<string, IList<(string, string[])>>
                {
                    {
                        "e",
                        new List<(string, string[])>
                        {
                            ("ok", new string[0]),
                            ("ok", new string[]{ "done" }),
                        }
                    },
                    {
                        "f",
                        new List<(string, string[])>
                        {
                            ("ok", new string[]{ "done", "again" }),
                            ("scr", new string[]{ "not satisfied" }),
                        }
                    }
                },
                DefaultValues = new Dictionary<string, string>
                {
                    { "f", "c" }
                }
            };
            IContext context = new DefaultContext();
            var pathValues = new Dictionary<string, string>
            {
                { "d", "/" }, { "e", "b" }, { "f", "c" }
            };
            var actualConstraintLogs = new List<string>();
            var cf1 = new ConfigurablePathConstraint
            {
                ExpectedContext = context,
                ExpectedDirection = ContextUtils.PathConstraintMatchDirectionFormat,
                ExpectedPathTemplate = instance,
                ExpectedValues = pathValues,
                ConstraintLogs = actualConstraintLogs,
                ReturnValue = false
            };
            var cf2 = new ConfigurablePathConstraint
            {
                ExpectedContext = context,
                ExpectedDirection = ContextUtils.PathConstraintMatchDirectionFormat,
                ExpectedPathTemplate = instance,
                ExpectedValues = pathValues,
                ConstraintLogs = actualConstraintLogs,
                ReturnValue = false,
            };
            instance.ConstraintFunctions = new Dictionary<string, IPathConstraint>
            {
                { "ok", cf1 }, { "scr", cf2 }
            };
            var expectedConstraintLogs = new List<string>
            {
                "e", "e"
            };
            var expected = new List<string>();
            DefaultPathTemplateFormatOptions formatOptions = null;
            var actual = instance.InterpolateAll(context, pathValues, formatOptions);
            Assert.Equal(expectedConstraintLogs, actualConstraintLogs);
            Assert.Equal(expected, actual);
            Assert.Throws<PathTemplateInterpolationException>(() => instance.Interpolate(context, pathValues, formatOptions));
        }

        [Fact]
        public void TestInterpolate6()
        {
            var instance = new DefaultPathTemplateInternal
            {
                ParsedExamples = new DefaultPathTemplateExampleInternal[]
                {
                    new DefaultPathTemplateExampleInternal
                    {
                        Tokens = new PathToken[]
                        {
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "d"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "e"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "f"
                            }
                        }
                    },
                    new DefaultPathTemplateExampleInternal
                    {
                        Tokens = new PathToken[]
                        {
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "d"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "e"
                            }
                        }
                    }
                },
                AllConstraints = new Dictionary<string, IList<(string, string[])>>
                {
                    {
                        "e",
                        new List<(string, string[])>
                        {
                            ("ok", new string[0]),
                            ("ok", new string[]{ "done" }),
                        }
                    },
                    {
                        "f",
                        new List<(string, string[])>
                        {
                            ("ok", new string[]{ "done", "again" }),
                            ("scr", new string[]{ "not satisfied" }),
                        }
                    }
                },
                DefaultValues = new Dictionary<string, string>
                {
                    { "f", "c" }
                }
            };
            IContext context = new DefaultContext();
            var pathValues = new Dictionary<string, string>
            {
                { "d", "/" }, { "e", "b" }, { "f", "c" }
            };
            var actualConstraintLogs = new List<string>();
            var cf1 = new ConfigurablePathConstraint
            {
                ExpectedContext = context,
                ExpectedDirection = ContextUtils.PathConstraintMatchDirectionFormat,
                ExpectedPathTemplate = instance,
                ExpectedValues = pathValues,
                ConstraintLogs = actualConstraintLogs,
                ReturnValue = false
            };
            var cf2 = new ConfigurablePathConstraint
            {
                ExpectedContext = context,
                ExpectedDirection = ContextUtils.PathConstraintMatchDirectionFormat,
                ExpectedPathTemplate = instance,
                ExpectedValues = pathValues,
                ConstraintLogs = actualConstraintLogs,
                ReturnValue = false,
            };
            instance.ConstraintFunctions = new Dictionary<string, IPathConstraint>
            {
                { "ok", cf1 }, { "scr", cf2 }
            };
            var expectedConstraintLogs = new List<string>();
            var expected = new List<string>
            {
                "///b/c/", "///b/"
            };
            DefaultPathTemplateFormatOptions formatOptions = new DefaultPathTemplateFormatOptions
            {
                ApplyConstraints = false,
                ApplyLeadingSlash = true,
                ApplyTrailingSlash = true,
                CaseSensitiveMatchEnabled = true,
                EscapeNonWildCardSegments = false
            };
            var actual = instance.InterpolateAll(context, pathValues, formatOptions);
            Assert.Equal(expectedConstraintLogs, actualConstraintLogs);
            Assert.Equal(expected, actual);
            Assert.Equal(expected[1], instance.Interpolate(context, pathValues, formatOptions));
        }
    }
}
