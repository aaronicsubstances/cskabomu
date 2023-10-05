using Kabomu.Abstractions;
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
            var memStream = new MemoryStream();
            await actual.CopyToAsync(memStream);
            var actualBodyBytes = memStream.ToArray();
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
            Assert.Equal(expected.MaxResponseBodySize,
                actual.MaxResponseBodySize);
            Assert.Equal(expected.TimeoutMillis,
                actual.TimeoutMillis);
            Assert.Equal(expected.ExtraConnectivityParams,
                actual.ExtraConnectivityParams);
            Assert.Equal(expected.MaxHeadersSize,
                actual.MaxHeadersSize);
        }

        public static async Task<string> ReadToString(Stream instance,
            bool readWithOldStyle = false)
        {
            return MiscUtilsInternal.BytesToString(
                await ReadToBytes(instance, readWithOldStyle));
        }

        public static string ReadToStringSync(Stream instance,
            bool readOneByOne = false)
        {
            return MiscUtilsInternal.BytesToString(
                ReadToBytesSync(instance, readOneByOne));
        }

        public static async Task<byte[]> ReadToBytes(Stream instance,
            bool readWithOldStyle = false)
        {
            var memStream = new MemoryStream();
            if (readWithOldStyle)
            {
                var tcs = new TaskCompletionSource();
                var buffer = new byte[IOUtilsInternal.DefaultReadBufferSize];
                AsyncCallback callback = null;
                callback = ar =>
                {
                    int bytesRead;
                    try
                    {
                        bytesRead = instance.EndRead(ar);
                    }
                    catch (Exception e)
                    {
                        tcs.SetException(e);
                        return;
                    }
                    if (bytesRead == 0)
                    {
                        tcs.SetResult();
                        return;
                    }
                    memStream.Write(buffer, 0, bytesRead);
                    instance.BeginRead(buffer, 0, buffer.Length, callback, null);
                };
                instance.BeginRead(buffer, 0, buffer.Length, callback, null);
                await tcs.Task;
            }
            else
            {
                await instance.CopyToAsync(memStream);
            }
            return memStream.ToArray();
        }

        public static byte[] ReadToBytesSync(Stream instance,
            bool readOneByOne = false)
        {
            var memStream = new MemoryStream();
            if (readOneByOne)
            {
                int byteRead;
                while ((byteRead = instance.ReadByte()) != -1)
                {
                    memStream.WriteByte((byte)byteRead);
                }
            }
            else
            {
                instance.CopyTo(memStream);
            }
            return memStream.ToArray();
        }
    }
}
