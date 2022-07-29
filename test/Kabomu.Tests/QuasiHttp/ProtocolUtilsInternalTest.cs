using Kabomu.Common;
using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.EntityBody;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.QuasiHttp
{
    public class ProtocolUtilsInternalTest
    {
        [Theory]
        [MemberData(nameof(CreateTestDetermineEffectiveNonZeroIntegerOptionData))]
        public void TestDetermineEffectiveNonZeroIntegerOption(int? preferred,
            int? fallback1, int defaultValue, int expected)
        {
            var actual = ProtocolUtilsInternal.DetermineEffectiveNonZeroIntegerOption(
                preferred, fallback1, defaultValue);
            Assert.Equal(expected, actual);
        }

        public static List<object[]> CreateTestDetermineEffectiveNonZeroIntegerOptionData()
        {
            var testData = new List<object[]>();

            int? preferred = 1;
            int? fallback1 = null;
            int defaultValue = 20;
            int expected = 1;
            testData.Add(new object[] { preferred, fallback1, defaultValue,
                expected });

            preferred = 5;
            fallback1 = 3;
            defaultValue = 11;
            expected = 5;
            testData.Add(new object[] { preferred, fallback1, defaultValue,
                expected });

            preferred = -15;
            fallback1 = 3;
            defaultValue = -1;
            expected = -15;
            testData.Add(new object[] { preferred, fallback1, defaultValue,
                expected });

            preferred = null;
            fallback1 = 3;
            defaultValue = -1;
            expected = 3;
            testData.Add(new object[] { preferred, fallback1, defaultValue,
                expected });

            preferred = null;
            fallback1 = -3;
            defaultValue = -1;
            expected = -3;
            testData.Add(new object[] { preferred, fallback1, defaultValue,
                expected });

            preferred = null;
            fallback1 = null;
            defaultValue = 2;
            expected = 2;
            testData.Add(new object[] { preferred, fallback1, defaultValue,
                expected });

            preferred = null;
            fallback1 = null;
            defaultValue = -8;
            expected = -8;
            testData.Add(new object[] { preferred, fallback1, defaultValue,
                expected });

            preferred = null;
            fallback1 = null;
            defaultValue = 0;
            expected = 0;
            testData.Add(new object[] { preferred, fallback1, defaultValue,
                expected });

            return testData;
        }

        [Theory]
        [MemberData(nameof(CreateTestDetermineEffectivePositiveIntegerOptionnData))]
        public void TestDetermineEffectivePositiveIntegerOption(int? preferred, int? fallback1,
            int defaultValue, int expected)
        {
            var actual = ProtocolUtilsInternal.DetermineEffectivePositiveIntegerOption(preferred, fallback1,
                defaultValue);
            Assert.Equal(expected, actual);
        }

        public static List<object[]> CreateTestDetermineEffectivePositiveIntegerOptionnData()
        {
            var testData = new List<object[]>();

            int? preferred = null;
            int? fallback1 = 1;
            int defaultValue = 30;
            int expected = 1;
            testData.Add(new object[] { preferred, fallback1, defaultValue,
                expected });

            preferred = 5;
            fallback1 = 3;
            defaultValue = 11;
            expected = 5;
            testData.Add(new object[] { preferred, fallback1, defaultValue,
                expected });

            preferred = null;
            fallback1 = 3;
            defaultValue = -1;
            expected = 3;
            testData.Add(new object[] { preferred, fallback1, defaultValue,
                expected });

            preferred = null;
            fallback1 = null;
            defaultValue = 2;
            expected = 2;
            testData.Add(new object[] { preferred, fallback1, defaultValue,
                expected });

            preferred = null;
            fallback1 = null;
            defaultValue = -8;
            expected = -8;
            testData.Add(new object[] { preferred, fallback1, defaultValue,
                expected });

            preferred = null;
            fallback1 = null;
            defaultValue = 0;
            expected = 0;
            testData.Add(new object[] { preferred, fallback1, defaultValue,
                expected });

            return testData;
        }

        [Theory]
        [MemberData(nameof(CreateTestDetermineEffectiveOptionsData))]
        public void TestDetermineEffectiveOptions(IDictionary<string, object> preferred,
            IDictionary<string, object> fallback, IDictionary<string, object> expected)
        {
            var actual = ProtocolUtilsInternal.DetermineEffectiveOptions(preferred, fallback);
            Assert.Equal(expected, actual);
        }

        public static List<object[]> CreateTestDetermineEffectiveOptionsData()
        {
            var testData = new List<object[]>();

            IDictionary<string, object> preferred = null;
            IDictionary<string, object> fallback = null;
            var expected = new Dictionary<string, object>();
            testData.Add(new object[] { preferred, fallback, expected });

            preferred = new Dictionary<string, object>
            {
                { "a", 2 }, { "b", 3 }
            };
            fallback = null;
            expected = new Dictionary<string, object>
            {
                { "a", 2 }, { "b", 3 }
            };
            testData.Add(new object[] { preferred, fallback, expected });

            preferred = null;
            fallback = new Dictionary<string, object>
            {
                { "a", 2 }, { "b", 3 }
            };
            expected = new Dictionary<string, object>
            {
                { "a", 2 }, { "b", 3 }
            };
            testData.Add(new object[] { preferred, fallback, expected });

            preferred = new Dictionary<string, object>
            {
                { "a", 2 }, { "b", 3 }
            };
            fallback = new Dictionary<string, object>
            {
                { "c", 4 }, { "d", 3 }
            };
            expected = new Dictionary<string, object>
            {
                { "a", 2 }, { "b", 3 },
                { "c", 4 }, { "d", 3 }
            };
            testData.Add(new object[] { preferred, fallback, expected });

            preferred = new Dictionary<string, object>
            {
                { "a", 2 }, { "b", 3 }
            };
            fallback = new Dictionary<string, object>
            {
                { "a", 4 }, { "d", 3 }
            };
            expected = new Dictionary<string, object>
            {
                { "a", 2 }, { "b", 3 }, { "d", 3 }
            };
            testData.Add(new object[] { preferred, fallback, expected });

            preferred = new Dictionary<string, object>
            {
                { "a", 2 }
            };
            fallback = new Dictionary<string, object>
            {
                { "a", 4 }, { "d", 3 }
            };
            expected = new Dictionary<string, object>
            {
                { "a", 2 }, { "d", 3 }
            };
            testData.Add(new object[] { preferred, fallback, expected });

            return testData;
        }

        [Theory]
        [MemberData(nameof(CreateTestDetermineEffectiveBooleanOptionData))]
        public void TestDetermineEffectiveBooleanOption(bool? preferred,
            bool? fallback1, bool defaultValue, bool expected)
        {
            var actual = ProtocolUtilsInternal.DetermineEffectiveBooleanOption(
                preferred, fallback1, defaultValue);
            Assert.Equal(expected, actual);
        }

        public static List<object[]> CreateTestDetermineEffectiveBooleanOptionData()
        {
            var testData = new List<object[]>();

            bool? preferred = true;
            bool? fallback1 = null;
            bool defaultValue = true;
            bool expected = true;
            testData.Add(new object[] { preferred, fallback1, defaultValue,
                expected });

            preferred = false;
            fallback1 = true;
            defaultValue = true;
            expected = false;
            testData.Add(new object[] { preferred, fallback1, defaultValue,
                expected });

            preferred = null;
            fallback1 = false;
            defaultValue = true;
            expected = false;
            testData.Add(new object[] { preferred, fallback1, defaultValue,
                expected });

            preferred = null;
            fallback1 = true;
            defaultValue = false;
            expected = true;
            testData.Add(new object[] { preferred, fallback1, defaultValue,
                expected });

            preferred = null;
            fallback1 = true;
            defaultValue = true;
            expected = true;
            testData.Add(new object[] { preferred, fallback1, defaultValue,
                expected });

            preferred = null;
            fallback1 = null;
            defaultValue = true;
            expected = true;
            testData.Add(new object[] { preferred, fallback1, defaultValue,
                expected });

            preferred = null;
            fallback1 = null;
            defaultValue = false;
            expected = false;
            testData.Add(new object[] { preferred, fallback1, defaultValue,
                expected });

            preferred = true;
            fallback1 = true;
            defaultValue = false;
            expected = true;
            testData.Add(new object[] { preferred, fallback1, defaultValue,
                expected });

            return testData;
        }

        [Theory]
        [MemberData(nameof(CreateTestCreateEquivalentInMemoryResponseBodyData))]
        public async Task TestCreateEquivalentInMemoryResponseBody(int bufferSize,
            int bufferingLimit, IQuasiHttpBody responseBody,
            byte[] expectedResBodyBytes)
        {
            var expected = new DefaultQuasiHttpResponse
            {
                Body = new ContentLengthOverrideBody(new ByteBufferBody(expectedResBodyBytes)
                    { ContentType = responseBody.ContentType }, responseBody.ContentLength)
            };
            var actualResponseBody = await ProtocolUtilsInternal.CreateEquivalentInMemoryResponseBody(responseBody,
                bufferSize, bufferingLimit);
            var actual = new DefaultQuasiHttpResponse
            {
                Body = actualResponseBody
            };
            await ComparisonUtils.CompareResponses(bufferSize, expected, actual, expectedResBodyBytes);
        }

        public static List<object[]> CreateTestCreateEquivalentInMemoryResponseBodyData()
        {
            var testData = new List<object[]>();

            int bufferSize = 1;
            int bufferingLimit = 0;
            byte[] expectedResBodyBytes = new byte[0];
            IQuasiHttpBody responseBody = new ByteBufferBody(expectedResBodyBytes);
            testData.Add(new object[] { bufferSize, bufferingLimit, responseBody, expectedResBodyBytes });

            bufferSize = 1;
            bufferingLimit = 0;
            expectedResBodyBytes = new byte[0];
            responseBody = new StringBody(ByteUtils.BytesToString(expectedResBodyBytes, 0,
                expectedResBodyBytes.Length));
            testData.Add(new object[] { bufferSize, bufferingLimit, responseBody, expectedResBodyBytes });

            bufferSize = 2;
            expectedResBodyBytes = new byte[]{ (byte)'a', (byte)'b', (byte)'c', (byte)'d',
                (byte)'e', (byte)'f' };
            bufferingLimit = expectedResBodyBytes.Length;
            responseBody = new StringBody(ByteUtils.BytesToString(expectedResBodyBytes, 0,
                expectedResBodyBytes.Length))
            {
                ContentType = "text/plain"
            };
            testData.Add(new object[] { bufferSize, bufferingLimit, responseBody, expectedResBodyBytes });

            bufferSize = 10;
            expectedResBodyBytes = new byte[]{ (byte)'a', (byte)'b', (byte)'c', (byte)'d',
                (byte)'e', (byte)'f' };
            bufferingLimit = expectedResBodyBytes.Length;
            responseBody = new StringBody(ByteUtils.BytesToString(expectedResBodyBytes, 0,
                expectedResBodyBytes.Length));
            testData.Add(new object[] { bufferSize, bufferingLimit, responseBody, expectedResBodyBytes });

            bufferSize = 10;
            bufferingLimit = 8;
            expectedResBodyBytes = new byte[]{ (byte)'a', (byte)'b', (byte)'c', (byte)'d',
                (byte)'e', (byte)'f' };
            responseBody = new ByteBufferBody(expectedResBodyBytes)
            {
                ContentType = "application/octet-stream"
            };
            testData.Add(new object[] { bufferSize, bufferingLimit, responseBody, expectedResBodyBytes });

            // test that over abundance of data works fine.

            bufferSize = 100;
            bufferingLimit = 4;
            responseBody = new ByteBufferBody(new byte[]{ (byte)'a', (byte)'b', (byte)'c', (byte)'d',
                (byte)'e', (byte)'f' })
            {
                ContentType = "application/json"
            };
            responseBody = new ContentLengthOverrideBody(responseBody, 3);
            expectedResBodyBytes = new byte[]{ (byte)'a', (byte)'b', (byte)'c' };
            testData.Add(new object[] { bufferSize, bufferingLimit, responseBody, expectedResBodyBytes });

            return testData;
        }

        [Fact]
        public async Task TestCreateEquivalentInMemoryResponseBodyForErrors1()
        {
            int bufferSize = 1;
            int bufferingLimit = 3;
            var responseBody = new StringBody("xyz!");
            await Assert.ThrowsAnyAsync<Exception>(() =>
            {
                return ProtocolUtilsInternal.CreateEquivalentInMemoryResponseBody(responseBody,
                    bufferSize, bufferingLimit);
            });
        }

        [Fact]
        public async Task TestCreateEquivalentInMemoryResponseBodyForErrors2()
        {
            int bufferSize = 1;
            int bufferingLimit = 3;
            var responseBody = new ByteBufferBody(ByteUtils.StringToBytes("xyz!"));
            await Assert.ThrowsAsync<BodySizeLimitExceededException>(() =>
            {
                return ProtocolUtilsInternal.CreateEquivalentInMemoryResponseBody(responseBody,
                    bufferSize, bufferingLimit);
            });
        }

        [Fact]
        public async Task TestCreateEquivalentInMemoryResponseBodyForErrors3()
        {
            int bufferSize = 1;
            int bufferingLimit = 30;
            var responseBody = new ContentLengthOverrideBody(new StringBody("xyz!"), 5);
            await Assert.ThrowsAsync<ContentLengthNotSatisfiedException>(() =>
            {
                return ProtocolUtilsInternal.CreateEquivalentInMemoryResponseBody(responseBody,
                    bufferSize, bufferingLimit);
            });
        }
    }
}
