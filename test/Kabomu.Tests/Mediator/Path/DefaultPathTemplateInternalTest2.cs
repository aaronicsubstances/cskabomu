using Kabomu.Mediator.Handling;
using Kabomu.Mediator.Path;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using static Kabomu.Mediator.Path.DefaultPathTemplateExampleInternal;

namespace Kabomu.Tests.Mediator.Path
{
    public class DefaultPathTemplateInternalTest2
    {
        [Fact]
        public void TestInterpolateForErrors1()
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
            IContext context = new DefaultContextInternal();
            var pathValues = new Dictionary<string, string>();
            object formatOptions = "problematic";
            Assert.ThrowsAny<Exception>(() => instance.InterpolateAll(context, pathValues, formatOptions));
            Assert.ThrowsAny<Exception>(() => instance.Interpolate(context, pathValues, formatOptions));
        }

        [Fact]
        public void TestInterpolateForErrors2()
        {
            var instance = new DefaultPathTemplateInternal
            {
                ParsedExamples = new DefaultPathTemplateExampleInternal[0]
            };
            IContext context = new DefaultContextInternal();
            var pathValues = new Dictionary<string, string>();
            DefaultPathTemplateFormatOptions formatOptions = null;
            Assert.Empty(instance.InterpolateAll(context, pathValues, formatOptions));
            Assert.Throws<PathTemplateInterpolationException>(() => instance.Interpolate(context, pathValues, formatOptions));
        }

