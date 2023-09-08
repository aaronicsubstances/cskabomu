using Kabomu.Common;
using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.ChunkedTransfer;
using Kabomu.QuasiHttp.EntityBody;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Xunit;
using Xunit.Abstractions;

[assembly: InternalsVisibleTo("Kabomu.Tests")]
[assembly: InternalsVisibleTo("Kabomu.IntegrationTests")]

namespace Kabomu.Tests.Shared.Common
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
            Assert.Equal(expected.Method, actual.Method);
            Assert.Equal(expected.HttpVersion, actual.HttpVersion);
            Assert.Equal(expected.HttpStatusMessage, actual.HttpStatusMessage);
            CompareHeaders(expected.Headers, actual.Headers);
        }

        public static void AssertSetEqual(ICollection<string> expected, ICollection<string> actual)
        {
            var expectedWrapper = new HashSet<string>(expected);
            var actualWrapper = new HashSet<string>(actual);
            Assert.Subset(expectedWrapper, actualWrapper);
            Assert.Superset(expectedWrapper, actualWrapper);
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
            Assert.Equal(expected.Method, actual.Method);
            Assert.Equal(expected.HttpVersion, actual.HttpVersion);
            Assert.Equal(expected.Target, actual.Target);
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
            CompareHeaders(expected.Headers, actual.Headers);
            //Assert.Equal(expected.Environment, actual.Environment);
            await CompareBodies(expected.Body, actual.Body, expectedResBodyBytes);
        }

        public static async Task CompareBodies(IQuasiHttpBody expected,
            IQuasiHttpBody actual, byte[] expectedBodyBytes)
        {
            if (expectedBodyBytes == null)
            {
                Assert.Same(expected, actual);
                return;
            }
            Assert.NotNull(actual);
            Assert.Equal(expected.ContentLength, actual.ContentLength);
            var actualBodyBytes = await IOUtils.ReadAllBytes(actual.AsReader());
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

        public static void CompareConnectivityParams(
            object expectedRemoteEndpoint, object actualRemoteEndpoint,
            IQuasiHttpProcessingOptions expectedSendOptions, IQuasiHttpProcessingOptions actualSendOptions)
        {
            Assert.Equal(expectedRemoteEndpoint, actualRemoteEndpoint);
            if (expectedSendOptions == null)
            {
                Assert.Null(actualSendOptions);
                return;
            }
            Assert.NotNull(actualSendOptions);
            Assert.Equal(expectedSendOptions.ResponseBufferingEnabled,
                actualSendOptions.ResponseBufferingEnabled);
            Assert.Equal(expectedSendOptions.ResponseBodyBufferingSizeLimit,
                actualSendOptions.ResponseBodyBufferingSizeLimit);
            Assert.Equal(expectedSendOptions.TimeoutMillis,
                actualSendOptions.TimeoutMillis);
            Assert.Equal(expectedSendOptions.ExtraConnectivityParams,
                actualSendOptions.ExtraConnectivityParams);
            Assert.Equal(expectedSendOptions.MaxHeadersSize,
                actualSendOptions.MaxHeadersSize);
        }

        /// <summary>
        /// Provides equivalent functionality to Promise.all() of NodeJS
        /// </summary>
        /// <param name="candiates">tasks</param>
        /// <returns>asynchronous result which represents successful
        /// end of all arguments, or failure of one of them</returns>
        public static async Task WhenAnyFailOrAllSucceed(List<Task> candiates)
        {
            var newList = new List<Task>(candiates);
            while (newList.Count > 0)
            {
                var t = await Task.WhenAny(newList);
                await t;
                newList.Remove(t);
            }
        }
    }
}
