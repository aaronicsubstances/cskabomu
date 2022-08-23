using Kabomu.Mediator.Path;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Kabomu.Tests.Mediator.Path
{
    public class PathUtilsInternalTest
    {
        [Fact]
        public void TestReverseUnnecessaryUriEscapes()
        {
            var actual = PathUtilsInternal.ReverseUnnecessaryUriEscapes("");
            Assert.Equal("", actual);

            actual = PathUtilsInternal.ReverseUnnecessaryUriEscapes("ad");
            Assert.Equal("ad", actual);

            actual = PathUtilsInternal.ReverseUnnecessaryUriEscapes("%");
            Assert.Equal("%", actual);

            actual = PathUtilsInternal.ReverseUnnecessaryUriEscapes("%%gh");
            Assert.Equal("%%gh", actual);

            actual = PathUtilsInternal.ReverseUnnecessaryUriEscapes("%25%20ad\n%41");
            Assert.Equal("%25%20ad\nA", actual);

            actual = PathUtilsInternal.ReverseUnnecessaryUriEscapes("%20%25ad\n%41%42-%61%63%");
            Assert.Equal("%20%25ad\nAB-ac%", actual);
        }

        [Fact]
        public void TestFastConvertPercentEncodedToPositiveNum()
        {
            var s = new StringBuilder();
            s.Append("%=:-21-%24-%26-%27-%28-%29-%2a-%2b-%2c-%3B-%3D-%3A-%40-%25-");
            s.Append("%41-%42-%59-%5a-%61-%62-%79-%7a-%30-%31-%38-%39");

            var actual = PathUtilsInternal.FastConvertPercentEncodedToPositiveNum(s, 0);
            Assert.Equal(0, actual);

            actual = PathUtilsInternal.FastConvertPercentEncodedToPositiveNum(s, 1);
            Assert.Equal(0, actual);

            actual = PathUtilsInternal.FastConvertPercentEncodedToPositiveNum(s, 4);
            Assert.Equal('!', actual);

            actual = PathUtilsInternal.FastConvertPercentEncodedToPositiveNum(s, 8);
            Assert.Equal('$', actual);

            actual = PathUtilsInternal.FastConvertPercentEncodedToPositiveNum(s, 12);
            Assert.Equal('&', actual);

            actual = PathUtilsInternal.FastConvertPercentEncodedToPositiveNum(s, 16);
            Assert.Equal('\'', actual);

            actual = PathUtilsInternal.FastConvertPercentEncodedToPositiveNum(s, 20);
            Assert.Equal('(', actual);

            actual = PathUtilsInternal.FastConvertPercentEncodedToPositiveNum(s, 24);
            Assert.Equal(')', actual);

            actual = PathUtilsInternal.FastConvertPercentEncodedToPositiveNum(s, 28);
            Assert.Equal('*', actual);

            actual = PathUtilsInternal.FastConvertPercentEncodedToPositiveNum(s, 32);
            Assert.Equal('+', actual);

            actual = PathUtilsInternal.FastConvertPercentEncodedToPositiveNum(s, 36);
            Assert.Equal(',', actual);

            actual = PathUtilsInternal.FastConvertPercentEncodedToPositiveNum(s, 40);
            Assert.Equal(';', actual);

            actual = PathUtilsInternal.FastConvertPercentEncodedToPositiveNum(s, 44);
            Assert.Equal('=', actual);

            actual = PathUtilsInternal.FastConvertPercentEncodedToPositiveNum(s, 48);
            Assert.Equal(':', actual);

            actual = PathUtilsInternal.FastConvertPercentEncodedToPositiveNum(s, 52);
            Assert.Equal('@', actual);

            actual = PathUtilsInternal.FastConvertPercentEncodedToPositiveNum(s, 56);
            Assert.Equal(0, actual);

            actual = PathUtilsInternal.FastConvertPercentEncodedToPositiveNum(s, 60);
            Assert.Equal('A', actual);

            actual = PathUtilsInternal.FastConvertPercentEncodedToPositiveNum(s, 64);
            Assert.Equal('B', actual);

            actual = PathUtilsInternal.FastConvertPercentEncodedToPositiveNum(s, 68);
            Assert.Equal('Y', actual);

            actual = PathUtilsInternal.FastConvertPercentEncodedToPositiveNum(s, 72);
            Assert.Equal('Z', actual);

            actual = PathUtilsInternal.FastConvertPercentEncodedToPositiveNum(s, 76);
            Assert.Equal('a', actual);

            actual = PathUtilsInternal.FastConvertPercentEncodedToPositiveNum(s, 80);
            Assert.Equal('b', actual);

            actual = PathUtilsInternal.FastConvertPercentEncodedToPositiveNum(s, 84);
            Assert.Equal('y', actual);

            actual = PathUtilsInternal.FastConvertPercentEncodedToPositiveNum(s, 88);
            Assert.Equal('z', actual);

            actual = PathUtilsInternal.FastConvertPercentEncodedToPositiveNum(s, 92);
            Assert.Equal('0', actual);

            actual = PathUtilsInternal.FastConvertPercentEncodedToPositiveNum(s, 96);
            Assert.Equal('1', actual);

            actual = PathUtilsInternal.FastConvertPercentEncodedToPositiveNum(s, 100);
            Assert.Equal('8', actual);

            actual = PathUtilsInternal.FastConvertPercentEncodedToPositiveNum(s, 104);
            Assert.Equal('9', actual);
        }

        [Fact]
        public void TestEncodeAlmostEveryUriChar()
        {
            var expected = "%22a-zA-Z0-9-._~%20%21%24%26%27%28%29%2A%2B%2C%3B%3D%3A%40";
            var actual = PathUtilsInternal.EncodeAlmostEveryUriChar(
                "\"a-zA-Z0-9-._~ !$&'()*+,;=:@");
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestAreTwoPossiblyNullStringsEqual()
        {
            var expected = true;
            var actual = PathUtilsInternal.AreTwoPossiblyNullStringsEqual(null, null, true);
            Assert.Equal(expected, actual);

            expected = false;
            actual = PathUtilsInternal.AreTwoPossiblyNullStringsEqual(null, "", true);
            Assert.Equal(expected, actual);

            expected = false;
            actual = PathUtilsInternal.AreTwoPossiblyNullStringsEqual("", null, true);
            Assert.Equal(expected, actual);

            expected = true;
            actual = PathUtilsInternal.AreTwoPossiblyNullStringsEqual("", "", true);
            Assert.Equal(expected, actual);

            expected = false;
            actual = PathUtilsInternal.AreTwoPossiblyNullStringsEqual("food", "", true);
            Assert.Equal(expected, actual);

            expected = false;
            actual = PathUtilsInternal.AreTwoPossiblyNullStringsEqual("food", "FOOD", false);
            Assert.Equal(expected, actual);

            expected = true;
            actual = PathUtilsInternal.AreTwoPossiblyNullStringsEqual("food", "FOOD", true);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestConvertPossibleNullToString()
        {
            var expected = "";
            var actual = PathUtilsInternal.ConvertPossibleNullToString(null);
            Assert.Equal(expected, actual);

            expected = "34";
            actual = PathUtilsInternal.ConvertPossibleNullToString(34);
            Assert.Equal(expected, actual);

            expected = "tree";
            actual = PathUtilsInternal.ConvertPossibleNullToString("tree");
            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(CreateGetEffectiveEscapeNonWildCardSegmentData))]
        public void TestGetEffectiveEscapeNonWildCardSegment(
            DefaultPathTemplateFormatOptions options,
            object parsedExample,
            bool expected)
        {
            var actual = PathUtilsInternal.GetEffectiveEscapeNonWildCardSegment(options,
                (DefaultPathTemplateExampleInternal)parsedExample);
            Assert.Equal(expected, actual);
        }

        public static List<object[]> CreateGetEffectiveEscapeNonWildCardSegmentData()
        {
            var testData = new List<object[]>();

            DefaultPathTemplateFormatOptions options = null;
            DefaultPathTemplateExampleInternal parsedExample = new DefaultPathTemplateExampleInternal();
            bool expected = true;
            testData.Add(new object[]{ options, parsedExample, expected });

            options = new DefaultPathTemplateFormatOptions();
            parsedExample = new DefaultPathTemplateExampleInternal();
            expected = true;
            testData.Add(new object[] { options, parsedExample, expected });

            options = new DefaultPathTemplateFormatOptions();
            parsedExample = new DefaultPathTemplateExampleInternal
            {
                UnescapeNonWildCardSegments = false
            };
            expected = false;
            testData.Add(new object[] { options, parsedExample, expected });

            options = new DefaultPathTemplateFormatOptions
            {
                EscapeNonWildCardSegments = false
            };
            parsedExample = new DefaultPathTemplateExampleInternal();
            expected = false;
            testData.Add(new object[] { options, parsedExample, expected });

            options = new DefaultPathTemplateFormatOptions
            {
                EscapeNonWildCardSegments = false
            };
            parsedExample = new DefaultPathTemplateExampleInternal
            {
                UnescapeNonWildCardSegments = true
            };
            expected = false;
            testData.Add(new object[] { options, parsedExample, expected });

            options = new DefaultPathTemplateFormatOptions
            {
                EscapeNonWildCardSegments = true
            };
            parsedExample = new DefaultPathTemplateExampleInternal
            {
                UnescapeNonWildCardSegments = false
            };
            expected = true;
            testData.Add(new object[] { options, parsedExample, expected });

            options = new DefaultPathTemplateFormatOptions
            {
                EscapeNonWildCardSegments = null
            };
            parsedExample = new DefaultPathTemplateExampleInternal
            {
                UnescapeNonWildCardSegments = false
            };
            expected = false;
            testData.Add(new object[] { options, parsedExample, expected });

            options = new DefaultPathTemplateFormatOptions
            {
                EscapeNonWildCardSegments = null
            };
            parsedExample = new DefaultPathTemplateExampleInternal
            {
                UnescapeNonWildCardSegments = true
            };
            expected = true;
            testData.Add(new object[] { options, parsedExample, expected });

            return testData;
        }

        [Theory]
        [MemberData(nameof(CreateGetEffectiveApplyLeadingSlashData))]
        public void TestGetEffectiveApplyLeadingSlash(
            DefaultPathTemplateFormatOptions options,
            object parsedExample,
            bool expected)
        {
            var actual = PathUtilsInternal.GetEffectiveApplyLeadingSlash(options,
                (DefaultPathTemplateExampleInternal)parsedExample);
            Assert.Equal(expected, actual);
        }

        public static List<object[]> CreateGetEffectiveApplyLeadingSlashData()
        {
            var testData = new List<object[]>();

            DefaultPathTemplateFormatOptions options = null;
            DefaultPathTemplateExampleInternal parsedExample = new DefaultPathTemplateExampleInternal();
            bool expected = true;
            testData.Add(new object[] { options, parsedExample, expected });

            options = new DefaultPathTemplateFormatOptions();
            parsedExample = new DefaultPathTemplateExampleInternal();
            expected = true;
            testData.Add(new object[] { options, parsedExample, expected });

            options = new DefaultPathTemplateFormatOptions();
            parsedExample = new DefaultPathTemplateExampleInternal
            {
                MatchLeadingSlash = false
            };
            expected = false;
            testData.Add(new object[] { options, parsedExample, expected });

            options = new DefaultPathTemplateFormatOptions
            {
                ApplyLeadingSlash = false
            };
            parsedExample = new DefaultPathTemplateExampleInternal();
            expected = false;
            testData.Add(new object[] { options, parsedExample, expected });

            options = new DefaultPathTemplateFormatOptions
            {
                ApplyLeadingSlash = false
            };
            parsedExample = new DefaultPathTemplateExampleInternal
            {
                MatchLeadingSlash = true
            };
            expected = false;
            testData.Add(new object[] { options, parsedExample, expected });

            options = new DefaultPathTemplateFormatOptions
            {
                ApplyLeadingSlash = true
            };
            parsedExample = new DefaultPathTemplateExampleInternal
            {
                MatchLeadingSlash = false
            };
            expected = true;
            testData.Add(new object[] { options, parsedExample, expected });

            options = new DefaultPathTemplateFormatOptions
            {
                ApplyLeadingSlash = null
            };
            parsedExample = new DefaultPathTemplateExampleInternal
            {
                MatchLeadingSlash = false
            };
            expected = false;
            testData.Add(new object[] { options, parsedExample, expected });

            options = new DefaultPathTemplateFormatOptions
            {
                ApplyLeadingSlash = null
            };
            parsedExample = new DefaultPathTemplateExampleInternal
            {
                MatchLeadingSlash = true
            };
            expected = true;
            testData.Add(new object[] { options, parsedExample, expected });

            return testData;
        }

        [Theory]
        [MemberData(nameof(CreateGetEffectiveApplyTrailingSlashData))]
        public void TestGetEffectiveApplyTrailingSlash(
            DefaultPathTemplateFormatOptions options,
            object parsedExample,
            bool expected)
        {
            var actual = PathUtilsInternal.GetEffectiveApplyTrailingSlash(options,
                (DefaultPathTemplateExampleInternal)parsedExample);
            Assert.Equal(expected, actual);
        }

        public static List<object[]> CreateGetEffectiveApplyTrailingSlashData()
        {
            var testData = new List<object[]>();

            DefaultPathTemplateFormatOptions options = null;
            DefaultPathTemplateExampleInternal parsedExample = new DefaultPathTemplateExampleInternal();
            bool expected = false;
            testData.Add(new object[] { options, parsedExample, expected });

            options = new DefaultPathTemplateFormatOptions();
            parsedExample = new DefaultPathTemplateExampleInternal();
            expected = false;
            testData.Add(new object[] { options, parsedExample, expected });

            options = new DefaultPathTemplateFormatOptions();
            parsedExample = new DefaultPathTemplateExampleInternal
            {
                MatchTrailingSlash = false
            };
            expected = false;
            testData.Add(new object[] { options, parsedExample, expected });

            options = new DefaultPathTemplateFormatOptions
            {
                ApplyTrailingSlash = false
            };
            parsedExample = new DefaultPathTemplateExampleInternal();
            expected = false;
            testData.Add(new object[] { options, parsedExample, expected });

            options = new DefaultPathTemplateFormatOptions
            {
                ApplyTrailingSlash = false
            };
            parsedExample = new DefaultPathTemplateExampleInternal
            {
                MatchTrailingSlash = true
            };
            expected = false;
            testData.Add(new object[] { options, parsedExample, expected });

            options = new DefaultPathTemplateFormatOptions
            {
                ApplyTrailingSlash = true
            };
            parsedExample = new DefaultPathTemplateExampleInternal
            {
                MatchTrailingSlash = false
            };
            expected = true;
            testData.Add(new object[] { options, parsedExample, expected });

            options = new DefaultPathTemplateFormatOptions
            {
                ApplyTrailingSlash = null
            };
            parsedExample = new DefaultPathTemplateExampleInternal
            {
                MatchTrailingSlash = false
            };
            expected = false;
            testData.Add(new object[] { options, parsedExample, expected });

            options = new DefaultPathTemplateFormatOptions
            {
                ApplyTrailingSlash = null
            };
            parsedExample = new DefaultPathTemplateExampleInternal
            {
                MatchTrailingSlash = true
            };
            expected = true;
            testData.Add(new object[] { options, parsedExample, expected });

            return testData;
        }

        [Theory]
        [MemberData(nameof(CreateTestNormalizeAndSplitPathData))]
        public void TestNormalizeAndSplitPath(string path, IList<string> expected)
        {
            var actual = PathUtilsInternal.NormalizeAndSplitPath(path);
            Assert.Equal(expected, actual);
        }

        public static List<object[]> CreateTestNormalizeAndSplitPathData()
        {
            var testData = new List<object[]>();

            string path = "";
            string[] expected = new string[0];
            testData.Add(new object[]{ path, expected });

            path = "/";
            expected = new string[0];
            testData.Add(new object[] { path, expected });

            path = "//";
            expected = new string[] { "" };
            testData.Add(new object[] { path, expected });

            path = "///";
            expected = new string[] { "", "" };
            testData.Add(new object[] { path, expected });

            path = "/ ";
            expected = new string[] { " " };
            testData.Add(new object[] { path, expected });

            path = "// ";
            expected = new string[] { "", " " };
            testData.Add(new object[] { path, expected });

            path = " ";
            expected = new string[] { " " };
            testData.Add(new object[] { path, expected });

            path = " /sea/turtle//in/there/";
            expected = new string[] { " ", "sea", "turtle", "", "in", "there" };
            testData.Add(new object[] { path, expected });

            path = "bread";
            expected = new string[] { "bread" };
            testData.Add(new object[] { path, expected });

            path = "/bread";
            expected = new string[] { "bread" };
            testData.Add(new object[] { path, expected });

            path = "bread/";
            expected = new string[] { "bread" };
            testData.Add(new object[] { path, expected });

            path = "/bread";
            expected = new string[] { "bread" };
            testData.Add(new object[] { path, expected });

            path = "water/of/life";
            expected = new string[] { "water", "of", "life" };
            testData.Add(new object[] { path, expected });

            path = "/water/of/life";
            expected = new string[] { "water", "of", "life" };
            testData.Add(new object[] { path, expected });

            path = "water/of/life/";
            expected = new string[] { "water", "of", "life" };
            testData.Add(new object[] { path, expected });

            path = "/water/of/life";
            expected = new string[] { "water", "of", "life" };
            testData.Add(new object[] { path, expected });

            return testData;
        }

        [Theory]
        [MemberData(nameof(CreateTestExtractPathData))]
        public void TestExtractPath(string requestTarget, string expected)
        {
            var actual = PathUtilsInternal.ExtractPath(requestTarget);
            Assert.Equal(expected, actual);
        }

        public static List<object[]> CreateTestExtractPathData()
        {
            return new List<object[]>
            {
                new object[]{ "", "" },
                new object[]{ null, "" },
                new object[]{ "/", "/" },
                new object[]{ "http://localhost", "" },
                new object[]{ "http://localhost/", "/" },
                new object[]{ "http://localhost?week", "" },
                new object[]{ "http://localhost/?week", "/" },
                new object[]{ "http://localhost/day#week", "/day" },
                new object[]{ "http://localhost/~-_.%41ABab012%22%25%20?week#month", "/~-_.%41ABab012%22%25%20" },
                new object[]{ "http://localhost/23/%2016?week", "/23/%2016" },
                new object[]{ "//localhost/23/%2016?week", "/23/%2016" },
                new object[]{ "file:/23/%2016?week", "/23/%2016" },
                new object[]{ "file:///23/%2016?week", "/23/%2016" },
                new object[]{ "file://localhost/23/%2016?week", "/23/%2016" },
            };
        }
    }
}
