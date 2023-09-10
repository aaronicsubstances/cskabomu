using Kabomu.Abstractions;
using Kabomu.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Xunit;
using Xunit.Abstractions;

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

        public static async Task CompareRequests(
            IQuasiHttpRequest expected, IQuasiHttpRequest actual,
            byte[] expectedReqBodyBytes)
        {
            if (expected == null)
            {
                Assert.Null(actual);
                return;
            }
            Assert.Equal(expected.HttpMethod, actual.HttpMethod);
            Assert.Equal(expected.HttpVersion, actual.HttpVersion);
            Assert.Equal(expected.Target, actual.Target);
            Assert.Equal(expected.ContentLength, actual.ContentLength);
            CompareHeaders(expected.Headers, actual.Headers);
            //Assert.Equal(expected.Environment, actual.Environment);
            await CompareBodies(expected.Body, actual.Body, expectedReqBodyBytes);
        }

        public static async Task CompareResponses(
            IQuasiHttpResponse expected, IQuasiHttpResponse actual,
            byte[] expectedResBodyBytes)
        {
            if (expected == null)
            {
                Assert.Null(actual);
                return;
            }
            Assert.NotNull(actual);
            Assert.Equal(expected.StatusCode, actual.StatusCode);
            Assert.Equal(expected.HttpVersion, actual.HttpVersion);
            Assert.Equal(expected.HttpStatusMessage, actual.HttpStatusMessage);
            Assert.Equal(expected.ContentLength, actual.ContentLength);
            CompareHeaders(expected.Headers, actual.Headers);
            //Assert.Equal(expected.Environment, actual.Environment);
            await CompareBodies(expected.Body, actual.Body, expectedResBodyBytes);
        }

        public static async Task CompareBodies(Stream expected,
            Stream actual, byte[] expectedBodyBytes)
        {
            if (expectedBodyBytes == null)
            {
                Assert.Same(expected, actual);
                return;
            }
            Assert.NotNull(actual);
            var actualBodyBytes = (await MiscUtils.ReadAllBytes(actual)).ToArray();
            Assert.Equal(expectedBodyBytes, actualBodyBytes);
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
                    if (value != null && value.Count > 0)
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
                    if (value != null && value.Count > 0)
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

        public static void CompareProcessingOptions(
            IQuasiHttpProcessingOptions expected,
            IQuasiHttpProcessingOptions actual)
        {
            if (expected == null)
            {
                Assert.Null(actual);
                return;
            }
            Assert.NotNull(actual);
            Assert.Equal(expected.ResponseBufferingEnabled,
                actual.ResponseBufferingEnabled);
            Assert.Equal(expected.ResponseBodyBufferingSizeLimit,
                actual.ResponseBodyBufferingSizeLimit);
            Assert.Equal(expected.TimeoutMillis,
                actual.TimeoutMillis);
            Assert.Equal(expected.ExtraConnectivityParams,
                actual.ExtraConnectivityParams);
            Assert.Equal(expected.MaxHeadersSize,
                actual.MaxHeadersSize);
        }

        public static Stream CreateRandomizedChunkStream(byte[] b)
        {
            async IAsyncEnumerable<byte[]> Generate()
            {
                await Task.Yield();
                int offset = 0;
                while (offset < b.Length)
                {
                    int bytesToCopy = Random.Shared.Next(b.Length - offset) + 1;
                    var nextChunk = new byte[bytesToCopy];
                    Array.Copy(b, offset, nextChunk, 0, bytesToCopy);
                    yield return nextChunk;
                    offset += bytesToCopy;
                    await Task.Yield();
                }
            }
            return new AsyncEnumerableBackedStream(Generate());
        }

        public static Stream CreateInputStreamFromSource(
            Func<byte[], int, int, Task<int>> source)
        {
            async IAsyncEnumerable<byte[]> Generate()
            {
                var readBuffer = new byte[8192];
                while (true)
                {
                    int bytesRead = await source(readBuffer, 0, readBuffer.Length);
                    if (bytesRead > readBuffer.Length)
                    {
                        throw new ExpectationViolationException(
                            "read beyond requested length: " +
                            $"({bytesRead} > {readBuffer.Length})");
                    }
                    if (bytesRead > 0)
                    {
                        var chunk = new byte[bytesRead];
                        Array.Copy(readBuffer, chunk, bytesRead);
                        yield return chunk;
                    }
                    else
                    {
                        break;
                    }
                }
            }
            return new AsyncEnumerableBackedStream(Generate());
        }
    }
}
