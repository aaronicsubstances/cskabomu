using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Kabomu.Tests.Shared
{
    public static class ComparisonUtils
    {
        public static void CompareRequests(IMutexApi mutex, int maxChunkSize,
            IQuasiHttpRequest expected, IQuasiHttpRequest actual,
            string expectedReqBodyStr)
        {
            Assert.Equal(expected.Path, actual.Path);
            CompareHeaders(expected.Headers, actual.Headers);
            if (expectedReqBodyStr == null)
            {
                Assert.Null(actual.Body);
            }
            else
            {
                Assert.NotNull(actual.Body);
                Assert.Equal(expected.Body.ContentLength, actual.Body.ContentLength);
                Assert.Equal(expected.Body.ContentType, actual.Body.ContentType);
                byte[] actualReqBodyBytes = null;
                var cbCalled = false;
                TransportUtils.ReadBodyToEnd(actual.Body, mutex, maxChunkSize, (e, data) =>
                {
                    Assert.False(cbCalled);
                    Assert.Null(e);
                    actualReqBodyBytes = data;
                    cbCalled = true;
                });
                Assert.True(cbCalled);
                var actualReqBodyStr = Encoding.UTF8.GetString(actualReqBodyBytes, 0,
                    actualReqBodyBytes.Length);
                Assert.Equal(expectedReqBodyStr, actualReqBodyStr);
            }
        }

        public static void CompareResponses(IMutexApi mutex, int maxChunkSize,
            IQuasiHttpResponse expected, IQuasiHttpResponse actual,
            string expectedResBodyStr)
        {
            Assert.Equal(expected.StatusIndicatesSuccess, actual.StatusIndicatesSuccess);
            Assert.Equal(expected.StatusIndicatesClientError, actual.StatusIndicatesClientError);
            Assert.Equal(expected.StatusMessage, actual.StatusMessage);
            CompareHeaders(expected.Headers, actual.Headers);
            if (expectedResBodyStr == null)
            {
                Assert.Null(actual.Body);
            }
            else
            {
                Assert.NotNull(actual.Body);
                Assert.Equal(expected.Body.ContentLength, actual.Body.ContentLength);
                Assert.Equal(expected.Body.ContentType, actual.Body.ContentType);
                byte[] actualResBodyBytes = null;
                var cbCalled = false;
                TransportUtils.ReadBodyToEnd(actual.Body, mutex, maxChunkSize, (e, data) =>
                {
                    Assert.False(cbCalled);
                    Assert.Null(e);
                    actualResBodyBytes = data;
                    cbCalled = true;
                });
                Assert.True(cbCalled);
                var actualResBodyStr = Encoding.UTF8.GetString(actualResBodyBytes, 0,
                    actualResBodyBytes.Length);
                Assert.Equal(expectedResBodyStr, actualResBodyStr);
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
