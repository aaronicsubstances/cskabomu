﻿using Kabomu.Common;
using Kabomu.Mediator.Path;
using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.ChunkedTransfer;
using Kabomu.QuasiHttp.EntityBody;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Xunit;
using Xunit.Abstractions;
using static Kabomu.Mediator.Path.DefaultPathTemplateExampleInternal;

namespace Kabomu.Tests.Shared
{
    public static class ComparisonUtils
    {
        public static void AssertLogsEqual(List<string> expectedLogs, List<string> actualLogs, ITestOutputHelper outputHelper)
        {
            try
            {
                Assert.Equal(expectedLogs, actualLogs);
            }
            catch (Exception)
            {
                if (outputHelper != null)
                {
                    outputHelper.WriteLine("Expected:");
                    outputHelper.WriteLine(string.Join(Environment.NewLine, expectedLogs));
                    outputHelper.WriteLine("Actual:");
                    outputHelper.WriteLine(string.Join(Environment.NewLine, actualLogs));
                }
                throw;
            }
        }

        public static void CompareLeadChunks(LeadChunk expected, LeadChunk actual)
        {
            Assert.Equal(expected.Version, actual.Version);
            Assert.Equal(expected.Flags, actual.Flags);
            Assert.Equal(expected.RequestTarget, actual.RequestTarget);
            Assert.Equal(expected.StatusCode, actual.StatusCode);
            Assert.Equal(expected.ContentLength, actual.ContentLength);
            Assert.Equal(expected.ContentType, actual.ContentType);
            Assert.Equal(expected.HttpMethod, actual.HttpMethod);
            Assert.Equal(expected.HttpVersion, actual.HttpVersion);
            Assert.Equal(expected.HttpStatusMessage, actual.HttpStatusMessage);
            CompareHeaders(expected.Headers, actual.Headers);
        }

        public static void CompareSubsequentChunks(SubsequentChunk expected, SubsequentChunk actual)
        {
            Assert.Equal(expected.Version, actual.Version);
            Assert.Equal(expected.Flags, actual.Flags);
            CompareData(expected.Data, expected.DataOffset, expected.DataLength, actual.Data,
                actual.DataOffset, actual.DataLength);
        }

        public static async Task CompareRequests(int maxChunkSize,
            IQuasiHttpRequest expected, IQuasiHttpRequest actual,
            byte[] expectedReqBodyBytes)
        {
            Assert.Equal(expected.HttpMethod, actual.HttpMethod);
            Assert.Equal(expected.HttpVersion, actual.HttpVersion);
            Assert.Equal(expected.Target, actual.Target);
            CompareHeaders(expected.Headers, actual.Headers);
            if (expectedReqBodyBytes == null)
            {
                Assert.Null(actual.Body);
            }
            else
            {
                Assert.NotNull(actual.Body);
                Assert.Equal(expected.Body.ContentLength, actual.Body.ContentLength);
                Assert.Equal(expected.Body.ContentType, actual.Body.ContentType);
                var actualReqBodyBytes = await TransportUtils.ReadBodyToEnd(actual.Body, maxChunkSize);
                Assert.Equal(expectedReqBodyBytes, actualReqBodyBytes);
            }
        }

        public static async Task CompareResponses(int maxChunkSize,
            IQuasiHttpResponse expected, IQuasiHttpResponse actual,
            byte[] expectedResBodyBytes)
        {
            Assert.Equal(expected.StatusCode, actual.StatusCode);
            Assert.Equal(expected.HttpVersion, actual.HttpVersion);
            Assert.Equal(expected.HttpStatusMessage, actual.HttpStatusMessage);
            CompareHeaders(expected.Headers, actual.Headers);
            await CompareBodies(maxChunkSize, expected.Body, actual.Body, expectedResBodyBytes);
        }

        public static async Task CompareBodies(int maxChunkSize, IQuasiHttpBody expected,
            IQuasiHttpBody actual, byte[] expectedBodyBytes)
        {
            if (expectedBodyBytes == null)
            {
                Assert.Null(actual);
            }
            else
            {
                Assert.NotNull(actual);
                Assert.Equal(expected.ContentLength, actual.ContentLength);
                Assert.Equal(expected.ContentType, actual.ContentType);
                var actualResBodyBytes = await TransportUtils.ReadBodyToEnd(actual, maxChunkSize);
                Assert.Equal(expectedBodyBytes, actualResBodyBytes);
            }
        }

