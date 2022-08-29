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
            var pathValues = new Dictionary<string, string>();
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
    }
}
