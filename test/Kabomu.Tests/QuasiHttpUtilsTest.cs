using Kabomu.Abstractions;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests
{
    public class QuasiHttpUtilsTest
    {
        [Fact]
        public void TestClassConstants()
        {
            Assert.Equal("CONNECT", QuasiHttpUtils.MethodConnect);
            Assert.Equal("DELETE", QuasiHttpUtils.MethodDelete);
            Assert.Equal("GET", QuasiHttpUtils.MethodGet);
            Assert.Equal("HEAD", QuasiHttpUtils.MethodHead);
            Assert.Equal("OPTIONS", QuasiHttpUtils.MethodOptions);
            Assert.Equal("PATCH", QuasiHttpUtils.MethodPatch);
            Assert.Equal("POST", QuasiHttpUtils.MethodPost);
            Assert.Equal("PUT", QuasiHttpUtils.MethodPut);
            Assert.Equal("TRACE", QuasiHttpUtils.MethodTrace);

            Assert.Equal(200, QuasiHttpUtils.StatusCodeOk);
            Assert.Equal(500, QuasiHttpUtils.StatusCodeServerError);
            Assert.Equal(400, QuasiHttpUtils.StatusCodeClientErrorBadRequest);
            Assert.Equal(401, QuasiHttpUtils.StatusCodeClientErrorUnauthorized);
            Assert.Equal(403, QuasiHttpUtils.StatusCodeClientErrorForbidden);
            Assert.Equal(404, QuasiHttpUtils.StatusCodeClientErrorNotFound);
            Assert.Equal(405, QuasiHttpUtils.StatusCodeClientErrorMethodNotAllowed);
            Assert.Equal(413, QuasiHttpUtils.StatusCodeClientErrorPayloadTooLarge);
            Assert.Equal(414, QuasiHttpUtils.StatusCodeClientErrorURITooLong);
            Assert.Equal(415, QuasiHttpUtils.StatusCodeClientErrorUnsupportedMediaType);
            Assert.Equal(422, QuasiHttpUtils.StatusCodeClientErrorUnprocessableEntity);
            Assert.Equal(429, QuasiHttpUtils.StatusCodeClientErrorTooManyRequests);
        }

        [Fact]
        public void TestMergeProcessingOptions1()
        {
            IQuasiHttpProcessingOptions preferred = null;
            IQuasiHttpProcessingOptions fallback = null;
            var actual = QuasiHttpUtils.MergeProcessingOptions(
                preferred, fallback);
            Assert.Null(actual);
        }

        [Fact]
        public void TestMergeProcessingOptions2()
        {
            var preferred = new DefaultQuasiHttpProcessingOptions();
            IQuasiHttpProcessingOptions fallback = null;
            var actual = QuasiHttpUtils.MergeProcessingOptions(
                preferred, fallback);
            Assert.Same(actual, preferred);
        }

        [Fact]
        public void TestMergeProcessingOptions3()
        {
            IQuasiHttpProcessingOptions preferred = null;
            var fallback = new DefaultQuasiHttpProcessingOptions();
            var actual = QuasiHttpUtils.MergeProcessingOptions(
                preferred, fallback);
            Assert.Same(actual, fallback);
        }


        [Fact]
        public void TestMergeProcessingOptions4()
        {
            var preferred = new DefaultQuasiHttpProcessingOptions();
            var fallback = new DefaultQuasiHttpProcessingOptions();
            var actual = QuasiHttpUtils.MergeProcessingOptions(
                preferred, fallback);
            var expected = new DefaultQuasiHttpProcessingOptions
            {
                ExtraConnectivityParams = new Dictionary<string, object>()
            };
            ComparisonUtils.CompareProcessingOptions(expected, actual);
        }


        [Fact]
        public void TestMergeProcessingOptions5()
        {
            var preferred = new DefaultQuasiHttpProcessingOptions
            {
                ExtraConnectivityParams = new Dictionary<string, object>
                {
                    { "scheme", "tht" }
                },
                MaxHeadersSize = 10,
                MaxResponseBodySize = -1,
                TimeoutMillis = 0
            };
            var fallback = new DefaultQuasiHttpProcessingOptions
            {
                ExtraConnectivityParams = new Dictionary<string, object>
                {
                    { "scheme", "htt" },
                    { "two", 2 }
                },
                MaxHeadersSize = 30,
                MaxResponseBodySize = 40,
                TimeoutMillis = -1
            };
            var actual = QuasiHttpUtils.MergeProcessingOptions(
                preferred, fallback);
            var expected = new DefaultQuasiHttpProcessingOptions
            {
                ExtraConnectivityParams = new Dictionary<string, object>
                {
                    { "scheme", "tht" },
                    { "two", 2 }
                },
                MaxHeadersSize = 10,
                MaxResponseBodySize = -1,
                TimeoutMillis = -1
            };
            ComparisonUtils.CompareProcessingOptions(expected, actual);
        }

        [Theory]
        [MemberData(nameof(CreateTestDetermineEffectiveNonZeroIntegerOptionData))]
        public void TestDetermineEffectiveNonZeroIntegerOption(int? preferred,
            int? fallback1, int defaultValue, int expected)
        {
            var actual = QuasiHttpUtils.DetermineEffectiveNonZeroIntegerOption(
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
            var actual = QuasiHttpUtils.DetermineEffectivePositiveIntegerOption(preferred, fallback1,
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
            var actual = QuasiHttpUtils.DetermineEffectiveOptions(preferred, fallback);
            Assert.Equal(expected, actual);
        }

        public static List<object[]> CreateTestDetermineEffectiveOptionsData()
        {
            var testData = new List<object[]>();

            IDictionary<string, object> preferred = null;
            IDictionary<string, object> fallback = null;
            var expected = new Dictionary<string, object>();
            testData.Add(new object[] { preferred, fallback, expected });

            preferred = new Dictionary<string, object>();
            fallback = new Dictionary<string, object>();
            expected = new Dictionary<string, object>();
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

        [Fact]
        public void TestCreateCancellableTimeoutTask1()
        {
            var actual = QuasiHttpUtils.CreateCancellableTimeoutTask(0);
            Assert.Null(actual);
        }

        [Fact]
        public void TestCreateCancellableTimeoutTask2()
        {
            var actual = QuasiHttpUtils.CreateCancellableTimeoutTask(-3);
            Assert.Null(actual);
        }

        [Fact]
        public async Task TestCreateCancellableTimeoutTask3()
        {
            var p = QuasiHttpUtils.CreateCancellableTimeoutTask(50);
            Assert.NotNull(p.Task);
            var result = await p.Task;
            Assert.True(result);
            p.Cancel();
            result = await p.Task;
            Assert.True(result);
        }

        [Fact]
        public async Task TestCreateCancellableTimeoutTask4()
        {
            var p = QuasiHttpUtils.CreateCancellableTimeoutTask(500);
            Assert.NotNull(p.Task);
            await Task.Delay(100);
            p.Cancel();
            var result = await p.Task;
            Assert.False(result);
            p.Cancel();
            result = await p.Task;
            Assert.False(result);
        }
    }
}