        public static void CompareHeaders(IDictionary<string, IList<string>> expected,
            IDictionary<string, IList<string>> actual)
        {
            var expectedKeys = new List<string>();
            if (expected != null)
            {
                foreach (var key in expected.Keys)
                {
                    var value = expected[key];
                    if (value != null & value.Count > 0)
                    {
                        expectedKeys.Add(key);
                    }
                }
            }
            expectedKeys.Sort();
            var actualKeys = new List<string>();
            if (actual != null)
            {
                foreach (var key in actual.Keys)
                {
                    var value = actual[key];
                    if (value != null & value.Count > 0)
                    {
                        actualKeys.Add(key);
                    }
                }
            }
            actualKeys.Sort();
            Assert.Equal(expectedKeys, actualKeys);
            foreach (var key in expectedKeys)
            {
                var expectedValue = expected[key];
                var actualValue = actual[key];
                Assert.Equal(expectedValue, actualValue);
            }
        }

        public static void CompareData(byte[] expectedData, int expectedDataOffset, int expectedDataLength,
            byte[] actualData, int actualDataOffset, int actualDataLength)
        {
            Assert.Equal(expectedDataLength, actualDataLength);
            for (int i = 0; i < expectedDataLength; i++)
            {
                var expectedByte = expectedData[expectedDataOffset + i];
                var actualByte = actualData[actualDataOffset + i];
                Assert.Equal(expectedByte, actualByte);
            }
        }

        public static void AssertTemplatesEqual(IPathTemplate expected, IPathTemplate actual,
            ITestOutputHelper outputHelper)
        {
            try
            {
                CompareTemplates((DefaultPathTemplateInternal)expected,
                    (DefaultPathTemplateInternal)actual);
            }
            catch (Exception)
            {
                if (outputHelper != null)
                {
                    outputHelper.WriteLine("Expected:");
                    outputHelper.WriteLine(JsonConvert.SerializeObject(expected, Newtonsoft.Json.Formatting.Indented));
                    outputHelper.WriteLine("Actual:");
                    outputHelper.WriteLine(JsonConvert.SerializeObject(actual, Newtonsoft.Json.Formatting.Indented));
                }
                throw;
            }
        }

        internal static void CompareTemplates(DefaultPathTemplateInternal expected,
            DefaultPathTemplateInternal actual)
        {
            Assert.Equal(expected.DefaultValues, actual.DefaultValues);
            Assert.Equal(expected.ConstraintFunctions, actual.ConstraintFunctions);
            var expectedConstraintKeys = new List<string>();
            if (expected.AllConstraints != null)
            {
                expectedConstraintKeys.AddRange(expected.AllConstraints.Keys);
            }
            expectedConstraintKeys.Sort();
            var actualConstraintKeys = new List<string>();
            if (actual.AllConstraints != null)
            {
                actualConstraintKeys.AddRange(actual.AllConstraints.Keys);
            }
            actualConstraintKeys.Sort();
            Assert.Equal(expectedConstraintKeys, actualConstraintKeys);
            foreach (var key in expectedConstraintKeys)
            {
                var expectedValue = expected.AllConstraints[key];
                var actualValue = actual.AllConstraints[key];
                Assert.Equal(expectedValue, actualValue);
            }

            Assert.Equal(expected.ParsedExamples.Count, actual.ParsedExamples.Count);
            for (int i = 0; i < expected.ParsedExamples.Count; i++)
            {
                var expectedParsedExample = expected.ParsedExamples[i];
                var actualParsedExample = actual.ParsedExamples[i];
                Assert.Equal(expectedParsedExample.CaseSensitiveMatchEnabled,
                    actualParsedExample.CaseSensitiveMatchEnabled);
                Assert.Equal(expectedParsedExample.UnescapeNonWildCardSegments,
                    actualParsedExample.UnescapeNonWildCardSegments);
                Assert.Equal(expectedParsedExample.MatchLeadingSlash,
                    actualParsedExample.MatchLeadingSlash);
                Assert.Equal(expectedParsedExample.MatchTrailingSlash,
                    actualParsedExample.MatchTrailingSlash);

                Assert.Equal(expectedParsedExample.Tokens.Count,
                    actualParsedExample.Tokens.Count);

                for (int j = 0; j < expectedParsedExample.Tokens.Count; j++)
                {
                    CompareTokens(expectedParsedExample.Tokens[j],
                        actualParsedExample.Tokens[j]);
                }
            }
        }

        internal static void CompareTokens(PathToken expected, PathToken actual)
        {
            Assert.Equal(expected.Type, actual.Type);
            Assert.Equal(expected.Value, actual.Value);
            Assert.Equal(expected.EmptySegmentAllowed, actual.EmptySegmentAllowed);
        }
    }
}