        [Fact]
        public void TestInterpolate1a()
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
            var expected = new List<string>
            {
                "/"                
            };
            IContext context = null;
            Dictionary<string, string> pathValues = null;
            DefaultPathTemplateFormatOptions formatOptions = null;
            var actual = instance.InterpolateAll(context, pathValues, formatOptions);
            Assert.Equal(expected, actual);
            Assert.Equal(expected[0], instance.Interpolate(context, pathValues, formatOptions));
        }

        [Fact]
        public void TestInterpolate1b()
        {
            var instance = new DefaultPathTemplateInternal
            {
                ParsedExamples = new DefaultPathTemplateExampleInternal[]
                {
                    new DefaultPathTemplateExampleInternal
                    {
                        MatchLeadingSlash = false,
                        Tokens = new PathToken[0]
                    }
                }
            };
            var expected = new List<string>
            {
                ""
            };
            IContext context = null;
            var pathValues = new Dictionary<string, string>();
            DefaultPathTemplateFormatOptions formatOptions = null;
            var actual = instance.InterpolateAll(context, pathValues, formatOptions);
            Assert.Equal(expected, actual);
            Assert.Equal(expected[0], instance.Interpolate(context, pathValues, formatOptions));
        }

        [Fact]
        public void TestInterpolate1c()
        {
            var instance = new DefaultPathTemplateInternal
            {
                ParsedExamples = new DefaultPathTemplateExampleInternal[]
                {
                    new DefaultPathTemplateExampleInternal
                    {
                        MatchLeadingSlash = false,
                        MatchTrailingSlash = true,
                        Tokens = new PathToken[0]
                    }
                }
            };
            var expected = new List<string>
            {
                "/"
            };
            IContext context = null;
            var pathValues = new Dictionary<string, string>();
            DefaultPathTemplateFormatOptions formatOptions = null;
            var actual = instance.InterpolateAll(context, pathValues, formatOptions);
            Assert.Equal(expected, actual);
            Assert.Equal(expected[0], instance.Interpolate(context, pathValues, formatOptions));
        }

        [Fact]
        public void TestInterpolate1d()
        {
            var instance = new DefaultPathTemplateInternal
            {
                ParsedExamples = new DefaultPathTemplateExampleInternal[]
                {
                    new DefaultPathTemplateExampleInternal
                    {
                        MatchTrailingSlash = true,
                        MatchLeadingSlash = true,
                        Tokens = new PathToken[0]
                    }
                }
            };
            var expected = new List<string>
            {
                "/"
            };
            IContext context = null;
            var pathValues = new Dictionary<string, string>();
            DefaultPathTemplateFormatOptions formatOptions = null;
            var actual = instance.InterpolateAll(context, pathValues, formatOptions);
            Assert.Equal(expected, actual);
            Assert.Equal(expected[0], instance.Interpolate(context, pathValues, formatOptions));
        }

        [Fact]
        public void TestInterpolate2a()
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
            var expected = new List<string>
            {
                "/"
            };
            IContext context = null;
            var pathValues = new Dictionary<string, string>();
            DefaultPathTemplateFormatOptions formatOptions = new DefaultPathTemplateFormatOptions();
            var actual = instance.InterpolateAll(context, pathValues, formatOptions);
            Assert.Equal(expected, actual);
            Assert.Equal(expected[0], instance.Interpolate(context, pathValues, formatOptions));
        }

        [Fact]
        public void TestInterpolate2b()
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
            var expected = new List<string>
            {
                ""
            };
            IContext context = null;
            var pathValues = new Dictionary<string, string>();
            DefaultPathTemplateFormatOptions formatOptions = new DefaultPathTemplateFormatOptions();
            var actual = instance.InterpolateAll(context, pathValues, formatOptions);
            Assert.Equal(expected, actual);
            Assert.Equal(expected[0], instance.Interpolate(context, pathValues, formatOptions));
        }

        [Fact]
        public void TestInterpolate2c()
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
            var expected = new List<string>
            {
                ""
            };
            IContext context = null;
            var pathValues = new Dictionary<string, string>();
            DefaultPathTemplateFormatOptions formatOptions = new DefaultPathTemplateFormatOptions
            {
                ApplyLeadingSlash = false
            };
            var actual = instance.InterpolateAll(context, pathValues, formatOptions);
            Assert.Equal(expected, actual);
            Assert.Equal(expected[0], instance.Interpolate(context, pathValues, formatOptions));
        }

        [Fact]
        public void TestInterpolate2d()
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
            var expected = new List<string>
            {
                "/"
            };
            IContext context = null;
            var pathValues = new Dictionary<string, string>();
            DefaultPathTemplateFormatOptions formatOptions = new DefaultPathTemplateFormatOptions
            {
                ApplyTrailingSlash = true
            };
            var actual = instance.InterpolateAll(context, pathValues, formatOptions);
            Assert.Equal(expected, actual);
            Assert.Equal(expected[0], instance.Interpolate(context, pathValues, formatOptions));
        }

        [Fact]
        public void TestInterpolate3a()
        {
            var instance = new DefaultPathTemplateInternal
            {
                ParsedExamples = new DefaultPathTemplateExampleInternal[]
                {
                    new DefaultPathTemplateExampleInternal
                    {
                        MatchLeadingSlash = true,
                        MatchTrailingSlash = false,
                        Tokens = new PathToken[]
                        {
                            new PathToken
                            {
                                Type = PathToken.TokenTypeLiteral,
                                Value = "bread"
                            }
                        }
                    }
                }
            };
            var expected = new List<string>
            {
                "/bread"
            };
            IContext context = null;
            var pathValues = new Dictionary<string, string>();
            DefaultPathTemplateFormatOptions formatOptions = new DefaultPathTemplateFormatOptions();
            var actual = instance.InterpolateAll(context, pathValues, formatOptions);
            Assert.Equal(expected, actual);
            Assert.Equal(expected[0], instance.Interpolate(context, pathValues, formatOptions));
        }

        [Fact]
        public void TestInterpolate3b()
        {
            var instance = new DefaultPathTemplateInternal
            {
                ParsedExamples = new DefaultPathTemplateExampleInternal[]
                {
                    new DefaultPathTemplateExampleInternal
                    {
                        MatchLeadingSlash = true,
                        MatchTrailingSlash = false,
                        Tokens = new PathToken[]
                        {
                            new PathToken
                            {
                                Type = PathToken.TokenTypeLiteral,
                                Value = "bread"
                            }
                        }
                    },
                    new DefaultPathTemplateExampleInternal
                    {
                        MatchLeadingSlash = false,
                        MatchTrailingSlash = true,
                        Tokens = new PathToken[]
                        {
                            new PathToken
                            {
                                Type = PathToken.TokenTypeLiteral,
                                Value = "bread"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeLiteral,
                                Value = "and"
                            }
                        }
                    },
                    new DefaultPathTemplateExampleInternal
                    {
                        MatchLeadingSlash = true,
                        MatchTrailingSlash = true,
                        Tokens = new PathToken[]
                        {
                            new PathToken
                            {
                                Type = PathToken.TokenTypeLiteral,
                                Value = "bread"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeLiteral,
                                Value = "and"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeLiteral,
                                Value = "tea"
                            }
                        }
                    },
                    new DefaultPathTemplateExampleInternal
                    {
                        MatchLeadingSlash = false,
                        MatchTrailingSlash = false,
                        Tokens = new PathToken[]
                        {
                            new PathToken
                            {
                                Type = PathToken.TokenTypeLiteral,
                                Value = "just"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeLiteral,
                                Value = "bread"
                            }
                        }
                    },
                }
            };
            var expected = new List<string>
            {
                "/bread", "bread/and/",
                "/bread/and/tea/", "just/bread"
            };
            IContext context = null;
            var pathValues = new Dictionary<string, string>();
            DefaultPathTemplateFormatOptions formatOptions = new DefaultPathTemplateFormatOptions();
            var actual = instance.InterpolateAll(context, pathValues, formatOptions);
            Assert.Equal(expected, actual);
            Assert.Equal(expected[0], instance.Interpolate(context, pathValues, formatOptions));
        }

        [Fact]
        public void TestInterpolate3c()
        {
            var instance = new DefaultPathTemplateInternal
            {
                ParsedExamples = new DefaultPathTemplateExampleInternal[]
                {
                    new DefaultPathTemplateExampleInternal
                    {
                        MatchLeadingSlash = false,
                        MatchTrailingSlash = true,
                        Tokens = new PathToken[]
                        {
                            new PathToken
                            {
                                Type = PathToken.TokenTypeLiteral,
                                Value = "bread"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeLiteral,
                                Value = "and"
                            }
                        }
                    },
                    new DefaultPathTemplateExampleInternal
                    {
                        MatchLeadingSlash = true,
                        MatchTrailingSlash = true,
                        Tokens = new PathToken[]
                        {
                            new PathToken
                            {
                                Type = PathToken.TokenTypeLiteral,
                                Value = "bread"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeLiteral,
                                Value = "and"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeLiteral,
                                Value = "tea"
                            }
                        }
                    },
                    new DefaultPathTemplateExampleInternal
                    {
                        MatchLeadingSlash = false,
                        MatchTrailingSlash = false,
                        Tokens = new PathToken[]
                        {
                            new PathToken
                            {
                                Type = PathToken.TokenTypeLiteral,
                                Value = "just"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeLiteral,
                                Value = "bread"
                            }
                        }
                    },
                    new DefaultPathTemplateExampleInternal
                    {
                        MatchLeadingSlash = true,
                        MatchTrailingSlash = false,
                        Tokens = new PathToken[]
                        {
                            new PathToken
                            {
                                Type = PathToken.TokenTypeLiteral,
                                Value = "bread"
                            }
                        }
                    },
                }
            };
            var expected = new List<string>
            {
                "/bread/and/", "/bread/and/tea/", 
                "/just/bread/", "/bread/",
            };
            IContext context = null;
            var pathValues = new Dictionary<string, string>();
            DefaultPathTemplateFormatOptions formatOptions = new DefaultPathTemplateFormatOptions
            {
                ApplyLeadingSlash = true,
                ApplyTrailingSlash = true
            };
            var actual = instance.InterpolateAll(context, pathValues, formatOptions);
            Assert.Equal(expected, actual);
            Assert.Equal(expected[3], instance.Interpolate(context, pathValues, formatOptions));
        }

        [Fact]
        public void TestInterpolate3d()
        {
            var instance = new DefaultPathTemplateInternal
            {
                ParsedExamples = new DefaultPathTemplateExampleInternal[]
                {
                    new DefaultPathTemplateExampleInternal
                    {
                        MatchLeadingSlash = true,
                        MatchTrailingSlash = true,
                        Tokens = new PathToken[]
                        {
                            new PathToken
                            {
                                Type = PathToken.TokenTypeLiteral,
                                Value = "bread"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeLiteral,
                                Value = "and"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeLiteral,
                                Value = "tea"
                            }
                        }
                    },
                    new DefaultPathTemplateExampleInternal
                    {
                        MatchLeadingSlash = false,
                        MatchTrailingSlash = false,
                        Tokens = new PathToken[]
                        {
                            new PathToken
                            {
                                Type = PathToken.TokenTypeLiteral,
                                Value = "just"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeLiteral,
                                Value = "bread"
                            }
                        }
                    },
                    new DefaultPathTemplateExampleInternal
                    {
                        MatchLeadingSlash = false,
                        MatchTrailingSlash = true,
                        Tokens = new PathToken[]
                        {
                            new PathToken
                            {
                                Type = PathToken.TokenTypeLiteral,
                                Value = "bread"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeLiteral,
                                Value = "and"
                            }
                        }
                    },
                    new DefaultPathTemplateExampleInternal
                    {
                        MatchLeadingSlash = true,
                        MatchTrailingSlash = false,
                        Tokens = new PathToken[]
                        {
                            new PathToken
                            {
                                Type = PathToken.TokenTypeLiteral,
                                Value = "bread?name=value#key%"
                            }
                        }
                    }
                }
            };
            var expected = new List<string>
            {
                "bread/and/tea", "just/bread", "bread/and",
                "bread?name=value#key%",
            };
            IContext context = null;
            var pathValues = new Dictionary<string, string>();
            DefaultPathTemplateFormatOptions formatOptions = new DefaultPathTemplateFormatOptions
            {
                ApplyLeadingSlash = false,
                ApplyTrailingSlash = false
            };
            var actual = instance.InterpolateAll(context, pathValues, formatOptions);
            Assert.Equal(expected, actual);
            Assert.Equal(expected[2], instance.Interpolate(context, pathValues, formatOptions));
        }

        [Fact]
        public void TestInterpolate4a()
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
                                Value = "*"
                            }
                        }
                    }
                }
            };
            var expected = new List<string>
            {
                "/"
            };
            IContext context = null;
            var pathValues = new Dictionary<string, string>
            {
                { "*", null }
            };
            DefaultPathTemplateFormatOptions formatOptions = null;
            var actual = instance.InterpolateAll(context, pathValues, formatOptions);
            Assert.Equal(expected, actual);
            Assert.Equal(expected[0], instance.Interpolate(context, pathValues, formatOptions));
        }

        [Fact]
        public void TestInterpolate4b()
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
                                Value = "*"
                            }
                        }
                    }
                }
            };
            var expected = new List<string>
            {
                "/drink"
            };
            IContext context = null;
            var pathValues = new Dictionary<string, string>
            {
                { "*", "/drink" }
            };
            DefaultPathTemplateFormatOptions formatOptions = null;
            var actual = instance.InterpolateAll(context, pathValues, formatOptions);
            Assert.Equal(expected, actual);
            Assert.Equal(expected[0], instance.Interpolate(context, pathValues, formatOptions));
        }

        [Fact]
        public void TestInterpolate4c()
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
                                Type = PathToken.TokenTypeWildCard,
                                Value = "w"
                            }
                        }
                    }
                }
            };
            var expected = new List<string>
            {
                ""
            };
            IContext context = null;
            var pathValues = new Dictionary<string, string>
            {
                { "w", null }
            };
            DefaultPathTemplateFormatOptions formatOptions = null;
            var actual = instance.InterpolateAll(context, pathValues, formatOptions);
            Assert.Equal(expected, actual);
            Assert.Equal(expected[0], instance.Interpolate(context, pathValues, formatOptions));
        }

        [Fact]
        public void TestInterpolate4d()
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
                                Type = PathToken.TokenTypeWildCard,
                                Value = "w"
                            }
                        }
                    }
                }
            };
            var expected = new List<string>
            {
                "/drink"
            };
            IContext context = null;
            var pathValues = new Dictionary<string, string>
            {
                { "w", "/drink" }
            };
            DefaultPathTemplateFormatOptions formatOptions = null;
            var actual = instance.InterpolateAll(context, pathValues, formatOptions);
            Assert.Equal(expected, actual);
            Assert.Equal(expected[0], instance.Interpolate(context, pathValues, formatOptions));
        }

        [Fact]
        public void TestInterpolate4e()
        {
            var instance = new DefaultPathTemplateInternal
            {
                ParsedExamples = new DefaultPathTemplateExampleInternal[]
                {
                    new DefaultPathTemplateExampleInternal
                    {
                        MatchLeadingSlash = false,
                        MatchTrailingSlash = true,
                        Tokens = new PathToken[]
                        {
                            new PathToken
                            {
                                Type = PathToken.TokenTypeLiteral,
                                Value = "singing"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "*"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeWildCard,
                                Value = "w"
                            }
                        }
                    }
                }
            };
            var expected = new List<string>
            {
                "singing/all%20the/time/throughout/the/day"
            };
            IContext context = null;
            var pathValues = new Dictionary<string, string>
            {
                { "*", "all the" },
                { "w", "time/throughout/the/day" }
            };
            DefaultPathTemplateFormatOptions formatOptions = null;
            var actual = instance.InterpolateAll(context, pathValues, formatOptions);
            Assert.Equal(expected, actual);
            Assert.Equal(expected[0], instance.Interpolate(context, pathValues, formatOptions));
        }

        [Fact]
        public void TestInterpolate4f()
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
                                Value = "singing"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "*"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeWildCard,
                                Value = "w"
                            }
                        },
                    }
                }
            };
            var expected = new List<string>
            {
                "singing/all%20day/"
            };
            IContext context = null;
            var pathValues = new Dictionary<string, string>
            {
                { "*", "all day" },
                { "w", null }
            };
            DefaultPathTemplateFormatOptions formatOptions = new DefaultPathTemplateFormatOptions
            {
                ApplyLeadingSlash = false,
                ApplyTrailingSlash = true
            };
            var actual = instance.InterpolateAll(context, pathValues, formatOptions);
            Assert.Equal(expected, actual);
            Assert.Equal(expected[0], instance.Interpolate(context, pathValues, formatOptions));
        }

        [Fact]
        public void TestInterpolate4g()
        {
            var instance = new DefaultPathTemplateInternal
            {
                ParsedExamples = new DefaultPathTemplateExampleInternal[]
                {
                    new DefaultPathTemplateExampleInternal
                    {
                        MatchTrailingSlash = true,
                        MatchLeadingSlash = true,
                        Tokens = new PathToken[]
                        {
                            new PathToken
                            {
                                Type = PathToken.TokenTypeWildCard,
                                Value = ""
                            }
                        }
                    }
                }
            };
            var expected = new List<string>
            {
                "/"
            };
            IContext context = null;
            var pathValues = new Dictionary<string, string>
            {
                { "", null }
            };
            DefaultPathTemplateFormatOptions formatOptions = null;
            var actual = instance.InterpolateAll(context, pathValues, formatOptions);
            Assert.Equal(expected, actual);
            Assert.Equal(expected[0], instance.Interpolate(context, pathValues, formatOptions));
        }

        [Fact]
        public void TestInterpolate4h()
        {
            var instance = new DefaultPathTemplateInternal
            {
                ParsedExamples = new DefaultPathTemplateExampleInternal[]
                {
                    new DefaultPathTemplateExampleInternal
                    {
                        MatchTrailingSlash = true,
                        MatchLeadingSlash = true,
                        Tokens = new PathToken[]
                        {
                            new PathToken
                            {
                                Type = PathToken.TokenTypeWildCard,
                                Value = ""
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeLiteral,
                                Value = ""
                            }
                        }
                    }
                }
            };
            var expected = new List<string>
            {
                "//"
            };
            IContext context = null;
            var pathValues = new Dictionary<string, string>
            {
                { "", null }
            };
            DefaultPathTemplateFormatOptions formatOptions = null;
            var actual = instance.InterpolateAll(context, pathValues, formatOptions);
            Assert.Equal(expected, actual);
            Assert.Equal(expected[0], instance.Interpolate(context, pathValues, formatOptions));
        }

        [Fact]
        public void TestInterpolate4i()
        {
            var instance = new DefaultPathTemplateInternal
            {
                ParsedExamples = new DefaultPathTemplateExampleInternal[]
                {
                    new DefaultPathTemplateExampleInternal
                    {
                        MatchTrailingSlash = true,
                        MatchLeadingSlash = true,
                        Tokens = new PathToken[]
                        {
                            new PathToken
                            {
                                Type = PathToken.TokenTypeWildCard,
                                Value = ""
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeLiteral,
                                Value = ""
                            }
                        }
                    }
                }
            };
            var expected = new List<string>
            {
                "/"
            };
            IContext context = null;
            var pathValues = new Dictionary<string, string>
            {
                { "", "" }
            };
            DefaultPathTemplateFormatOptions formatOptions = new DefaultPathTemplateFormatOptions
            {
                ApplyLeadingSlash = false,
                ApplyTrailingSlash = false
            };
            var actual = instance.InterpolateAll(context, pathValues, formatOptions);
            Assert.Equal(expected, actual);
            Assert.Equal(expected[0], instance.Interpolate(context, pathValues, formatOptions));
        }

        [Fact]
        public void TestInterpolate4j()
        {
            var instance = new DefaultPathTemplateInternal
            {
                ParsedExamples = new DefaultPathTemplateExampleInternal[]
                {
                    new DefaultPathTemplateExampleInternal
                    {
                        MatchTrailingSlash = true,
                        MatchLeadingSlash = true,
                        Tokens = new PathToken[]
                        {
                            new PathToken
                            {
                                Type = PathToken.TokenTypeWildCard,
                                Value = "*"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeLiteral,
                                Value = "bread"
                            }
                        }
                    }
                }
            };
            var expected = new List<string>
            {
                "/peace/and/justice/bread/"
            };
            IContext context = null;
            var pathValues = new Dictionary<string, string>
            {
                { "*", "/peace/and/justice/" }
            };
            DefaultPathTemplateFormatOptions formatOptions = null;
            var actual = instance.InterpolateAll(context, pathValues, formatOptions);
            Assert.Equal(expected, actual);
            Assert.Equal(expected[0], instance.Interpolate(context, pathValues, formatOptions));
        }

        [Fact]
        public void TestInterpolate4k()
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
                                Value = "singing"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "*"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeWildCard,
                                Value = "w"
                            }
                        },
                    }
                }
            };
            var expected = new List<string>();
            IContext context = null;
            Dictionary<string, string> pathValues = null; ;
            DefaultPathTemplateFormatOptions formatOptions = null;
            var actual = instance.InterpolateAll(context, pathValues, formatOptions);
            Assert.Equal(expected, actual);
            Assert.Throws<PathTemplateInterpolationException>(() => instance.Interpolate(context, pathValues, formatOptions));
        }

        [Fact]
        public void TestInterpolate4L()
        {
            var instance = new DefaultPathTemplateInternal
            {
                ParsedExamples = new DefaultPathTemplateExampleInternal[]
                {
                    new DefaultPathTemplateExampleInternal
                    {
                        MatchTrailingSlash = true,
                        MatchLeadingSlash = true,
                        Tokens = new PathToken[]
                        {
                            new PathToken
                            {
                                Type = PathToken.TokenTypeWildCard,
                                Value = ""
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeLiteral,
                                Value = ""
                            }
                        }
                    }
                }
            };
            var expected = new List<string>();
            IContext context = null;
            var pathValues = new Dictionary<string, string>();
            DefaultPathTemplateFormatOptions formatOptions = null;
            var actual = instance.InterpolateAll(context, pathValues, formatOptions);
            Assert.Equal(expected, actual);
            Assert.Throws<PathTemplateInterpolationException>(() => instance.Interpolate(context, pathValues, formatOptions));
        }

        [Fact]
        public void TestInterpolate4m()
        {
            var instance = new DefaultPathTemplateInternal
            {
                ParsedExamples = new DefaultPathTemplateExampleInternal[]
                {
                    new DefaultPathTemplateExampleInternal
                    {
                        MatchTrailingSlash = true,
                        MatchLeadingSlash = true,
                        Tokens = new PathToken[]
                        {
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "tea"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeLiteral,
                                Value = ""
                            }
                        }
                    }
                }
            };
            var expected = new List<string>();
            IContext context = null;
            var pathValues = new Dictionary<string, string>
            {
                { "tea", null }
            };
            DefaultPathTemplateFormatOptions formatOptions = null;
            var actual = instance.InterpolateAll(context, pathValues, formatOptions);
            Assert.Equal(expected, actual);
            Assert.Throws<PathTemplateInterpolationException>(() => instance.Interpolate(context, pathValues, formatOptions));
        }

        [Fact]
        public void TestInterpolate4n()
        {
            var instance = new DefaultPathTemplateInternal
            {
                ParsedExamples = new DefaultPathTemplateExampleInternal[]
                {
                    new DefaultPathTemplateExampleInternal
                    {
                        MatchTrailingSlash = true,
                        MatchLeadingSlash = true,
                        Tokens = new PathToken[]
                        {
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "tea"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeLiteral,
                                Value = ""
                            }
                        }
                    }
                }
            };
            var expected = new List<string>();
            IContext context = null;
            var pathValues = new Dictionary<string, string>
            {
                { "tea", "" }
            };
            DefaultPathTemplateFormatOptions formatOptions = null;
            var actual = instance.InterpolateAll(context, pathValues, formatOptions);
            Assert.Equal(expected, actual);
            Assert.Throws<PathTemplateInterpolationException>(() => instance.Interpolate(context, pathValues, formatOptions));
        }

        [Fact]
        public void TestInterpolate5a()
        {
            var instance = new DefaultPathTemplateInternal
            {
                ParsedExamples = new DefaultPathTemplateExampleInternal[]
                {
                    new DefaultPathTemplateExampleInternal
                    {
                        MatchTrailingSlash = true,
                        MatchLeadingSlash = true,
                        Tokens = new PathToken[]
                        {
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "tea",
                                EmptySegmentAllowed = true
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeLiteral,
                                Value = ""
                            }
                        }
                    }
                }
            };
            var expected = new List<string>
            {
                "///"
            };
            IContext context = null;
            var pathValues = new Dictionary<string, string>
            {
                { "tea", null }
            };
            DefaultPathTemplateFormatOptions formatOptions = null;
            var actual = instance.InterpolateAll(context, pathValues, formatOptions);
            Assert.Equal(expected, actual);
            Assert.Equal(expected[0], instance.Interpolate(context, pathValues, formatOptions));
        }

        [Fact]
        public void TestInterpolate5b()
        {
            var instance = new DefaultPathTemplateInternal
            {
                ParsedExamples = new DefaultPathTemplateExampleInternal[]
                {
                    new DefaultPathTemplateExampleInternal
                    {
                        MatchTrailingSlash = true,
                        MatchLeadingSlash = true,
                        Tokens = new PathToken[]
                        {
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "tea",
                                EmptySegmentAllowed = true
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeLiteral,
                                Value = ""
                            }
                        }
                    }
                }
            };
            var expected = new List<string>
            {
                "/"
            };
            IContext context = null;
            var pathValues = new Dictionary<string, string>
            {
                { "tea", "" }
            };
            DefaultPathTemplateFormatOptions formatOptions = new DefaultPathTemplateFormatOptions
            {
                ApplyLeadingSlash = false,
                ApplyTrailingSlash = false
            };
            var actual = instance.InterpolateAll(context, pathValues, formatOptions);
            Assert.Equal(expected, actual);
            Assert.Equal(expected[0], instance.Interpolate(context, pathValues, formatOptions));
        }

        [Fact]
        public void TestInterpolate5c()
        {
            var instance = new DefaultPathTemplateInternal
            {
                ParsedExamples = new DefaultPathTemplateExampleInternal[]
                {
                    new DefaultPathTemplateExampleInternal
                    {
                        Tokens = new PathToken[0]
                    },
                    new DefaultPathTemplateExampleInternal
                    {
                        Tokens = new PathToken[]
                        {
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "controller"
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
                                Value = "controller"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "action"
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
                                Value = "controller"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "action"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "id"
                            }
                        }
                    }
                },
                DefaultValues = new Dictionary<string, string>
                {
                    { "controller", "Home" },
                    { "action", "Index" }
                }
            };
            var expected = new List<string>();
            IContext context = new DefaultContextInternal();
            var pathValues = new Dictionary<string, string>
            {
                { "id", "2" }
            };
            DefaultPathTemplateFormatOptions formatOptions = null;
            var actual = instance.InterpolateAll(context, pathValues, formatOptions);
            Assert.Equal(expected, actual);
            Assert.Throws<PathTemplateInterpolationException>(() => instance.Interpolate(context, pathValues, formatOptions));
        }

        [Fact]
        public void TestInterpolate5d()
        {
            var instance = new DefaultPathTemplateInternal
            {
                ParsedExamples = new DefaultPathTemplateExampleInternal[]
                {
                    new DefaultPathTemplateExampleInternal
                    {
                        Tokens = new PathToken[0]
                    },
                    new DefaultPathTemplateExampleInternal
                    {
                        Tokens = new PathToken[]
                        {
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "controller"
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
                                Value = "controller"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "action"
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
                                Value = "controller"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "action"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "id"
                            }
                        }
                    }
                },
                DefaultValues = new Dictionary<string, string>
                {
                    { "controller", "Home" },
                    { "action", "Index" }
                }
            };
            var expected = new List<string>
            {
                "/"
            };
            IContext context = new DefaultContextInternal();
            var pathValues = new Dictionary<string, string>();
            DefaultPathTemplateFormatOptions formatOptions = null;
            var actual = instance.InterpolateAll(context, pathValues, formatOptions);
            Assert.Equal(expected, actual);
            Assert.Equal(expected[0], instance.Interpolate(context, pathValues, formatOptions));
        }

        [Fact]
        public void TestInterpolate5e()
        {
            var instance = new DefaultPathTemplateInternal
            {
                ParsedExamples = new DefaultPathTemplateExampleInternal[]
                {
                    new DefaultPathTemplateExampleInternal
                    {
                        Tokens = new PathToken[0]
                    },
                    new DefaultPathTemplateExampleInternal
                    {
                        Tokens = new PathToken[]
                        {
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "controller"
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
                                Value = "controller"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "action"
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
                                Value = "controller"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "action"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "id"
                            }
                        }
                    }
                },
                DefaultValues = new Dictionary<string, string>
                {
                    { "controller", "Home" },
                    { "action", "Index" }
                }
            };
            var expected = new List<string>
            {
                "/", "/Home"
            };
            IContext context = new DefaultContextInternal();
            var pathValues = new Dictionary<string, string>
            {
                { "controller", "Home" }
            };
            DefaultPathTemplateFormatOptions formatOptions = null;
            var actual = instance.InterpolateAll(context, pathValues, formatOptions);
            Assert.Equal(expected, actual);
            Assert.Equal(expected[0], instance.Interpolate(context, pathValues, formatOptions));
        }

        [Fact]
        public void TestInterpolate5f()
        {
            var instance = new DefaultPathTemplateInternal
            {
                ParsedExamples = new DefaultPathTemplateExampleInternal[]
                {
                    new DefaultPathTemplateExampleInternal
                    {
                        Tokens = new PathToken[0]
                    },
                    new DefaultPathTemplateExampleInternal
                    {
                        Tokens = new PathToken[]
                        {
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "controller"
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
                                Value = "controller"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "action"
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
                                Value = "controller"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "action"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "id"
                            }
                        }
                    }
                },
                DefaultValues = new Dictionary<string, string>
                {
                    { "controller", "Home" },
                    { "action", "Index" }
                }
            };
            var expected = new List<string>
            {
                "/", "/home", "/home/index"
            };
            IContext context = new DefaultContextInternal();
            var pathValues = new Dictionary<string, string>
            {
                { "controller", "home" }, { "action", "index" }
            };
            DefaultPathTemplateFormatOptions formatOptions = null;
            var actual = instance.InterpolateAll(context, pathValues, formatOptions);
            Assert.Equal(expected, actual);
            Assert.Equal(expected[0], instance.Interpolate(context, pathValues, formatOptions));
        }

        [Fact]
        public void TestInterpolate5g()
        {
            var instance = new DefaultPathTemplateInternal
            {
                ParsedExamples = new DefaultPathTemplateExampleInternal[]
                {
                    new DefaultPathTemplateExampleInternal
                    {
                        Tokens = new PathToken[0]
                    },
                    new DefaultPathTemplateExampleInternal
                    {
                        CaseSensitiveMatchEnabled = true,
                        Tokens = new PathToken[]
                        {
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "controller"
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
                                Value = "controller"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "action"
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
                                Value = "controller"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "action"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "id"
                            }
                        }
                    }
                },
                DefaultValues = new Dictionary<string, string>
                {
                    { "controller", "Home" },
                    { "action", "Index" }
                }
            };
            var expected = new List<string>
            {
                "/", "/home/index"
            };
            IContext context = new DefaultContextInternal();
            var pathValues = new Dictionary<string, string>
            {
                { "controller", "home" }, { "action", "index" }
            };
            DefaultPathTemplateFormatOptions formatOptions = null;
            var actual = instance.InterpolateAll(context, pathValues, formatOptions);
            Assert.Equal(expected, actual);
            Assert.Equal(expected[0], instance.Interpolate(context, pathValues, formatOptions));
        }

        [Fact]
        public void TestInterpolate5h()
        {
            var instance = new DefaultPathTemplateInternal
            {
                ParsedExamples = new DefaultPathTemplateExampleInternal[]
                {
                    new DefaultPathTemplateExampleInternal
                    {
                        Tokens = new PathToken[0]
                    },
                    new DefaultPathTemplateExampleInternal
                    {
                        Tokens = new PathToken[]
                        {
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "controller"
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
                                Value = "controller"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "action"
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
                                Value = "controller"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "action"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "id"
                            }
                        }
                    }
                },
                DefaultValues = new Dictionary<string, string>
                {
                    { "controller", "Home" },
                    { "action", "Index" }
                }
            };
            var expected = new List<string>
            {
                "/home/index/3"
            };
            IContext context = new DefaultContextInternal();
            var pathValues = new Dictionary<string, string>
            {
                { "controller", "home" }, { "action", "index" }, { "id", "3" }
            };
            DefaultPathTemplateFormatOptions formatOptions = null;
            var actual = instance.InterpolateAll(context, pathValues, formatOptions);
            Assert.Equal(expected, actual);
            Assert.Equal(expected[0], instance.Interpolate(context, pathValues, formatOptions));
        }

        [Fact]
        public void TestInterpolate5i()
        {
            var instance = new DefaultPathTemplateInternal
            {
                ParsedExamples = new DefaultPathTemplateExampleInternal[]
                {
                    new DefaultPathTemplateExampleInternal
                    {
                        CaseSensitiveMatchEnabled = true,
                        Tokens = new PathToken[0]
                    },
                    new DefaultPathTemplateExampleInternal
                    {
                        CaseSensitiveMatchEnabled = true,
                        Tokens = new PathToken[]
                        {
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "controller"
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
                                Value = "controller"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "action"
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
                                Value = "controller"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "action"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "id"
                            }
                        }
                    }
                },
                DefaultValues = new Dictionary<string, string>
                {
                    { "controller", "Home" },
                    { "action", "Index" }
                }
            };
            var expected = new List<string>
            {
                "/home/index"
            };
            IContext context = new DefaultContextInternal();
            var pathValues = new Dictionary<string, string>
            {
                { "controller", "home" }, { "action", "index" }
            };
            DefaultPathTemplateFormatOptions formatOptions = new DefaultPathTemplateFormatOptions
            {
                CaseSensitiveMatchEnabled = null
            };
            var actual = instance.InterpolateAll(context, pathValues, formatOptions);
            Assert.Equal(expected, actual);
            Assert.Equal(expected[0], instance.Interpolate(context, pathValues, formatOptions));
        }

        [Fact]
        public void TestInterpolate5k()
        {
            var instance = new DefaultPathTemplateInternal
            {
                ParsedExamples = new DefaultPathTemplateExampleInternal[]
                {
                    new DefaultPathTemplateExampleInternal
                    {
                        CaseSensitiveMatchEnabled = true,
                        Tokens = new PathToken[0]
                    },
                    new DefaultPathTemplateExampleInternal
                    {
                        CaseSensitiveMatchEnabled = true,
                        Tokens = new PathToken[]
                        {
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "controller"
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
                                Value = "controller"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "action"
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
                                Value = "controller"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "action"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "id"
                            }
                        }
                    }
                },
                DefaultValues = new Dictionary<string, string>
                {
                    { "controller", "Home" },
                    { "action", "Index" }
                }
            };
            var expected = new List<string>
            {
                "/", "/home", "/home/index"
            };
            IContext context = new DefaultContextInternal();
            var pathValues = new Dictionary<string, string>
            {
                { "controller", "home" }, { "action", "index" }
            };
            DefaultPathTemplateFormatOptions formatOptions = new DefaultPathTemplateFormatOptions
            {
                CaseSensitiveMatchEnabled = false
            };
            var actual = instance.InterpolateAll(context, pathValues, formatOptions);
            Assert.Equal(expected, actual);
            Assert.Equal(expected[0], instance.Interpolate(context, pathValues, formatOptions));
        }

        [Fact]
        public void TestInterpolate5L()
        {
            var instance = new DefaultPathTemplateInternal
            {
                ParsedExamples = new DefaultPathTemplateExampleInternal[]
                {
                    new DefaultPathTemplateExampleInternal
                    {
                        CaseSensitiveMatchEnabled = true,
                        Tokens = new PathToken[0]
                    },
                    new DefaultPathTemplateExampleInternal
                    {
                        CaseSensitiveMatchEnabled = true,
                        Tokens = new PathToken[]
                        {
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "controller"
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
                                Value = "controller"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "action"
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
                                Value = "controller"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "action"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "id"
                            }
                        }
                    }
                },
                DefaultValues = new Dictionary<string, string>
                {
                    { "controller", "Home" },
                    { "action", "Index" }
                }
            };
            var expected = new List<string>
            {
                "/", "/Home", "/Home/Index"
            };
            IContext context = new DefaultContextInternal();
            var pathValues = new Dictionary<string, string>
            {
                { "controller", "Home" }, { "action", "Index" }
            };
            DefaultPathTemplateFormatOptions formatOptions = new DefaultPathTemplateFormatOptions
            {
                CaseSensitiveMatchEnabled = true
            };
            var actual = instance.InterpolateAll(context, pathValues, formatOptions);
            Assert.Equal(expected, actual);
            Assert.Equal(expected[0], instance.Interpolate(context, pathValues, formatOptions));
        }

        [Fact]
        public void TestInterpolate5m()
        {
            var instance = new DefaultPathTemplateInternal
            {
                ParsedExamples = new DefaultPathTemplateExampleInternal[]
                {
                    new DefaultPathTemplateExampleInternal
                    {
                        Tokens = new PathToken[0]
                    },
                    new DefaultPathTemplateExampleInternal
                    {
                        Tokens = new PathToken[]
                        {
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "controller"
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
                                Value = "controller"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "action"
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
                                Value = "controller"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "action"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "id"
                            }
                        }
                    }
                },
                DefaultValues = new Dictionary<string, string>
                {
                    { "controller", "Home" },
                    { "action", "Index" }
                }
            };
            var expected = new List<string>
            {
                "/ceo%20account%2F", "/ceo%20account%2F/index"
            };
            IContext context = new DefaultContextInternal();
            var pathValues = new Dictionary<string, string>
            {
                { "controller", "ceo account/" }, { "action", "index" }
            };
            DefaultPathTemplateFormatOptions formatOptions = null;
            var actual = instance.InterpolateAll(context, pathValues, formatOptions);
            Assert.Equal(expected, actual);
            Assert.Equal(expected[0], instance.Interpolate(context, pathValues, formatOptions));
        }

        [Fact]
        public void TestInterpolate5n()
        {
            var instance = new DefaultPathTemplateInternal
            {
                ParsedExamples = new DefaultPathTemplateExampleInternal[]
                {
                    new DefaultPathTemplateExampleInternal
                    {
                        Tokens = new PathToken[0]
                    },
                    new DefaultPathTemplateExampleInternal
                    {
                        Tokens = new PathToken[]
                        {
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "controller"
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
                                Value = "controller"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "action"
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
                                Value = "controller"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "action"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "id"
                            }
                        }
                    }
                },
                DefaultValues = new Dictionary<string, string>
                {
                    { "controller", "Home" },
                    { "action", "Index" }
                }
            };
            var expected = new List<string>
            {
                "/ceo account//upd|ate/"
            };
            IContext context = new DefaultContextInternal();
            var pathValues = new Dictionary<string, string>
            {
                { "controller", "ceo account/" }, { "action", "upd|ate" }
            };
            DefaultPathTemplateFormatOptions formatOptions = new DefaultPathTemplateFormatOptions
            {
                ApplyTrailingSlash = true,
                EscapeNonWildCardSegments = false
            };
            var actual = instance.InterpolateAll(context, pathValues, formatOptions);
            Assert.Equal(expected, actual);
            Assert.Equal(expected[0], instance.Interpolate(context, pathValues, formatOptions));
        }

        [Fact]
        public void TestInterpolate6a()
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
                                Value = "controller"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "action"
                            }
                        }
                    },
                    new DefaultPathTemplateExampleInternal
                    {
                        MatchLeadingSlash = false,
                        UnescapeNonWildCardSegments = false,
                        CaseSensitiveMatchEnabled = true,
                        Tokens = new PathToken[]
                        {
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "controller"
                            }
                        }
                    }
                },
                DefaultValues = new Dictionary<string, string>
                {
                    { "action", "upd|ate/" }
                }
            };
            var expected = new List<string>
            {
                "/%2FCEO%20account%2F/UPD%7Cate%2F"
            };
            IContext context = new DefaultContextInternal();
            var pathValues = new Dictionary<string, string>
            {
                { "controller", "/CEO account/" }, { "action", "UPD|ate/" }
            };
            DefaultPathTemplateFormatOptions formatOptions = null;
            var actual = instance.InterpolateAll(context, pathValues, formatOptions);
            Assert.Equal(expected, actual);
            Assert.Equal(expected[0], instance.Interpolate(context, pathValues, formatOptions));
        }

        [Fact]
        public void TestInterpolate6b()
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
                                Value = "controller"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "action"
                            }
                        }
                    },
                    new DefaultPathTemplateExampleInternal
                    {
                        MatchLeadingSlash = false,
                        UnescapeNonWildCardSegments = false,
                        CaseSensitiveMatchEnabled = false,
                        Tokens = new PathToken[]
                        {
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "controller"
                            }
                        }
                    }
                },
                DefaultValues = new Dictionary<string, string>
                {
                    { "action", "upd|ate/" }
                }
            };
            var expected = new List<string>
            {
                "/%2FCEO%20account%2F/UPD%7Cate%2F", "/CEO account/"
            };
            IContext context = new DefaultContextInternal();
            var pathValues = new Dictionary<string, string>
            {
                { "controller", "/CEO account/" }, { "action", "UPD|ate/" }
            };
            DefaultPathTemplateFormatOptions formatOptions = new DefaultPathTemplateFormatOptions();
            var actual = instance.InterpolateAll(context, pathValues, formatOptions);
            Assert.Equal(expected, actual);
            Assert.Equal(expected[1], instance.Interpolate(context, pathValues, formatOptions));
        }

        [Fact]
        public void TestInterpolate6c()
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
                                Value = "controller"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "action"
                            }
                        }
                    },
                    new DefaultPathTemplateExampleInternal
                    {
                        MatchLeadingSlash = false,
                        UnescapeNonWildCardSegments = false,
                        CaseSensitiveMatchEnabled = true,
                        Tokens = new PathToken[]
                        {
                            new PathToken
                            {
                                Type = PathToken.TokenTypeSegment,
                                Value = "controller"
                            }
                        }
                    }
                },
                DefaultValues = new Dictionary<string, string>
                {
                    { "action", "upd|ate/" }
                }
            };
            var expected = new List<string>
            {
                "/%2Fceo%20account%2F/upd%7Cate%2F", "/ceo account/"
            };
            IContext context = new DefaultContextInternal();
            var pathValues = new Dictionary<string, string>
            {
                { "controller", "/ceo account/" }, { "action", "upd|ate/" }
            };
            DefaultPathTemplateFormatOptions formatOptions = null;
            var actual = instance.InterpolateAll(context, pathValues, formatOptions);
            Assert.Equal(expected, actual);
            Assert.Equal(expected[1], instance.Interpolate(context, pathValues, formatOptions));
        }

        [Fact]
        public void TestInterpolate6d()
        {
            var instance = new DefaultPathTemplateInternal
            {
                ParsedExamples = new DefaultPathTemplateExampleInternal[]
                {
                    new DefaultPathTemplateExampleInternal
                    {
                        MatchLeadingSlash = false,
                        MatchTrailingSlash = false,
                        Tokens = new PathToken[]
                        {
                            new PathToken
                            {
                                Type = PathToken.TokenTypeWildCard,
                                Value = "w"
                            }
                        }
                    }
                }
            };
            var expected = new List<string>
            {
                "/drink/"
            };
            IContext context = null;
            var pathValues = new Dictionary<string, string>
            {
                { "w", "/drink/" }
            };
            DefaultPathTemplateFormatOptions formatOptions = null;
            var actual = instance.InterpolateAll(context, pathValues, formatOptions);
            Assert.Equal(expected, actual);
            Assert.Equal(expected[0], instance.Interpolate(context, pathValues, formatOptions));
        }

        [Fact]
        public void TestInterpolate6e()
        {
            var instance = new DefaultPathTemplateInternal
            {
                ParsedExamples = new DefaultPathTemplateExampleInternal[]
                {
                    new DefaultPathTemplateExampleInternal
                    {
                        MatchLeadingSlash = false,
                        MatchTrailingSlash = false,
                        Tokens = new PathToken[]
                        {
                            new PathToken
                            {
                                Type = PathToken.TokenTypeWildCard,
                                Value = "w"
                            }
                        }
                    }
                }
            };
            var expected = new List<string>
            {
                "drink"
            };
            IContext context = null;
            var pathValues = new Dictionary<string, string>
            {
                { "w", "drink" }
            };
            DefaultPathTemplateFormatOptions formatOptions = null;
            var actual = instance.InterpolateAll(context, pathValues, formatOptions);
            Assert.Equal(expected, actual);
            Assert.Equal(expected[0], instance.Interpolate(context, pathValues, formatOptions));
        }

        [Fact]
        public void TestInterpolate6f()
        {
            var instance = new DefaultPathTemplateInternal
            {
                ParsedExamples = new DefaultPathTemplateExampleInternal[]
                {
                    new DefaultPathTemplateExampleInternal
                    {
                        MatchLeadingSlash = true,
                        MatchTrailingSlash = true,
                        Tokens = new PathToken[]
                        {
                            new PathToken
                            {
                                Type = PathToken.TokenTypeWildCard,
                                Value = "w"
                            }
                        }
                    }
                }
            };
            var expected = new List<string>
            {
                "/drink/"
            };
            IContext context = null;
            var pathValues = new Dictionary<string, string>
            {
                { "w", "/drink/" }
            };
            DefaultPathTemplateFormatOptions formatOptions = null;
            var actual = instance.InterpolateAll(context, pathValues, formatOptions);
            Assert.Equal(expected, actual);
            Assert.Equal(expected[0], instance.Interpolate(context, pathValues, formatOptions));
        }

        [Fact]
        public void TestInterpolate6g()
        {
            var instance = new DefaultPathTemplateInternal
            {
                ParsedExamples = new DefaultPathTemplateExampleInternal[]
                {
                    new DefaultPathTemplateExampleInternal
                    {
                        MatchLeadingSlash = true,
                        MatchTrailingSlash = true,
                        Tokens = new PathToken[]
                        {
                            new PathToken
                            {
                                Type = PathToken.TokenTypeWildCard,
                                Value = "w"
                            }
                        }
                    }
                }
            };
            var expected = new List<string>
            {
                "drink"
            };
            IContext context = null;
            var pathValues = new Dictionary<string, string>
            {
                { "w", "drink" }
            };
            DefaultPathTemplateFormatOptions formatOptions = null;
            var actual = instance.InterpolateAll(context, pathValues, formatOptions);
            Assert.Equal(expected, actual);
            Assert.Equal(expected[0], instance.Interpolate(context, pathValues, formatOptions));
        }

        [Fact]
        public void TestInterpolate6h()
        {
            var instance = new DefaultPathTemplateInternal
            {
                ParsedExamples = new DefaultPathTemplateExampleInternal[]
                {
                    new DefaultPathTemplateExampleInternal
                    {
                        MatchTrailingSlash = false,
                        MatchLeadingSlash = false,
                        Tokens = new PathToken[]
                        {
                            new PathToken
                            {
                                Type = PathToken.TokenTypeWildCard,
                                Value = "w"
                            }
                        }
                    }
                }
            };
            var expected = new List<string>
            {
                "//drink//"
            };
            IContext context = null;
            var pathValues = new Dictionary<string, string>
            {
                { "w", "/drink/" }
            };
            DefaultPathTemplateFormatOptions formatOptions = new DefaultPathTemplateFormatOptions
            {
                ApplyLeadingSlash = true,
                ApplyTrailingSlash = true
            };
            var actual = instance.InterpolateAll(context, pathValues, formatOptions);
            Assert.Equal(expected, actual);
            Assert.Equal(expected[0], instance.Interpolate(context, pathValues, formatOptions));
        }

        [Fact]
        public void TestInterpolate6i()
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
                                Value = "p"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeWildCard,
                                Value = "w"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeLiteral,
                                Value = "p"
                            },
                        }
                    }
                }
            };
            var expected = new List<string>
            {
                "/p/drink/p/"
            };
            IContext context = null;
            var pathValues = new Dictionary<string, string>
            {
                { "w", "/drink/" }
            };
            DefaultPathTemplateFormatOptions formatOptions = new DefaultPathTemplateFormatOptions
            {
                ApplyLeadingSlash = true,
                ApplyTrailingSlash = true
            };
            var actual = instance.InterpolateAll(context, pathValues, formatOptions);
            Assert.Equal(expected, actual);
            Assert.Equal(expected[0], instance.Interpolate(context, pathValues, formatOptions));
        }

        [Fact]
        public void TestInterpolate6j()
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
                                Value = "p"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeWildCard,
                                Value = "w"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeLiteral,
                                Value = "p"
                            },
                        }
                    }
                }
            };
            var expected = new List<string>
            {
                "p/drink/p"
            };
            IContext context = null;
            var pathValues = new Dictionary<string, string>
            {
                { "w", "/drink/" }
            };
            DefaultPathTemplateFormatOptions formatOptions = new DefaultPathTemplateFormatOptions
            {
                ApplyLeadingSlash = false,
                ApplyTrailingSlash = false
            };
            var actual = instance.InterpolateAll(context, pathValues, formatOptions);
            Assert.Equal(expected, actual);
            Assert.Equal(expected[0], instance.Interpolate(context, pathValues, formatOptions));
        }

        [Fact]
        public void TestInterpolate6k()
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
                                Value = "p"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeWildCard,
                                Value = "w"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeLiteral,
                                Value = "p"
                            },
                        }
                    }
                }
            };
            var expected = new List<string>
            {
                "p/drink/p/"
            };
            IContext context = null;
            var pathValues = new Dictionary<string, string>
            {
                { "w", "drink" }
            };
            DefaultPathTemplateFormatOptions formatOptions = new DefaultPathTemplateFormatOptions
            {
                ApplyLeadingSlash = false,
                ApplyTrailingSlash = true
            };
            var actual = instance.InterpolateAll(context, pathValues, formatOptions);
            Assert.Equal(expected, actual);
            Assert.Equal(expected[0], instance.Interpolate(context, pathValues, formatOptions));
        }

        [Fact]
        public void TestInterpolate6L()
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
                                Value = "p"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeWildCard,
                                Value = "w"
                            },
                            new PathToken
                            {
                                Type = PathToken.TokenTypeLiteral,
                                Value = "p"
                            },
                        }
                    }
                }
            };
            var expected = new List<string>
            {
                "/p/drink/p"
            };
            IContext context = null;
            var pathValues = new Dictionary<string, string>
            {
                { "w", "/drink/" }
            };
            DefaultPathTemplateFormatOptions formatOptions = new DefaultPathTemplateFormatOptions
            {
                ApplyLeadingSlash = true,
                ApplyTrailingSlash = false
            };
            var actual = instance.InterpolateAll(context, pathValues, formatOptions);
            Assert.Equal(expected, actual);
            Assert.Equal(expected[0], instance.Interpolate(context, pathValues, formatOptions));
        }
    }
}
