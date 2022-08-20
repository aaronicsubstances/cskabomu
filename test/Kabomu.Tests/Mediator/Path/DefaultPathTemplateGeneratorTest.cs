using Kabomu.Mediator.Handling;
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
        [MemberData(nameof(CreateTestParseData))]
        public void TestParse(string part1, object part2,
            IDictionary<string, IPathConstraint> constraintFunctions,
            IPathTemplate expected, string expectedError, int errorRowNum, int errorColNum)
        {
            var instance = new DefaultPathTemplateGenerator
            {
                ConstraintFunctions = constraintFunctions
            };

            Exception actualError = null;
            IPathTemplate actual = null;
            try
            {
                actual = instance.Parse(part1, part2);
            }
            catch (Exception e)
            {
                actualError = e;
            }

            if (expectedError == null)
            {
                Assert.Null(actualError);
                ComparisonUtils.AssertTemplatesEqual(expected, actual, _outputHelper);
            }
            else
            {
                Assert.NotNull(actualError);
                Assert.Contains($"row {errorRowNum}", actualError.Message);
                Assert.Contains($"column {errorColNum}", actualError.Message);
                Assert.Contains(expectedError, actualError.Message);
            }
        }

        public static List<object[]> CreateTestParseData()
        {
            var testData = new List<object[]>();

            string part1 = "";
            object part2 = null;
            IDictionary<string, IPathConstraint> constraintFunctions = null;
            DefaultPathTemplateInternal expected = null;
            string expectedError = "no examples";
            int errorRowNum = 0;
            int errorColNum = 0;

            testData.Add(new object[] { part1, part2, constraintFunctions,
                expected, expectedError, errorRowNum, errorColNum });

            part1 = "/";
            part2 = null;
            constraintFunctions = null;
            expected = new DefaultPathTemplateInternal
            {
                ParsedExamples = new List<DefaultPathTemplateExampleInternal>
                {
                   new DefaultPathTemplateExampleInternal
                   {
                       Tokens = new List<PathToken>()
                   }  
                },
                DefaultValues = new Dictionary<string, string>(),
                AllConstraints = new Dictionary<string, IList<(string, string[])>>(),
                ConstraintFunctions = new Dictionary<string, IPathConstraint>(),
            };
            expectedError = null;
            errorRowNum = 0;
            errorColNum = 0;

            testData.Add(new object[] { part1, part2, constraintFunctions,
                expected, expectedError, errorRowNum, errorColNum });

            part1 = "/  ";
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
                               Value = "",
                               EmptySegmentAllowed = true
                           }
                       }
                   }
                },
                DefaultValues = new Dictionary<string, string>(),
                AllConstraints = new Dictionary<string, IList<(string, string[])>>(),
                ConstraintFunctions = new Dictionary<string, IPathConstraint>(),
            };
            expectedError = null;
            errorRowNum = 0;
            errorColNum = 0;

            testData.Add(new object[] { part1, part2, constraintFunctions,
                expected, expectedError, errorRowNum, errorColNum });

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
                },
                DefaultValues = new Dictionary<string, string>(),
                AllConstraints = new Dictionary<string, IList<(string, string[])>>(),
                ConstraintFunctions = new Dictionary<string, IPathConstraint>(),
            };
            expectedError = null;
            errorRowNum = 0;
            errorColNum = 0;

            testData.Add(new object[] { part1, part2, constraintFunctions,
                expected, expectedError, errorRowNum, errorColNum });

            part1 = "/car ";
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
                           }
                       }
                   }
                },
                DefaultValues = new Dictionary<string, string>(),
                AllConstraints = new Dictionary<string, IList<(string, string[])>>(),
                ConstraintFunctions = new Dictionary<string, IPathConstraint>(),
            };
            expectedError = null;
            errorRowNum = 0;
            errorColNum = 0;

            testData.Add(new object[] { part1, part2, constraintFunctions,
                expected, expectedError, errorRowNum, errorColNum });

            part1 = "/car //vehicle";
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
                },
                DefaultValues = new Dictionary<string, string>(),
                AllConstraints = new Dictionary<string, IList<(string, string[])>>(),
                ConstraintFunctions = new Dictionary<string, IPathConstraint>(),
            };
            expectedError = null;
            errorRowNum = 0;
            errorColNum = 0;

            testData.Add(new object[] { part1, part2, constraintFunctions,
                expected, expectedError, errorRowNum, errorColNum });

            part1 = "/car //vehicle /// ,///yr//second// first/sei/du\n" +
                "defaults,country,gh,capital,accra\n" +
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
                },
                AllConstraints = new Dictionary<string, IList<(string, string[])>>(),
                ConstraintFunctions = new Dictionary<string, IPathConstraint>(),
            };
            expectedError = null;
            errorRowNum = 0;
            errorColNum = 0;

            testData.Add(new object[] { part1, part2, constraintFunctions,
                expected, expectedError, errorRowNum, errorColNum });

            return testData;
        }

        class TempPathConstraint : IPathConstraint
        {
            public bool MatchValue { get; set; }

            public bool Match(IContext context, IPathTemplate pathTemplate,
                IDictionary<string, string> values, string valueKey,
                string[] constraintArgs, int direction)
            {
                return MatchValue;
            }
        }
    }
}
