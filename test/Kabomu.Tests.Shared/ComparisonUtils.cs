using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.Shared
{
    public static class ComparisonUtils
    {
        public static void CompareLeadChunks(LeadChunk expected, LeadChunk actual)
        {
            Assert.Equal(expected.Version, actual.Version);
            Assert.Equal(expected.Flags, actual.Flags);
            Assert.Equal(expected.Path, actual.Path);
            Assert.Equal(expected.StatusIndicatesSuccess, actual.StatusIndicatesSuccess);
            Assert.Equal(expected.StatusIndicatesClientError, actual.StatusIndicatesClientError);
            Assert.Equal(expected.StatusMessage, actual.StatusMessage);
            Assert.Equal(expected.ContentLength, actual.ContentLength);
            Assert.Equal(expected.ContentType, actual.ContentType);
            Assert.Equal(expected.HttpMethod, actual.HttpMethod);
            Assert.Equal(expected.HttpVersion, actual.HttpVersion);
            Assert.Equal(expected.HttpStatusCode, actual.HttpStatusCode);
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
            Assert.Equal(expected.Path, actual.Path);
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
            Assert.Equal(expected.HttpStatusCode, actual.HttpStatusCode);
            Assert.Equal(expected.HttpVersion, actual.HttpVersion);
            Assert.Equal(expected.StatusIndicatesSuccess, actual.StatusIndicatesSuccess);
            Assert.Equal(expected.StatusIndicatesClientError, actual.StatusIndicatesClientError);
            Assert.Equal(expected.StatusMessage, actual.StatusMessage);
            CompareHeaders(expected.Headers, actual.Headers);
            if (expectedResBodyBytes == null)
            {
                Assert.Null(actual.Body);
            }
            else
            {
                Assert.NotNull(actual.Body);
                Assert.Equal(expected.Body.ContentLength, actual.Body.ContentLength);
                Assert.Equal(expected.Body.ContentType, actual.Body.ContentType);
                var actualResBodyBytes = await TransportUtils.ReadBodyToEnd(actual.Body, maxChunkSize);
                Assert.Equal(expectedResBodyBytes, actualResBodyBytes);
            }
        }

        public static void CompareHeaders(Dictionary<string, List<string>> expected,
            Dictionary<string, List<string>> actual)
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
    }
}
