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
        public void TestDetermineEffectiveOverallReqRespTimeoutMillis(IQuasiHttpSendOptions firstOptions,
            IQuasiHttpSendOptions fallbackOptions, int defaultValue, int expected)
        {
            var actual = ProtocolUtilsInternal.DetermineEffectiveOverallReqRespTimeoutMillis(
                firstOptions, fallbackOptions, defaultValue);
            Assert.Equal(expected, actual);
        }

        public static List<object[]> CreateTestDetermineEffectiveOverallReqRespTimeoutMillisData()
        {
            var testData = new List<object[]>();

            IQuasiHttpSendOptions firstOptions = new DefaultQuasiHttpSendOptions { OverallReqRespTimeoutMillis = 1 };
            IQuasiHttpSendOptions fallbackOptions = null;
            int defaultValue = 0;
            int expected = 1;
            testData.Add(new object[] { firstOptions, fallbackOptions, defaultValue,
                expected });

            firstOptions = new DefaultQuasiHttpSendOptions { OverallReqRespTimeoutMillis = 5 };
            fallbackOptions = new DefaultQuasiHttpSendOptions { OverallReqRespTimeoutMillis = 3 };
            defaultValue = 11;
            expected = 5;
            testData.Add(new object[] { firstOptions, fallbackOptions, defaultValue,
                expected });

            firstOptions = new DefaultQuasiHttpSendOptions { OverallReqRespTimeoutMillis = -15 };
            fallbackOptions = new DefaultQuasiHttpSendOptions { OverallReqRespTimeoutMillis = 3 };
            defaultValue = -1;
            expected = -15;
            testData.Add(new object[] { firstOptions, fallbackOptions, defaultValue,
                expected });

            firstOptions = new DefaultQuasiHttpSendOptions();
            fallbackOptions = new DefaultQuasiHttpSendOptions { OverallReqRespTimeoutMillis = 3 };
            defaultValue = -1;
            expected = 3;
            testData.Add(new object[] { firstOptions, fallbackOptions, defaultValue,
                expected });

            firstOptions = new DefaultQuasiHttpSendOptions();
            fallbackOptions = new DefaultQuasiHttpSendOptions { OverallReqRespTimeoutMillis = -3 };
            defaultValue = -1;
            expected = -3;
            testData.Add(new object[] { firstOptions, fallbackOptions, defaultValue,
                expected });

            firstOptions = new DefaultQuasiHttpSendOptions();
            fallbackOptions = new DefaultQuasiHttpSendOptions();
            defaultValue = 2;
            expected = 2;
            testData.Add(new object[] { firstOptions, fallbackOptions, defaultValue,
                expected });

            firstOptions = new DefaultQuasiHttpSendOptions();
            fallbackOptions = new DefaultQuasiHttpSendOptions();
            defaultValue = -8;
            expected = -8;
            testData.Add(new object[] { firstOptions, fallbackOptions, defaultValue,
                expected });

            firstOptions = null;
            fallbackOptions = null;
            defaultValue = 0;
            expected = 0;
            testData.Add(new object[] { firstOptions, fallbackOptions, defaultValue,
                expected });

            return testData;
        }

        [Theory]
        [MemberData(nameof(CreateTestDetermineEffectiveMaxChunkSizeData))]
        public void TestDetermineEffectiveMaxChunkSize(IQuasiHttpSendOptions firstOptions, IQuasiHttpSendOptions fallbackOptions,
            int secondFallback, int defaultValue, int expected)
        {
            var actual = ProtocolUtilsInternal.DetermineEffectiveMaxChunkSize(firstOptions, fallbackOptions,
                secondFallback, defaultValue);
            Assert.Equal(expected, actual);
        }

        public static List<object[]> CreateTestDetermineEffectiveMaxChunkSizeData()
        {
            var testData = new List<object[]>();

            IQuasiHttpSendOptions firstOptions = null;
            IQuasiHttpSendOptions fallbackOptions = null;
            int secondFallback = 1;
            int defaultValue = 0;
            int expected = 1;
            testData.Add(new object[] { firstOptions, fallbackOptions, secondFallback, defaultValue,
                expected });

            firstOptions = new DefaultQuasiHttpSendOptions { MaxChunkSize = 5 };
            fallbackOptions = new DefaultQuasiHttpSendOptions { MaxChunkSize = 3 };
            secondFallback = 0;
            defaultValue = 11;
            expected = 5;
            testData.Add(new object[] { firstOptions, fallbackOptions, secondFallback, defaultValue,
                expected });

            firstOptions = new DefaultQuasiHttpSendOptions { MaxChunkSize = 15 };
            fallbackOptions = new DefaultQuasiHttpSendOptions { MaxChunkSize = 3 };
            secondFallback = 10;
            defaultValue = -1;
            expected = 15;
            testData.Add(new object[] { firstOptions, fallbackOptions, secondFallback, defaultValue,
                expected });

            firstOptions = new DefaultQuasiHttpSendOptions();
            fallbackOptions = new DefaultQuasiHttpSendOptions { MaxChunkSize = 3 };
            secondFallback = 10;
            defaultValue = -1;
            expected = 3;
            testData.Add(new object[] { firstOptions, fallbackOptions, secondFallback, defaultValue,
                expected });

            firstOptions = new DefaultQuasiHttpSendOptions();
            fallbackOptions = new DefaultQuasiHttpSendOptions();
            secondFallback = -4;
            defaultValue = 2;
            expected = 2;
            testData.Add(new object[] { firstOptions, fallbackOptions, secondFallback, defaultValue,
                expected });

            firstOptions = new DefaultQuasiHttpSendOptions();
            fallbackOptions = new DefaultQuasiHttpSendOptions();
            secondFallback = -4;
            defaultValue = -8;
            expected = -8;
            testData.Add(new object[] { firstOptions, fallbackOptions, secondFallback, defaultValue,
                expected });

            firstOptions = null;
            fallbackOptions = null;
            secondFallback = -4;
            defaultValue = 0;
            expected = 0;
            testData.Add(new object[] { firstOptions, fallbackOptions, secondFallback, defaultValue,
                expected });

            return testData;
        }

        [Theory]
        [MemberData(nameof(CreateTestDetermineEffectiveRequestEnvironmentData))]
        public void TestDetermineEffectiveRequestEnvironment(IQuasiHttpSendOptions firstOptions,
            IQuasiHttpSendOptions fallbackOptions, IDictionary<string, object> expected)
        {
            var actual = ProtocolUtilsInternal.DetermineEffectiveRequestEnvironment(firstOptions, fallbackOptions);
            Assert.Equal(expected, actual);
        }

        public static List<object[]> CreateTestDetermineEffectiveRequestEnvironmentData()
        {
            var testData = new List<object[]>();

            IQuasiHttpSendOptions firstOptions = null;
            IQuasiHttpSendOptions fallbackOptions = null;
            var expected = new Dictionary<string, object>();
            testData.Add(new object[] { firstOptions, fallbackOptions, expected });

            firstOptions = new DefaultQuasiHttpSendOptions
            {
                RequestEnvironment = new Dictionary<string, object>
                {
                    { "a", 2 }, { "b", 3 }
                }
            };
            fallbackOptions = null;
            expected = new Dictionary<string, object>
            {
                { "a", 2 }, { "b", 3 }
            };
            testData.Add(new object[] { firstOptions, fallbackOptions, expected });

            firstOptions = null;
            fallbackOptions = new DefaultQuasiHttpSendOptions
            {
                RequestEnvironment = new Dictionary<string, object>
                {
                    { "a", 2 }, { "b", 3 }
                }
            };
            expected = new Dictionary<string, object>
            {
                { "a", 2 }, { "b", 3 }
            };
            testData.Add(new object[] { firstOptions, fallbackOptions, expected });

            firstOptions = new DefaultQuasiHttpSendOptions
            {
                RequestEnvironment = new Dictionary<string, object>
                {
                    { "a", 2 }, { "b", 3 }
                }
            };
            fallbackOptions = new DefaultQuasiHttpSendOptions
            {
                RequestEnvironment = new Dictionary<string, object>
                {
                    { "c", 4 }, { "d", 3 }
                }
            };
            expected = new Dictionary<string, object>
            {
                { "a", 2 }, { "b", 3 },
                { "c", 4 }, { "d", 3 }
            };
            testData.Add(new object[] { firstOptions, fallbackOptions, expected });

            firstOptions = new DefaultQuasiHttpSendOptions
            {
                RequestEnvironment = new Dictionary<string, object>
                {
                    { "a", 2 }, { "b", 3 }
                }
            };
            fallbackOptions = new DefaultQuasiHttpSendOptions
            {
                RequestEnvironment = new Dictionary<string, object>
                {
                    { "a", 4 }, { "d", 3 }
                }
            };
            expected = new Dictionary<string, object>
            {
                { "a", 2 }, { "b", 3 }, { "d", 3 }
            };
            testData.Add(new object[] { firstOptions, fallbackOptions, expected });

            firstOptions = new DefaultQuasiHttpSendOptions
            {
                RequestEnvironment = new Dictionary<string, object>
                {
                    { "a", 2 }
                }
            };
            fallbackOptions = new DefaultQuasiHttpSendOptions
            {
                RequestEnvironment = new Dictionary<string, object>
                {
                    { "a", 4 }, { "d", 3 }
                }
            };
            expected = new Dictionary<string, object>
            {
                { "a", 2 }, { "d", 3 }
            };
            testData.Add(new object[] { firstOptions, fallbackOptions, expected });

            firstOptions = new DefaultQuasiHttpSendOptions();
            fallbackOptions = new DefaultQuasiHttpSendOptions();
            expected = new Dictionary<string, object>();
            testData.Add(new object[] { firstOptions, fallbackOptions, expected });

            firstOptions = null;
            fallbackOptions = null;
            expected = new Dictionary<string, object>();
            testData.Add(new object[] { firstOptions, fallbackOptions, expected });

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
