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
    public class DefaultPathTemplateInternalTest1
    {
        [Fact]
        public void TestMatch1a()
        {
            var instance = new DefaultPathTemplateInternal
            {
                ParsedExamples = new DefaultPathTemplateExampleInternal[]
                {
                    new DefaultPathTemplateExampleInternal
                    {
                        Tokens = new PathToken[0]
                    }
                }
            };
            IPathMatchResult expected = new DefaultPathMatchResultInternal
            {
                BoundPath = "/",
                UnboundRequestTarget = "",
                PathValues = new Dictionary<string, string>()
            };
            IContext context = null;
            var actual = instance.Match(context, "/");
            ComparisonUtils.AssertPathMatchResult(expected, actual);
        }

        [Fact]
        public void TestMatch1b()
        {
            var instance = new DefaultPathTemplateInternal
            {
                ParsedExamples = new DefaultPathTemplateExampleInternal[]
                {
                    new DefaultPathTemplateExampleInternal
                    {
                        Tokens = new PathToken[0]
                    }
                }
            };
            IPathMatchResult expected = null;
            IContext context = null;
            var actual = instance.Match(context, "/bread");
            ComparisonUtils.AssertPathMatchResult(expected, actual);
        }

        [Fact]
        public void TestMatch1c()
        {
            var instance = new DefaultPathTemplateInternal
            {
                ParsedExamples = new DefaultPathTemplateExampleInternal[]
                {
                    new DefaultPathTemplateExampleInternal
                    {
                        Tokens = new PathToken[0]
                    }
                }
            };
            IPathMatchResult expected = new DefaultPathMatchResultInternal
            {
                BoundPath = "",
                UnboundRequestTarget = "",
                PathValues = new Dictionary<string, string>()
            };
            IContext context = null;
            var actual = instance.Match(context, "");
            ComparisonUtils.AssertPathMatchResult(expected, actual);
        }

        [Fact]
        public void TestMatch1d()
        {
            var instance = new DefaultPathTemplateInternal
            {
                ParsedExamples = new DefaultPathTemplateExampleInternal[]
                {
                    new DefaultPathTemplateExampleInternal
                    {
                        Tokens = new PathToken[0]
                    }
                }
            };
            IPathMatchResult expected = null;
            IContext context = null;
            var actual = instance.Match(context, "bread");
            ComparisonUtils.AssertPathMatchResult(expected, actual);
        }

        [Fact]
        public void TestMatch1e()
        {
            var instance = new DefaultPathTemplateInternal
            {
                ParsedExamples = new DefaultPathTemplateExampleInternal[]
                {
                    new DefaultPathTemplateExampleInternal
                    {
                        Tokens = new PathToken[0]
                    }
                }
            };
            IPathMatchResult expected = null;
            IContext context = null;
            var actual = instance.Match(context, null);
            ComparisonUtils.AssertPathMatchResult(expected, actual);
        }

        [Fact]
        public void TestMatch2a()
        {
            var instance = new DefaultPathTemplateInternal
            {
                ParsedExamples = new DefaultPathTemplateExampleInternal[]
                {
                    new DefaultPathTemplateExampleInternal
                    {
                        MatchLeadingSlash = true,
                        MatchTrailingSlash = false,
                        Tokens = new PathToken[0]
                    }
                }
            };
            IPathMatchResult expected = new DefaultPathMatchResultInternal
            {
                BoundPath = "/",
                UnboundRequestTarget = "",
                PathValues = new Dictionary<string, string>()
            };
            IContext context = null;
            var actual = instance.Match(context, "/");
            ComparisonUtils.AssertPathMatchResult(expected, actual);
        }

        [Fact]
        public void TestMatch2b()
        {
            var instance = new DefaultPathTemplateInternal
            {
                ParsedExamples = new DefaultPathTemplateExampleInternal[]
                {
                    new DefaultPathTemplateExampleInternal
                    {
                        MatchLeadingSlash = true,
                        MatchTrailingSlash = false,
                        Tokens = new PathToken[0]
                    }
                }
            };
            IPathMatchResult expected = null;
            IContext context = null;
            var actual = instance.Match(context, "");
            ComparisonUtils.AssertPathMatchResult(expected, actual);
        }

        [Fact]
        public void TestMatch3a()
        {
            var instance = new DefaultPathTemplateInternal
            {
                ParsedExamples = new DefaultPathTemplateExampleInternal[]
                {
                    new DefaultPathTemplateExampleInternal
                    {
                        MatchLeadingSlash = false,
                        MatchTrailingSlash = false,
                        Tokens = new PathToken[0]
                    }
                }
            };
            IPathMatchResult expected = null;
            IContext context = null;
            var actual = instance.Match(context, "/");
            ComparisonUtils.AssertPathMatchResult(expected, actual);
        }

        [Fact]
        public void TestMatch3b()
        {
            var instance = new DefaultPathTemplateInternal
            {
                ParsedExamples = new DefaultPathTemplateExampleInternal[]
                {
                    new DefaultPathTemplateExampleInternal
                    {
                        MatchLeadingSlash = false,
                        MatchTrailingSlash = false,
                        Tokens = new PathToken[0]
                    }
                }
            };
            IPathMatchResult expected = new DefaultPathMatchResultInternal
            {
                BoundPath = "",
                UnboundRequestTarget = "",
                PathValues = new Dictionary<string, string>()
            };
            IContext context = null;
            var actual = instance.Match(context, "");
            ComparisonUtils.AssertPathMatchResult(expected, actual);
        }

        [Fact]
        public void TestMatch4a()
        {
            var instance = new DefaultPathTemplateInternal
            {
                ParsedExamples = new DefaultPathTemplateExampleInternal[]
                {
                    new DefaultPathTemplateExampleInternal
                    {
                        MatchLeadingSlash = true,
                        MatchTrailingSlash = true,
                        Tokens = new PathToken[0]
                    }
                }
            };
            IPathMatchResult expected = new DefaultPathMatchResultInternal
            {
                BoundPath = "/",
                UnboundRequestTarget = "",
                PathValues = new Dictionary<string, string>()
            };
            IContext context = null;
            var actual = instance.Match(context, "/");
            ComparisonUtils.AssertPathMatchResult(expected, actual);
        }

        [Fact]
        public void TestMatch4b()
        {
            var instance = new DefaultPathTemplateInternal
            {
                ParsedExamples = new DefaultPathTemplateExampleInternal[]
                {
                    new DefaultPathTemplateExampleInternal
                    {
                        MatchLeadingSlash = true,
                        MatchTrailingSlash = true,
                        Tokens = new PathToken[0]
                    }
                }
            };
            IPathMatchResult expected = null;
            IContext context = null;
            var actual = instance.Match(context, "");
            ComparisonUtils.AssertPathMatchResult(expected, actual);
        }

        [Fact]
        public void TestMatch5a()
        {
            var instance = new DefaultPathTemplateInternal
            {
                ParsedExamples = new DefaultPathTemplateExampleInternal[]
                {
                    new DefaultPathTemplateExampleInternal
                    {
                        Tokens = new PathToken[0]
                    }
                }
            };
            IPathMatchResult expected = new DefaultPathMatchResultInternal
            {
                BoundPath = "/",
                UnboundRequestTarget = "?k=v&k2=v2",
                PathValues = new Dictionary<string, string>()
            };
            IContext context = null;
            var actual = instance.Match(context, "/?k=v&k2=v2");
            ComparisonUtils.AssertPathMatchResult(expected, actual);
        }

        [Fact]
        public void TestMatch5b()
        {
            var instance = new DefaultPathTemplateInternal
            {
                ParsedExamples = new DefaultPathTemplateExampleInternal[]
                {
                    new DefaultPathTemplateExampleInternal
                    {
                        Tokens = new PathToken[0]
                    }
                }
            };
            IPathMatchResult expected = new DefaultPathMatchResultInternal
            {
                BoundPath = "",
                UnboundRequestTarget = "#k=v&k2=v2",
                PathValues = new Dictionary<string, string>()
            };
            IContext context = null;
            var actual = instance.Match(context, "#k=v&k2=v2");
            ComparisonUtils.AssertPathMatchResult(expected, actual);
        }

        [Fact]
        public void TestMatch6a()
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
                                Value = "ui"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "ux"
                            }
                        }
                    }
                }
            };
            IPathMatchResult expected = new DefaultPathMatchResultInternal
            {
                BoundPath = "/UI/material",
                UnboundRequestTarget = "?check=0#k=v&k2=v2",
                PathValues = new Dictionary<string, string>
                {
                    { "ux", "material" }
                }
            };
            IContext context = new DefaultContext();
            var actual = instance.Match(context, "/UI/material?check=0#k=v&k2=v2");
            ComparisonUtils.AssertPathMatchResult(expected, actual);
        }

        [Fact]
        public void TestMatch6b()
        {
            var instance = new DefaultPathTemplateInternal
            {
                ParsedExamples = new DefaultPathTemplateExampleInternal[]
                {
                    new DefaultPathTemplateExampleInternal
                    {
                        CaseSensitiveMatchEnabled = true,
                        Tokens = new PathToken[]
                        {
                            new PathToken
                            {
                                Type = PathToken.TokenTypeLiteral,
                                Value = "ui"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "ux"
                            }
                        }
                    }
                }
            };
            IPathMatchResult expected = new DefaultPathMatchResultInternal
            {
                BoundPath = "/ui/material/",
                UnboundRequestTarget = "?check=0#k=v&k2=v2",
                PathValues = new Dictionary<string, string>
                {
                    { "ux", "material" }
                }
            };
            IContext context = new DefaultContext();
            var actual = instance.Match(context, "/ui/material/?check=0#k=v&k2=v2");
            ComparisonUtils.AssertPathMatchResult(expected, actual);
        }

        [Fact]
        public void TestMatch6c()
        {
            var instance = new DefaultPathTemplateInternal
            {
                ParsedExamples = new DefaultPathTemplateExampleInternal[]
                {
                    new DefaultPathTemplateExampleInternal
                    {
                        MatchTrailingSlash = false,
                        Tokens = new PathToken[]
                        {
                            new PathToken
                            {
                                Type = PathToken.TokenTypeLiteral,
                                Value = "ui"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "ux"
                            }
                        }
                    }
                }
            };
            IPathMatchResult expected = null;
            IContext context = new DefaultContext();
            var actual = instance.Match(context, "/ui/material/?check=0#k=v&k2=v2");
            ComparisonUtils.AssertPathMatchResult(expected, actual);
        }

        [Fact]
        public void TestMatch6d()
        {
            var instance = new DefaultPathTemplateInternal
            {
                ParsedExamples = new DefaultPathTemplateExampleInternal[]
                {
                    new DefaultPathTemplateExampleInternal
                    {
                        CaseSensitiveMatchEnabled = true,
                        Tokens = new PathToken[]
                        {
                            new PathToken
                            {
                                Type = PathToken.TokenTypeLiteral,
                                Value = "ui"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "ux"
                            }
                        }
                    }
                }
            };
            IPathMatchResult expected = null;
            IContext context = new DefaultContext();
            var actual = instance.Match(context, "/UI/material?check=0#k=v&k2=v2");
            ComparisonUtils.AssertPathMatchResult(expected, actual);
        }

        [Fact]
        public void TestMatch6e()
        {
            var instance = new DefaultPathTemplateInternal
            {
                ParsedExamples = new DefaultPathTemplateExampleInternal[]
                {
                    new DefaultPathTemplateExampleInternal
                    {
                        MatchLeadingSlash = false,
                        Tokens = new PathToken[]
                        {
                            new PathToken
                            {
                                Type = PathToken.TokenTypeLiteral,
                                Value = "ui"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "ux"
                            }
                        }
                    }
                }
            };
            IPathMatchResult expected = null;
            IContext context = new DefaultContext();
            var actual = instance.Match(context, "/ui/material?check=0#k=v&k2=v2");
            ComparisonUtils.AssertPathMatchResult(expected, actual);
        }

        [Fact]
        public void TestMatch6f()
        {
            var instance = new DefaultPathTemplateInternal
            {
                ParsedExamples = new DefaultPathTemplateExampleInternal[]
                {
                    new DefaultPathTemplateExampleInternal
                    {
                        MatchTrailingSlash = true,
                        Tokens = new PathToken[]
                        {
                            new PathToken
                            {
                                Type = PathToken.TokenTypeLiteral,
                                Value = "ui"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "ux"
                            }
                        }
                    }
                }
            };
            IPathMatchResult expected = new DefaultPathMatchResultInternal
            {
                BoundPath = "/U%49/mat%65rial/",
                UnboundRequestTarget = "?",
                PathValues = new Dictionary<string, string>
                {
                    { "ux", "material" }
                }
            };
            IContext context = new DefaultContext();
            var actual = instance.Match(context, "/U%49/mat%65rial/?");
            ComparisonUtils.AssertPathMatchResult(expected, actual);
        }

        [Fact]
        public void TestMatch6g()
        {
            var instance = new DefaultPathTemplateInternal
            {
                ParsedExamples = new DefaultPathTemplateExampleInternal[]
                {
                    new DefaultPathTemplateExampleInternal
                    {
                        UnescapeNonWildCardSegments = false,
                        Tokens = new PathToken[]
                        {
                            new PathToken
                            {
                                Type = PathToken.TokenTypeLiteral,
                                Value = "u%49"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "ux"
                            }
                        }
                    }
                }
            };
            IPathMatchResult expected = new DefaultPathMatchResultInternal
            {
                BoundPath = "U%49/mat%65rial/",
                UnboundRequestTarget = "",
                PathValues = new Dictionary<string, string>
                {
                    { "ux", "mat%65rial" }
                }
            };
            IContext context = new DefaultContext();
            var actual = instance.Match(context, "U%49/mat%65rial/");
            ComparisonUtils.AssertPathMatchResult(expected, actual);
        }

        [Fact]
        public void TestMatch6h()
        {
            var instance = new DefaultPathTemplateInternal
            {
                ParsedExamples = new DefaultPathTemplateExampleInternal[]
                {
                    new DefaultPathTemplateExampleInternal
                    {
                        UnescapeNonWildCardSegments = false,
                        Tokens = new PathToken[]
                        {
                            new PathToken
                            {
                                Type = PathToken.TokenTypeLiteral,
                                Value = "UI"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "ux"
                            }
                        }
                    }
                }
            };
            IPathMatchResult expected = null;
            IContext context = new DefaultContext();
            var actual = instance.Match(context, "/U%49/material");
            ComparisonUtils.AssertPathMatchResult(expected, actual);
        }

        [Fact]
        public void TestMatch6i()
        {
            var instance = new DefaultPathTemplateInternal
            {
                ParsedExamples = new DefaultPathTemplateExampleInternal[]
                {
                    new DefaultPathTemplateExampleInternal
                    {
                        UnescapeNonWildCardSegments = false,
                        Tokens = new PathToken[]
                        {
                            new PathToken
                            {
                                Type = PathToken.TokenTypeLiteral,
                                Value = "UI"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "ux"
                            }
                        }
                    }
                }
            };
            IPathMatchResult expected = null;
            IContext context = new DefaultContext();
            var actual = instance.Match(context, "/UI/");
            ComparisonUtils.AssertPathMatchResult(expected, actual);
        }

        [Fact]
        public void TestMatch6j()
        {
            var instance = new DefaultPathTemplateInternal
            {
                ParsedExamples = new DefaultPathTemplateExampleInternal[]
                {
                    new DefaultPathTemplateExampleInternal
                    {
                        UnescapeNonWildCardSegments = false,
                        Tokens = new PathToken[]
                        {
                            new PathToken
                            {
                                Type = PathToken.TokenTypeLiteral,
                                Value = "UI"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "ux"
                            }
                        }
                    }
                }
            };
            IPathMatchResult expected = null;
            IContext context = new DefaultContext();
            var actual = instance.Match(context, "/UI/x/y");
            ComparisonUtils.AssertPathMatchResult(expected, actual);
        }

        [Fact]
        public void TestMatch6k()
        {
            var instance = new DefaultPathTemplateInternal
            {
                ParsedExamples = new DefaultPathTemplateExampleInternal[]
                {
                    new DefaultPathTemplateExampleInternal
                    {
                        UnescapeNonWildCardSegments = false,
                        Tokens = new PathToken[]
                        {
                            new PathToken
                            {
                                Type = PathToken.TokenTypeLiteral,
                                Value = "UI"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "ux"
                            }
                        }
                    }
                },
                DefaultValues = new Dictionary<string, string>
                {
                    { "ux", "matte" }, { "fallback", "metro" }
                }
            };
            IPathMatchResult expected = null;
            IContext context = new DefaultContext();
            var actual = instance.Match(context, "/UI//");
            ComparisonUtils.AssertPathMatchResult(expected, actual);
        }

        [Fact]
        public void TestMatch6L()
        {
            var instance = new DefaultPathTemplateInternal
            {
                ParsedExamples = new DefaultPathTemplateExampleInternal[]
                {
                    new DefaultPathTemplateExampleInternal
                    {
                        UnescapeNonWildCardSegments = false,
                        Tokens = new PathToken[]
                        {
                            new PathToken
                            {
                                Type = PathToken.TokenTypeLiteral,
                                Value = "UI"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "ux",
                                EmptySegmentAllowed = true
                            }
                        }
                    }
                },
                DefaultValues = new Dictionary<string, string>
                {
                    { "ux", "matte" }, { "fallback", "metro" }
                }
            };
            IPathMatchResult expected = new DefaultPathMatchResultInternal
            {
                BoundPath = "/UI//",
                UnboundRequestTarget = "",
                PathValues = new Dictionary<string, string>
                {
                    { "ux", "" }, { "fallback", "metro" }
                }
            };
            IContext context = new DefaultContext();
            var actual = instance.Match(context, "/UI//");
            ComparisonUtils.AssertPathMatchResult(expected, actual);
        }

        [Fact]
        public void TestMatch6m()
        {
            IContext context = new DefaultContext();
            var actualConstraintLogs = new List<string>();
            IPathConstraint intCheck = new ConfigurablePathConstraint
            {
                ExpectedContext = context,
                ExpectedDirection = ContextUtils.PathConstraintMatchDirectionMatch,
                ConstraintLogs = actualConstraintLogs,
                ReturnValue = false
            };
            var instance = new DefaultPathTemplateInternal
            {
                ParsedExamples = new DefaultPathTemplateExampleInternal[]
                {
                    new DefaultPathTemplateExampleInternal
                    {
                        UnescapeNonWildCardSegments = false,
                        Tokens = new PathToken[]
                        {
                            new PathToken
                            {
                                Type = PathToken.TokenTypeLiteral,
                                Value = "UI"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "ux",
                                EmptySegmentAllowed = true
                            }
                        }
                    }
                },
                DefaultValues = new Dictionary<string, string>
                {
                    { "ux", "matte" }, { "fallback", "metro" }
                },
                ConstraintFunctions = new Dictionary<string, IPathConstraint>
                {
                    { "int", intCheck }
                },
                AllConstraints = new Dictionary<string, IList<(string, string[])>>
                {
                    {
                        "ux",
                        new List<(string, string[])>
                        {
                            ("int", new string[]{ "short" })
                        }
                    }
                }
            };
            IPathMatchResult expected = null;
            var expectedConstraintLogs = new List<string>
            {
                "ux,short"
            };
            var actual = instance.Match(context, "UI//");
            Assert.Equal(expectedConstraintLogs, actualConstraintLogs);
            ComparisonUtils.AssertPathMatchResult(expected, actual);
        }

        [Fact]
        public void TestMatch6n()
        {
            IContext context = new DefaultContext();
            var actualConstraintLogs = new List<string>();
            IPathConstraint intCheck = new ConfigurablePathConstraint
            {
                ExpectedContext = context,
                ExpectedDirection = ContextUtils.PathConstraintMatchDirectionMatch,
                ConstraintLogs = actualConstraintLogs,
                ReturnValue = false
            };
            IPathConstraint strCheck = new ConfigurablePathConstraint
            {
                ExpectedContext = context,
                ExpectedDirection = ContextUtils.PathConstraintMatchDirectionMatch,
                ConstraintLogs = actualConstraintLogs,
                ReturnValue = true
            };
            var instance = new DefaultPathTemplateInternal
            {
                ParsedExamples = new DefaultPathTemplateExampleInternal[]
                {
                    new DefaultPathTemplateExampleInternal
                    {
                        UnescapeNonWildCardSegments = false,
                        Tokens = new PathToken[]
                        {
                            new PathToken
                            {
                                Type = PathToken.TokenTypeLiteral,
                                Value = "UI"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "ux",
                                EmptySegmentAllowed = true
                            }
                        }
                    },
                    new DefaultPathTemplateExampleInternal
                    {
                        Tokens = new PathToken[]
                        {
                            new PathToken
                            {
                                Type = PathToken.TokenTypeLiteral,
                                Value = "UI"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeLiteral,
                                Value = ""
                            }
                        }
                    }
                },
                DefaultValues = new Dictionary<string, string>
                {
                    { "ux", "matte" }, { "fallback", "metro" }
                },
                ConstraintFunctions = new Dictionary<string, IPathConstraint>
                {
                    { "int", intCheck }, { "str", strCheck }
                },
                AllConstraints = new Dictionary<string, IList<(string, string[])>>
                {
                    {
                        "ux",
                        new List<(string, string[])>
                        {
                            ("str", new string[]{ "5", "6" }),
                            ("int", new string[]{ "short" })
                        }
                    }
                }
            };
            IPathMatchResult expected = new DefaultPathMatchResultInternal
            {
                BoundPath = "UI//",
                UnboundRequestTarget = "",
                PathValues = new Dictionary<string, string>
                {
                    { "ux", "matte" }, { "fallback", "metro" }
                }
            };
            var expectedConstraintLogs = new List<string>
            {
                "ux,5,6", "ux,short"
            };
            var actual = instance.Match(context, "UI//");
            Assert.Equal(expectedConstraintLogs, actualConstraintLogs);
            ComparisonUtils.AssertPathMatchResult(expected, actual);
        }

        [Fact]
        public void TestMatch7a()
        {
            IContext context = new DefaultContext();
            var actualConstraintLogs = new List<string>();
            IPathConstraint intCheck = new ConfigurablePathConstraint
            {
                ExpectedContext = context,
                ExpectedDirection = ContextUtils.PathConstraintMatchDirectionMatch,
                ConstraintLogs = actualConstraintLogs,
                ReturnValue = true
            };
            IPathConstraint strCheck = new ConfigurablePathConstraint
            {
                ExpectedContext = context,
                ExpectedDirection = ContextUtils.PathConstraintMatchDirectionMatch,
                ConstraintLogs = actualConstraintLogs,
                ReturnValue = true
            };
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
                                Value = "UI"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeLiteral,
                                Value = "ux"
                            }
                        }
                    },
                    new DefaultPathTemplateExampleInternal
                    {
                        Tokens = new PathToken[]
                        {
                            new PathToken
                            {
                                Type = PathToken.TokenTypeLiteral,
                                Value = "UI"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeWildCard,
                                Value = "ux"
                            }
                        }
                    }
                },
                DefaultValues = new Dictionary<string, string>
                {
                    { "ux", "matte" }, { "fallback", "metro" }
                },
                ConstraintFunctions = new Dictionary<string, IPathConstraint>
                {
                    { "int", intCheck }, { "str", strCheck }
                },
                AllConstraints = new Dictionary<string, IList<(string, string[])>>
                {
                    {
                        "ux",
                        new List<(string, string[])>
                        {
                            ("str", new string[]{ "5", "6" }),
                            ("int", new string[]{ "short" })
                        }
                    }
                }
            };
            IPathMatchResult expected = new DefaultPathMatchResultInternal
            {
                BoundPath = "UI/",
                UnboundRequestTarget = "?disp",
                PathValues = new Dictionary<string, string>
                {
                    { "ux", null }, { "fallback", "metro" }
                }
            };
            var expectedConstraintLogs = new List<string>
            {
                "ux,5,6", "ux,short"
            };
            var actual = instance.Match(context, "UI/?disp");
            Assert.Equal(expectedConstraintLogs, actualConstraintLogs);
            ComparisonUtils.AssertPathMatchResult(expected, actual);
        }

        [Fact]
        public void TestMatch7b()
        {
            IContext context = new DefaultContext();
            var actualConstraintLogs = new List<string>();
            IPathConstraint intCheck = new ConfigurablePathConstraint
            {
                ExpectedContext = context,
                ExpectedDirection = ContextUtils.PathConstraintMatchDirectionMatch,
                ConstraintLogs = actualConstraintLogs,
                ReturnValue = true
            };
            IPathConstraint strCheck = new ConfigurablePathConstraint
            {
                ExpectedContext = context,
                ExpectedDirection = ContextUtils.PathConstraintMatchDirectionMatch,
                ConstraintLogs = actualConstraintLogs,
                ReturnValue = true
            };
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
                                Value = "UI"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeWildCard,
                                Value = "ux"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeLiteral,
                                Value = "luda"
                            }
                        }
                    },
                    new DefaultPathTemplateExampleInternal
                    {
                        Tokens = new PathToken[]
                        {
                            new PathToken
                            {
                                Type = PathToken.TokenTypeLiteral,
                                Value = "UI"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeLiteral,
                                Value = "ux"
                            }
                        }
                    }
                },
                DefaultValues = new Dictionary<string, string>
                {
                    { "ux", "matte" }, { "fallback", "metro" }
                },
                ConstraintFunctions = new Dictionary<string, IPathConstraint>
                {
                    { "int", intCheck }, { "str", strCheck }
                },
                AllConstraints = new Dictionary<string, IList<(string, string[])>>
                {
                    {
                        "ux",
                        new List<(string, string[])>
                        {
                            ("str", new string[]{ "5", "6" }),
                            ("int", new string[]{ "short" })
                        }
                    }
                }
            };
            IPathMatchResult expected = new DefaultPathMatchResultInternal
            {
                BoundPath = "/UI/ux",
                UnboundRequestTarget = "#disp",
                PathValues = new Dictionary<string, string>
                {
                    { "ux", "matte" }, { "fallback", "metro" }
                }
            };
            var expectedConstraintLogs = new List<string>();
            var actual = instance.Match(context, "/UI/ux#disp");
            Assert.Equal(expectedConstraintLogs, actualConstraintLogs);
            ComparisonUtils.AssertPathMatchResult(expected, actual);
        }

        [Fact]
        public void TestMatch7c()
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
                                Value = "UI"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeWildCard,
                                Value = "ux"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeLiteral,
                                Value = "luda"
                            }
                        }
                    },
                    new DefaultPathTemplateExampleInternal
                    {
                        Tokens = new PathToken[]
                        {
                            new PathToken
                            {
                                Type = PathToken.TokenTypeLiteral,
                                Value = "UI"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeLiteral,
                                Value = "ux"
                            }
                        }
                    }
                },
                DefaultValues = new Dictionary<string, string>
                {
                    { "ux", "matte" }, { "fallback", "metro" }
                }
            };
            IPathMatchResult expected = new DefaultPathMatchResultInternal
            {
                BoundPath = "/UI//m/nn/%51ooo/luda",
                UnboundRequestTarget = "#disp?",
                PathValues = new Dictionary<string, string>
                {
                    { "ux", "//m/nn/%51ooo" }, { "fallback", "metro" }
                }
            };
            IContext context = new DefaultContext();
            var actual = instance.Match(context, "/UI//m/nn/%51ooo/luda#disp?");
            ComparisonUtils.AssertPathMatchResult(expected, actual);
        }

        [Fact]
        public void TestMatch7d()
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
                                Value = "UI"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeLiteral,
                                Value = "luda"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeWildCard,
                                Value = "ux"
                            }
                        }
                    }
                },
                DefaultValues = new Dictionary<string, string>
                {
                    { "ux", "matte" }, { "fallback", "metro" }
                }
            };
            IPathMatchResult expected = new DefaultPathMatchResultInternal
            {
                BoundPath = "/UI/luda",
                UnboundRequestTarget = "//m/nn/%51ooo?disp#",
                PathValues = new Dictionary<string, string>
                {
                    { "ux", "//m/nn/%51ooo" }, { "fallback", "metro" }
                }
            };
            IContext context = new DefaultContext();
            var actual = instance.Match(context, "/UI/luda//m/nn/%51ooo?disp#");
            ComparisonUtils.AssertPathMatchResult(expected, actual);
        }

        [Fact]
        public void TestMatch7e()
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
                                Type = PathToken.TokenTypeWildCard,
                                Value = "ux"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeLiteral,
                                Value = "UI"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeLiteral,
                                Value = "luda"
                            }
                        }
                    }
                },
                DefaultValues = new Dictionary<string, string>
                {
                    { "ux", "matte" }, { "fallback", "metro" }
                }
            };
            IPathMatchResult expected = new DefaultPathMatchResultInternal
            {
                BoundPath = "//m/nn/%51ooo/UI/luda",
                UnboundRequestTarget = "?disp#",
                PathValues = new Dictionary<string, string>
                {
                    { "ux", "//m/nn/%51ooo" }, { "fallback", "metro" }
                }
            };
            IContext context = new DefaultContext();
            var actual = instance.Match(context, "//m/nn/%51ooo/UI/luda?disp#");
            ComparisonUtils.AssertPathMatchResult(expected, actual);
        }

        [Fact]
        public void TestMatch7f()
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
                                Type = PathToken.TokenTypeWildCard,
                                Value = "ux"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeLiteral,
                                Value = "UI"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeLiteral,
                                Value = "luda"
                            }
                        }
                    }
                },
                DefaultValues = new Dictionary<string, string>
                {
                    { "ux", "matte" }
                }
            };
            IPathMatchResult expected = new DefaultPathMatchResultInternal
            {
                BoundPath = "/UI/luda",
                UnboundRequestTarget = "",
                PathValues = new Dictionary<string, string>
                {
                    { "ux", null }
                }
            };
            IContext context = new DefaultContext();
            var actual = instance.Match(context, "/UI/luda");
            ComparisonUtils.AssertPathMatchResult(expected, actual);
        }

        [Fact]
        public void TestMatch7g()
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
                                Type = PathToken.TokenTypeWildCard,
                                Value = "ux"
                            }
                        }
                    }
                }
            };
            IPathMatchResult expected = new DefaultPathMatchResultInternal
            {
                BoundPath = "",
                UnboundRequestTarget = "",
                PathValues = new Dictionary<string, string>
                {
                    { "ux", "" }
                }
            };
            IContext context = new DefaultContext();
            var actual = instance.Match(context, "");
            ComparisonUtils.AssertPathMatchResult(expected, actual);
        }

        [Fact]
        public void TestMatch7h()
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
                                Type = PathToken.TokenTypeWildCard,
                                Value = "ux"
                            }
                        }
                    }
                }
            };
            IPathMatchResult expected = new DefaultPathMatchResultInternal
            {
                BoundPath = "",
                UnboundRequestTarget = "/",
                PathValues = new Dictionary<string, string>
                {
                    { "ux", "/" }
                }
            };
            IContext context = new DefaultContext();
            var actual = instance.Match(context, "/");
            ComparisonUtils.AssertPathMatchResult(expected, actual);
        }

        [Fact]
        public void TestMatch7i()
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
                                Type = PathToken.TokenTypeWildCard,
                                Value = "ux"
                            }
                        }
                    }
                }
            };
            IPathMatchResult expected = new DefaultPathMatchResultInternal
            {
                BoundPath = "",
                UnboundRequestTarget = "/bread",
                PathValues = new Dictionary<string, string>
                {
                    { "ux", "/bread" }
                }
            };
            IContext context = new DefaultContext();
            var actual = instance.Match(context, "/bread");
            ComparisonUtils.AssertPathMatchResult(expected, actual);
        }

        [Fact]
        public void TestMatch7k()
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
                                Type = PathToken.TokenTypeWildCard,
                                Value = "ux"
                            }
                        }
                    }
                }
            };
            IPathMatchResult expected = new DefaultPathMatchResultInternal
            {
                BoundPath = "",
                UnboundRequestTarget = "/bread/?",
                PathValues = new Dictionary<string, string>
                {
                    { "ux", "/bread/" }
                }
            };
            IContext context = new DefaultContext();
            var actual = instance.Match(context, "/bread/?");
            ComparisonUtils.AssertPathMatchResult(expected, actual);
        }

        [Fact]
        public void TestMatch7L()
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
                                Type = PathToken.TokenTypeWildCard,
                                Value = "ux"
                            }
                        }
                    }
                }
            };
            IPathMatchResult expected = new DefaultPathMatchResultInternal
            {
                BoundPath = "",
                UnboundRequestTarget = "bread/?",
                PathValues = new Dictionary<string, string>
                {
                    { "ux", "bread/" }
                }
            };
            IContext context = new DefaultContext();
            var actual = instance.Match(context, "bread/?");
            ComparisonUtils.AssertPathMatchResult(expected, actual);
        }

        [Fact]
        public void TestMatch7m()
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
                                Type = PathToken.TokenTypeWildCard,
                                Value = "ux"
                            }
                        }
                    }
                }
            };
            IPathMatchResult expected = new DefaultPathMatchResultInternal
            {
                BoundPath = "",
                UnboundRequestTarget = "bread",
                PathValues = new Dictionary<string, string>
                {
                    { "ux", "bread" }
                }
            };
            IContext context = new DefaultContext();
            var actual = instance.Match(context, "bread");
            ComparisonUtils.AssertPathMatchResult(expected, actual);
        }

        [Fact]
        public void TestMatch7n()
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
                                Type = PathToken.TokenTypeWildCard,
                                Value = "ux"
                            }
                        }
                    }
                }
            };
            IPathMatchResult expected = new DefaultPathMatchResultInternal
            {
                BoundPath = "",
                UnboundRequestTarget = "/bread//of//life/?",
                PathValues = new Dictionary<string, string>
                {
                    { "ux", "/bread//of//life/" }
                }
            };
            IContext context = new DefaultContext();
            var actual = instance.Match(context, "/bread//of//life/?");
            ComparisonUtils.AssertPathMatchResult(expected, actual);
        }

        [Fact]
        public void TestMatch8a()
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
                                Value= "",
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeWildCard,
                                Value = "ux"
                            }
                        }
                    }
                }
            };
            IPathMatchResult expected = new DefaultPathMatchResultInternal
            {
                BoundPath = "/",
                UnboundRequestTarget = "/bread//of//life/?",
                PathValues = new Dictionary<string, string>
                {
                    { "ux", "/bread//of//life/" }
                }
            };
            IContext context = new DefaultContext();
            var actual = instance.Match(context, "//bread//of//life/?");
            ComparisonUtils.AssertPathMatchResult(expected, actual);
        }

        [Fact]
        public void TestMatch8b()
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
                                Value= "",
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeWildCard,
                                Value = "ux"
                            }
                        }
                    }
                }
            };
            IPathMatchResult expected = null;
            IContext context = new DefaultContext();
            var actual = instance.Match(context, "/bread//of//life/?");
            ComparisonUtils.AssertPathMatchResult(expected, actual);
        }

        [Fact]
        public void TestMatch8c()
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
                                Value= "tea",
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeWildCard,
                                Value = "ux"
                            }
                        }
                    }
                }
            };
            IPathMatchResult expected = new DefaultPathMatchResultInternal
            {
                BoundPath = "tea/",
                UnboundRequestTarget = "bread//of//life/?",
                PathValues = new Dictionary<string, string>
                {
                    { "ux", "bread//of//life/" }
                }
            };
            IContext context = new DefaultContext();
            var actual = instance.Match(context, "tea/bread//of//life/?");
            ComparisonUtils.AssertPathMatchResult(expected, actual);
        }

        [Fact]
        public void TestMatch8d()
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
                                Value= "tea",
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeWildCard,
                                Value = "ux"
                            }
                        }
                    }
                }
            };
            IPathMatchResult expected = new DefaultPathMatchResultInternal
            {
                BoundPath = "tea/",
                UnboundRequestTarget = "bread",
                PathValues = new Dictionary<string, string>
                {
                    { "ux", "bread" }
                }
            };
            IContext context = new DefaultContext();
            var actual = instance.Match(context, "tea/bread");
            ComparisonUtils.AssertPathMatchResult(expected, actual);
        }

        [Fact]
        public void TestMatch8e()
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
                                Value= "tea",
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeWildCard,
                                Value = "ux"
                            }
                        }
                    }
                }
            };
            IPathMatchResult expected = new DefaultPathMatchResultInternal
            {
                BoundPath = "tea",
                UnboundRequestTarget = "",
                PathValues = new Dictionary<string, string>
                {
                    { "ux", null }
                }
            };
            IContext context = new DefaultContext();
            var actual = instance.Match(context, "tea");
            ComparisonUtils.AssertPathMatchResult(expected, actual);
        }

        [Fact]
        public void TestMatch8f()
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
                                Type = PathToken.TokenTypeWildCard,
                                Value = "type"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeLiteral,
                                Value= "tea",
                            }
                        }
                    }
                }
            };
            IPathMatchResult expected = new DefaultPathMatchResultInternal
            {
                BoundPath = "green/tea",
                UnboundRequestTarget = "",
                PathValues = new Dictionary<string, string>
                {
                    { "type", "green" }
                }
            };
            IContext context = new DefaultContext();
            var actual = instance.Match(context, "green/tea");
            ComparisonUtils.AssertPathMatchResult(expected, actual);
        }

        [Fact]
        public void TestMatch8g()
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
                                Type = PathToken.TokenTypeWildCard,
                                Value = "type"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeLiteral,
                                Value= "tea",
                            }
                        }
                    }
                }
            };
            IPathMatchResult expected = new DefaultPathMatchResultInternal
            {
                BoundPath = "tea",
                UnboundRequestTarget = "",
                PathValues = new Dictionary<string, string>
                {
                    { "type", null }
                }
            };
            IContext context = new DefaultContext();
            var actual = instance.Match(context, "tea");
            ComparisonUtils.AssertPathMatchResult(expected, actual);
        }

        [Fact]
        public void TestMatch8i()
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
                                Type = PathToken.TokenTypeWildCard,
                                Value = "type"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeLiteral,
                                Value= "tea",
                            }
                        }
                    }
                }
            };
            IPathMatchResult expected = new DefaultPathMatchResultInternal
            {
                BoundPath = "/tea",
                UnboundRequestTarget = "",
                PathValues = new Dictionary<string, string>
                {
                    { "type", null }
                }
            };
            IContext context = new DefaultContext();
            var actual = instance.Match(context, "/tea");
            ComparisonUtils.AssertPathMatchResult(expected, actual);
        }

        [Fact]
        public void TestMatch8j()
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
                                Type = PathToken.TokenTypeWildCard,
                                Value = "type"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeLiteral,
                                Value= "tea",
                            }
                        }
                    }
                }
            };
            IPathMatchResult expected = new DefaultPathMatchResultInternal
            {
                BoundPath = "//tea",
                UnboundRequestTarget = "",
                PathValues = new Dictionary<string, string>
                {
                    { "type", "/" }
                }
            };
            IContext context = new DefaultContext();
            var actual = instance.Match(context, "//tea");
            ComparisonUtils.AssertPathMatchResult(expected, actual);
        }

        [Fact]
        public void TestMatch8k()
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
                                Value= "colour",
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeWildCard,
                                Value = "product"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value= "origin",
                            }
                        }
                    }
                }
            };
            IPathMatchResult expected = new DefaultPathMatchResultInternal
            {
                BoundPath = "/green/precious/te%61/from/chin%61%2fhills",
                UnboundRequestTarget = "",
                PathValues = new Dictionary<string, string>
                {
                    { "colour", "green" },
                    { "product", "/precious/te%61/from" },
                    { "origin", "china/hills" }
                }
            };
            IContext context = new DefaultContext();
            var actual = instance.Match(context, "/green/precious/te%61/from/chin%61%2fhills");
            ComparisonUtils.AssertPathMatchResult(expected, actual);
        }

        [Fact]
        public void TestMatch8L()
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
                                Value= "colour",
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeWildCard,
                                Value = "product"
                            }
                        }
                    }
                }
            };
            IPathMatchResult expected = new DefaultPathMatchResultInternal
            {
                BoundPath = "/green",
                UnboundRequestTarget = "/precious/te%61/from/chin%61%2fhills",
                PathValues = new Dictionary<string, string>
                {
                    { "colour", "green" },
                    { "product", "/precious/te%61/from/chin%61%2fhills" },
                }
            };
            IContext context = new DefaultContext();
            var actual = instance.Match(context, "/green/precious/te%61/from/chin%61%2fhills");
            ComparisonUtils.AssertPathMatchResult(expected, actual);
        }
    }
}
