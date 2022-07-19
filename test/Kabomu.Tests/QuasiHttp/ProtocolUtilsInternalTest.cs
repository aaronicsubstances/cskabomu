using Kabomu.Concurrency;
using Kabomu.QuasiHttp;
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
        [MemberData(nameof(CreateTestDetermineEffectiveOverallReqRespTimeoutMillisData))]
        public void TestDetermineEffectiveOverallReqRespTimeoutMillis(int? preferred,
            int? fallback1, int defaultValue, int expected)
        {
            var actual = ProtocolUtilsInternal.DetermineEffectiveOverallReqRespTimeoutMillis(
                preferred, fallback1, defaultValue);
            Assert.Equal(expected, actual);
        }

        public static List<object[]> CreateTestDetermineEffectiveOverallReqRespTimeoutMillisData()
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
        [MemberData(nameof(CreateTestDetermineEffectiveMaxChunkSizeData))]
        public void TestDetermineEffectiveMaxChunkSize(int? preferred, int? fallback1,
            int defaultValue, int expected)
        {
            var actual = ProtocolUtilsInternal.DetermineEffectiveMaxChunkSize(preferred, fallback1,
                defaultValue);
            Assert.Equal(expected, actual);
        }

        public static List<object[]> CreateTestDetermineEffectiveMaxChunkSizeData()
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
        [MemberData(nameof(CreateTestDetermineEffectiveMutexApiData))]
        public async Task TestDetermineEffectiveMutexApi(IMutexApi preferred,
            IMutexApiFactory fallbackFactory, IMutexApi expected)
        {
            var actual = await ProtocolUtilsInternal.DetermineEffectiveMutexApi(preferred,
                fallbackFactory);
            Assert.Equal(expected, actual);
        }

        public static List<object[]> CreateTestDetermineEffectiveMutexApiData()
        {
            var testData = new List<object[]>();

            IMutexApi preffered = new LockBasedMutexApi();
            TestMutexApiFactory factory = null;
            IMutexApi expected = preffered;
            testData.Add(new object[] { preffered, factory, expected });

            preffered = null;
            factory = null;
            expected = null;
            testData.Add(new object[] { preffered, factory, expected });

            preffered = new LockBasedMutexApi();
            factory = null;
            expected = preffered;
            testData.Add(new object[] { preffered, factory, expected });

            preffered = new LockBasedMutexApi();
            factory = new TestMutexApiFactory(new LockBasedMutexApi());
            expected = preffered;
            testData.Add(new object[] { preffered, factory, expected });

            preffered = null;
            factory = new TestMutexApiFactory(new LockBasedMutexApi());
            expected = factory.SoleMutexApi;
            testData.Add(new object[] { preffered, factory, expected });

            return testData;
        }

        private class TestMutexApiFactory : IMutexApiFactory
        {
            public TestMutexApiFactory(IMutexApi soleMutexApi)
            {
                SoleMutexApi = soleMutexApi;
            }

            public IMutexApi SoleMutexApi { get; }

            public Task<IMutexApi> Create()
            {
                return Task.FromResult(SoleMutexApi);
            }
        }
    }
}
