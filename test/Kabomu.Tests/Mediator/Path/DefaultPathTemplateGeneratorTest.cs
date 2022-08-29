﻿using Kabomu.Common;
using Kabomu.Mediator.Path;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Xunit.Abstractions;
using static Kabomu.Mediator.Path.DefaultPathTemplateExampleInternal;

namespace Kabomu.Tests.Mediator.Path
{
    public class DefaultPathTemplateGeneratorTest
    {
        private readonly ITestOutputHelper _outputHelper;

        public DefaultPathTemplateGeneratorTest(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        [Theory]
        [MemberData(nameof(CreateTestParseForErrorData))]
        public void TestParseForError(string part1, object part2,
            IDictionary<string, IPathConstraint> constraintFunctions,
            string expectedError, int errorRowNum, int errorColNum)
        {
            var instance = new DefaultPathTemplateGenerator
            {
                ConstraintFunctions = constraintFunctions
            };

            var actualError = Assert.Throws<ArgumentException>(() => instance.Parse(part1, part2));

            Assert.Contains($"row {errorRowNum}", actualError.Message);
            Assert.Contains($"column {errorColNum}", actualError.Message);
            Assert.Contains(expectedError, actualError.Message);
        }

        public static List<object[]> CreateTestParseForErrorData()
        {
            var testData = new List<object[]>();

            string part1 = "";
            object part2 = null;
            IDictionary<string, IPathConstraint> constraintFunctions = null;
            string expectedError = "no examples";
            int errorRowNum = 0;
            int errorColNum = 0;

            testData.Add(new object[] { part1, part2, constraintFunctions,
                expectedError, errorRowNum, errorColNum });

            part1 = "/\n" +
                " check :k,e";
            part2 = null;
            constraintFunctions = null;
            expectedError = "not found";
            errorRowNum = 2;
            errorColNum = 2;

            testData.Add(new object[] { part1, part2, constraintFunctions,
                expectedError, errorRowNum, errorColNum });

            part1 = "/\n" +
                " check :k,e";
            part2 = null;
            constraintFunctions = new Dictionary<string, IPathConstraint>
            {
                { "e", null }
            };
            expectedError = "null constraint function found";
            errorRowNum = 2;
            errorColNum = 2;

            testData.Add(new object[] { part1, part2, constraintFunctions,
                expectedError, errorRowNum, errorColNum });

            part1 = "/\n" +
                " check :k\n" +
                "\n" +
                ",,e";
            part2 = null;
            constraintFunctions = new Dictionary<string, IPathConstraint>
            {
                { "e", new ConfigurablePathConstraint() }
            };
            expectedError = "empty key";
            errorRowNum = 4;
            errorColNum = 1;

            testData.Add(new object[] { part1, part2, constraintFunctions,
                expectedError, errorRowNum, errorColNum });

            part1 = "save/";
            part2 = null;
            constraintFunctions = null;
            expectedError = "missing leading slash";
            errorRowNum = 1;
            errorColNum = 1;

            testData.Add(new object[] { part1, part2, constraintFunctions,
                expectedError, errorRowNum, errorColNum });

            part1 = "save";
            part2 = null;
            constraintFunctions = null;
            expectedError = "unknown key";
            errorRowNum = 1;
            errorColNum = 1;

            testData.Add(new object[] { part1, part2, constraintFunctions,
                expectedError, errorRowNum, errorColNum });

            part1 = "//";
            part2 = null;
            constraintFunctions = null;
            expectedError = "invalid";
            errorRowNum = 1;
            errorColNum = 1;

            testData.Add(new object[] { part1, part2, constraintFunctions,
                expectedError, errorRowNum, errorColNum });

            part1 = "////";
            part2 = null;
            constraintFunctions = null;
            expectedError = "invalid";
            errorRowNum = 1;
            errorColNum = 1;

            testData.Add(new object[] { part1, part2, constraintFunctions,
                expectedError, errorRowNum, errorColNum });

            part1 = "//two// two ";
            part2 = null;
            constraintFunctions = null;
            expectedError = "duplicate";
            errorRowNum = 1;
            errorColNum = 1;

            testData.Add(new object[] { part1, part2, constraintFunctions,
                expectedError, errorRowNum, errorColNum });

            part1 = "///one/// two ";
            part2 = null;
            constraintFunctions = null;
            expectedError = "duplicate";
            errorRowNum = 1;
            errorColNum = 1;

            testData.Add(new object[] { part1, part2, constraintFunctions,
                expectedError, errorRowNum, errorColNum });

            part1 = "///one// one ";
            part2 = null;
            constraintFunctions = null;
            expectedError = "duplicate";
            errorRowNum = 1;
            errorColNum = 1;

            testData.Add(new object[] { part1, part2, constraintFunctions,
                expectedError, errorRowNum, errorColNum });

            // test correct reporting of character positions during errors with path spec parsing.
            part1 = "name:e.g.,  ///";
            part2 = null;
            constraintFunctions = null;
            expectedError = "3";
            errorRowNum = 1;
            errorColNum = 2;

            testData.Add(new object[] { part1, part2, constraintFunctions,
                expectedError, errorRowNum, errorColNum });

            // blank input spec
            part1 = "name:e.g.,";
            part2 = null;
            constraintFunctions = null;
            expectedError = "blank string spec";
            errorRowNum = 1;
            errorColNum = 2;

            testData.Add(new object[] { part1, part2, constraintFunctions,
                expectedError, errorRowNum, errorColNum });

            // blank input spec
            part1 = "/,\"\"";
            part2 = null;
            constraintFunctions = null;
            expectedError = "blank string spec";
            errorRowNum = 1;
            errorColNum = 2;

            testData.Add(new object[] { part1, part2, constraintFunctions,
                expectedError, errorRowNum, errorColNum });

            return testData;
        }

        [Fact]
        public void TestParseForOtherErrors()
        {
            var instance = new DefaultPathTemplateGenerator();

            Assert.Throws<ArgumentNullException>(() =>
                instance.Parse(null, null));

            Assert.ThrowsAny<Exception>(() =>
                instance.Parse("/", "wrong option type"));
        }

        [Theory]
        [MemberData(nameof(CreateTestParseData))]
        public void TestParse(string part1, object part2,
            IDictionary<string, IPathConstraint> constraintFunctions,
            IPathTemplate expected)
        {
            var instance = new DefaultPathTemplateGenerator
            {
                ConstraintFunctions = constraintFunctions
            };

            var actual = instance.Parse(part1, part2);
            ComparisonUtils.AssertTemplatesEqual(expected, actual, _outputHelper);
        }

        public static List<object[]> CreateTestParseData()
        {
            var testData = new List<object[]>();

            string part1 = "/";
            object part2 = null;
            IDictionary<string, IPathConstraint> constraintFunctions = null;
            DefaultPathTemplateInternal expected = new DefaultPathTemplateInternal
            {
                ParsedExamples = new List<DefaultPathTemplateExampleInternal>
                {
                   new DefaultPathTemplateExampleInternal
                   {
                       Tokens = new List<PathToken>()
                   }
                }
            };

            testData.Add(new object[] { part1, part2, constraintFunctions, expected });

            part1 = "/  ";
            part2 = new DefaultPathTemplateMatchOptions
            {
                MatchTrailingSlash = true,
                UnescapeNonWildCardSegments = true
            };
            constraintFunctions = null;
            expected = new DefaultPathTemplateInternal
            {
                ParsedExamples = new List<DefaultPathTemplateExampleInternal>
                {
                   new DefaultPathTemplateExampleInternal
                   {
                       Tokens = new List<PathToken>
                       {
                           new PathToken
                           {
                               Type = PathToken.TokenTypeLiteral,
                               Value = "",
                               EmptySegmentAllowed = true
                           }
                       },
                       MatchTrailingSlash = true,
                       UnescapeNonWildCardSegments = true
                   }
                }
            };

            testData.Add(new object[] { part1, part2, constraintFunctions, expected });

            part1 = "/car";
            part2 = null;
            constraintFunctions = null;
            expected = new DefaultPathTemplateInternal
            {
                ParsedExamples = new List<DefaultPathTemplateExampleInternal>
                {
                   new DefaultPathTemplateExampleInternal
                   {
                       Tokens = new List<PathToken>
                       {
                           new PathToken
                           {
                               Type = PathToken.TokenTypeLiteral,
                               Value = "car"
                           }
                       }
                   }
                }
            };

            testData.Add(new object[] { part1, part2, constraintFunctions, expected });

            // test correct path spec parsing with surrounding whitespace,
            // and null match options.
            part1 = "name:default, /car ";
            part2 = new Dictionary<string, DefaultPathTemplateMatchOptions>
            {
                { "default", null }
            };
            constraintFunctions = null;
            expected = new DefaultPathTemplateInternal
            {
                ParsedExamples = new List<DefaultPathTemplateExampleInternal>
                {
                   new DefaultPathTemplateExampleInternal
                   {
                       Tokens = new List<PathToken>
                       {
                           new PathToken
                           {
                               Type = PathToken.TokenTypeLiteral,
                               Value = "car",
                               EmptySegmentAllowed = true
                           }
                       }
                   }
                }
            };

            testData.Add(new object[] { part1, part2, constraintFunctions, expected });

            part1 = "/c%61r //vehicle";
            part2 = null;
            constraintFunctions = null;
            expected = new DefaultPathTemplateInternal
            {
                ParsedExamples = new List<DefaultPathTemplateExampleInternal>
                {
                   new DefaultPathTemplateExampleInternal
                   {
                       Tokens = new List<PathToken>
                       {
                           new PathToken
                           {
                               Type = PathToken.TokenTypeLiteral,
                               Value = "car",
                               EmptySegmentAllowed = true
                           },
                           new PathToken
                           {
                               Type = PathToken.TokenTypeSegment,
                               Value = "vehicle",
                               EmptySegmentAllowed = false
                           }
                       }
                   }
                }
            };

            testData.Add(new object[] { part1, part2, constraintFunctions, expected });

            part1 = "/car //vehicle /// ,  ///yr//second// first/sei/du\n" +
                "defaults:,country,gh,capital,accra\n" +
                ",yr";
            part2 = null;
            constraintFunctions = null;
            expected = new DefaultPathTemplateInternal
            {
                ParsedExamples = new List<DefaultPathTemplateExampleInternal>
                {
                   new DefaultPathTemplateExampleInternal
                   {
                       Tokens = new List<PathToken>
                       {
                           new PathToken
                           {
                               Type = PathToken.TokenTypeLiteral,
                               Value = "car",
                               EmptySegmentAllowed = true
                           },
                           new PathToken
                           {
                               Type = PathToken.TokenTypeSegment,
                               Value = "vehicle",
                               EmptySegmentAllowed = true
                           },
                           new PathToken
                           {
                               Type = PathToken.TokenTypeWildCard,
                               Value = "",
                               EmptySegmentAllowed = true
                           }
                       }
                   },
                   new DefaultPathTemplateExampleInternal
                   {
                       Tokens = new List<PathToken>
                       {
                           new PathToken
                           {
                               Type = PathToken.TokenTypeWildCard,
                               Value = "yr"
                           },
                           new PathToken
                           {
                               Type = PathToken.TokenTypeSegment,
                               Value = "second"
                           },
                           new PathToken
                           {
                               Type = PathToken.TokenTypeSegment,
                               Value = "first",
                               EmptySegmentAllowed = true
                           },
                           new PathToken
                           {
                               Type = PathToken.TokenTypeLiteral,
                               Value = "sei"
                           },
                           new PathToken
                           {
                               Type = PathToken.TokenTypeLiteral,
                               Value = "du"
                           }
                       }
                   }
                },
                DefaultValues = new Dictionary<string, string>
                {
                    { "country", "gh" }, { "capital", "accra" }, { "yr", null }
                }
            };

            testData.Add(new object[] { part1, part2, constraintFunctions, expected });

            part1 = " defaults :,controller,Home\n" +
                ",action,Index\n" +
                "\n" +
                "name:general,/\n" +
                ",//controller\n" +
                ",//controller//action\n" +
                " name :specific,//controller//action//id\n" +
                "\n" +
                " check :id,int";
            part2 = new Dictionary<string, DefaultPathTemplateMatchOptions>
            {
                {
                    "general",
                    new DefaultPathTemplateMatchOptions
                    {
                        CaseSensitiveMatchEnabled = true,
                        MatchLeadingSlash = true,
                        MatchTrailingSlash = false,
                        UnescapeNonWildCardSegments = false
                    }
                }
            };
            var tt = new ConfigurablePathConstraint();
            constraintFunctions = new Dictionary<string, IPathConstraint>
            {
                { "int", tt }
            };
            expected = new DefaultPathTemplateInternal
            {
                ParsedExamples = new List<DefaultPathTemplateExampleInternal>
                {
                   new DefaultPathTemplateExampleInternal
                   {
                       Tokens = new List<PathToken>(),
                       CaseSensitiveMatchEnabled = true,
                       MatchLeadingSlash = true,
                       MatchTrailingSlash = false,
                       UnescapeNonWildCardSegments = false
                   },
                   new DefaultPathTemplateExampleInternal
                   {
                       Tokens = new List<PathToken>
                       {
                           new PathToken
                           {
                               Type = PathToken.TokenTypeSegment,
                               Value = "controller",
                           }
                       },
                       CaseSensitiveMatchEnabled = true,
                       MatchLeadingSlash = true,
                       MatchTrailingSlash = false,
                       UnescapeNonWildCardSegments = false
                   },
                   new DefaultPathTemplateExampleInternal
                   {
                       Tokens = new List<PathToken>
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
                       },
                       CaseSensitiveMatchEnabled = true,
                       MatchLeadingSlash = true,
                       MatchTrailingSlash = false,
                       UnescapeNonWildCardSegments = false
                   },
                   new DefaultPathTemplateExampleInternal
                   {
                       Tokens = new List<PathToken>
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
                    { "controller", "Home" }, { "action", "Index" }
                },
                AllConstraints = new Dictionary<string, IList<(string, string[])>>
                {
                    { "id", new List<(string, string[])>{ ("int", new string[0]) } }
                },
                ConstraintFunctions = new Dictionary<string, IPathConstraint>
                {
                    { "int", tt }
                }
            };

            testData.Add(new object[] { part1, part2, constraintFunctions, expected });

            // test defaults and constraints with more additions.
            part1 = "/car\n" +
                "\n" +
                " check :action,f3,c\n" +
                " check :controller,f3,a,b\r\n" +
                ",f4,a,b,char\n" +
                "\n" +
                "defaults:,a,v,b,v,c\n" +
                "defaults:,a,3,b,4,c,5,d\n" +
                ",e";
            part2 = null;
            var f3Tpc = new ConfigurablePathConstraint();
            var f4Tpc = new ConfigurablePathConstraint();
            constraintFunctions = new Dictionary<string, IPathConstraint>
            {
                { "f1", new ConfigurablePathConstraint() },
                { "f2", new ConfigurablePathConstraint() },
                { "f3", f3Tpc }, { "f4", f4Tpc }
            };
            expected = new DefaultPathTemplateInternal
            {
                ParsedExamples = new List<DefaultPathTemplateExampleInternal>
                {
                   new DefaultPathTemplateExampleInternal
                   {
                       Tokens = new List<PathToken>
                       {
                           new PathToken
                           {
                               Type = PathToken.TokenTypeLiteral,
                               Value = "car"
                           }
                       }
                   }
                },
                ConstraintFunctions = new Dictionary<string, IPathConstraint>
                {
                    { "f3", f3Tpc }, { "f4", f4Tpc }
                },
                AllConstraints = new Dictionary<string, IList<(string, string[])>>
                {
                    {
                        "controller",
                        new List<(string, string[])>
                        {
                            ("f3", new string[]{ "a", "b" }),
                            ("f4", new string[]{ "a", "b", "char" })
                        }
                    },
                    {
                        "action",
                        new List<(string, string[])>
                        {
                            ("f3", new string[]{ "c" })
                        }
                    }
                },
                DefaultValues = new Dictionary<string, string>
                {
                    { "a", "3" }, { "b", "4" }, { "c", "5" },
                    { "d", null }, { "e", null }
                }
            };

            testData.Add(new object[] { part1, part2, constraintFunctions, expected });
            
            // test reversal of percent encoding.
            part1 = "//%41/%20%41%42%43%58%59%5a%61%62%63%78%79%7a%30%31%38%39-._~%21%24%26%27%28%29%2A%2B%2C%3B%3D%3A%40%20";
            part2 = new DefaultPathTemplateMatchOptions
            {
                CaseSensitiveMatchEnabled = false,
                MatchLeadingSlash = false,
                MatchTrailingSlash = false,
                UnescapeNonWildCardSegments = true
            };
            constraintFunctions = null;
            expected = new DefaultPathTemplateInternal
            {
                ParsedExamples = new List<DefaultPathTemplateExampleInternal>
                {
                   new DefaultPathTemplateExampleInternal
                   {
                       Tokens = new List<PathToken>
                       {
                           new PathToken
                           {
                               Type = PathToken.TokenTypeSegment,
                               Value = "%41"
                           },
                           new PathToken
                           {
                               Type = PathToken.TokenTypeLiteral,
                               Value = "%20ABCXYZabcxyz0189-._~!$&'()*+,;=:@%20"
                           }
                       },
                       CaseSensitiveMatchEnabled = false,
                       MatchLeadingSlash = false,
                       MatchTrailingSlash = false,
                       UnescapeNonWildCardSegments = true
                   }
                }
            };

            testData.Add(new object[] { part1, part2, constraintFunctions, expected });

            // test correct detection of duplication.
            part1 = "//two/two/two";
            part2 = null;
            constraintFunctions = null;
            expected = new DefaultPathTemplateInternal
            {
                ParsedExamples = new List<DefaultPathTemplateExampleInternal>
                {
                   new DefaultPathTemplateExampleInternal
                   {
                       Tokens = new List<PathToken>
                       {
                           new PathToken
                           {
                               Type = PathToken.TokenTypeSegment,
                               Value = "two"
                           },
                           new PathToken
                           {
                               Type = PathToken.TokenTypeLiteral,
                               Value = "two"
                           },
                           new PathToken
                           {
                               Type = PathToken.TokenTypeLiteral,
                               Value = "two"
                           }
                       }
                   }
                }
            };

            testData.Add(new object[] { part1, part2, constraintFunctions, expected });

            // test correct skipping of escapes.
            part1 = "///%41/%41/%41";
            part2 = new DefaultPathTemplateMatchOptions
            {
                UnescapeNonWildCardSegments = false
            };
            constraintFunctions = null;
            expected = new DefaultPathTemplateInternal
            {
                ParsedExamples = new List<DefaultPathTemplateExampleInternal>
                {
                   new DefaultPathTemplateExampleInternal
                   {
                       UnescapeNonWildCardSegments = false,
                       Tokens = new List<PathToken>
                       {
                           new PathToken
                           {
                               Type = PathToken.TokenTypeWildCard,
                               Value = "%41"
                           },
                           new PathToken
                           {
                               Type = PathToken.TokenTypeLiteral,
                               Value = "%41"
                           },
                           new PathToken
                           {
                               Type = PathToken.TokenTypeLiteral,
                               Value = "%41"
                           }
                       }
                   }
                }
            };

            testData.Add(new object[] { part1, part2, constraintFunctions, expected });

            // test case insensitive matching of keys, and non-alpha characters in keys.
            part1 = $"{CsvUtils.EscapeValue(" \n NAME \r\n :\"\n,rty")}, ///w//L\n" +
                "/\n" +
                $"{CsvUtils.EscapeValue(" \n CHECK \r\n:\"\n,lty")},test\n" +
                $"{CsvUtils.EscapeValue(" \n DEFAULTS \r\n:\"\n")}," +
                    $"{CsvUtils.EscapeValue(" \n key \r\n:\"\n")}," +
                    $"{CsvUtils.EscapeValue(" \n value \r\n:\"\n")}\n" +
                "\n";
            part2 = new Dictionary<string, DefaultPathTemplateMatchOptions>
            {
                {
                    "\"\n,rty",
                    new DefaultPathTemplateMatchOptions
                    {
                        MatchTrailingSlash = true
                    }
                }
            };
            tt = new ConfigurablePathConstraint();
            constraintFunctions = new Dictionary<string, IPathConstraint>
            {
                { "test", tt }
            };
            expected = new DefaultPathTemplateInternal
            {
                ParsedExamples = new List<DefaultPathTemplateExampleInternal>
                {
                   new DefaultPathTemplateExampleInternal
                   {
                        MatchTrailingSlash = true,
                        Tokens = new List<PathToken>
                        {
                           new PathToken
                           {
                               Type = PathToken.TokenTypeWildCard,
                               Value = "w"
                           },
                           new PathToken
                           {
                               Type = PathToken.TokenTypeSegment,
                               Value = "L"
                           }
                        }
                   },
                   new DefaultPathTemplateExampleInternal
                   {
                       Tokens = new List<PathToken>()
                   }
                },
                DefaultValues = new Dictionary<string, string>
                {
                    { " \n key \r\n:\"\n", " \n value \r\n:\"\n" }
                },
                AllConstraints = new Dictionary<string, IList<(string, string[])>>
                {
                    { "\"\n,lty",  new List<(string, string[])>{ ("test", new string[0]) } }
                },
                ConstraintFunctions = new Dictionary<string, IPathConstraint>
                {
                    { "test", tt }
                }
            };

            testData.Add(new object[] { part1, part2, constraintFunctions, expected });

            return testData;
        }
    }
}