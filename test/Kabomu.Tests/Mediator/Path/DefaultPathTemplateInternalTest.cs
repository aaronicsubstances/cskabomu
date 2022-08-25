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
    public class DefaultPathTemplateInternalTest
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
                BoundPath = "UI//",
                UnboundRequestTarget = "",
                PathValues = new Dictionary<string, string>
                {
                    { "ux", "" }, { "fallback", "metro" }
                }
            };
            IContext context = new DefaultContext();
            var actual = instance.Match(context, "UI//");
            ComparisonUtils.AssertPathMatchResult(expected, actual);
        }
    }
}
