using Kabomu.Abstractions;
using Kabomu.Exceptions;
using Kabomu.Impl;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Xunit;

namespace Kabomu.Tests
{
    public class QuasiHttpProtocolUtilsTest
    {
        [Fact]
        public void TestClassConstants()
        {
            Assert.Equal("CONNECT", QuasiHttpProtocolUtils.MethodConnect);
            Assert.Equal("DELETE", QuasiHttpProtocolUtils.MethodDelete);
            Assert.Equal("GET", QuasiHttpProtocolUtils.MethodGet);
            Assert.Equal("HEAD", QuasiHttpProtocolUtils.MethodHead);
            Assert.Equal("OPTIONS", QuasiHttpProtocolUtils.MethodOptions);
            Assert.Equal("PATCH", QuasiHttpProtocolUtils.MethodPatch);
            Assert.Equal("POST", QuasiHttpProtocolUtils.MethodPost);
            Assert.Equal("PUT", QuasiHttpProtocolUtils.MethodPut);
            Assert.Equal("TRACE", QuasiHttpProtocolUtils.MethodTrace);

            Assert.Equal(200, QuasiHttpProtocolUtils.StatusCodeOk);
            Assert.Equal(500, QuasiHttpProtocolUtils.StatusCodeServerError);
            Assert.Equal(400, QuasiHttpProtocolUtils.StatusCodeClientErrorBadRequest);
            Assert.Equal(401, QuasiHttpProtocolUtils.StatusCodeClientErrorUnauthorized);
            Assert.Equal(403, QuasiHttpProtocolUtils.StatusCodeClientErrorForbidden);
            Assert.Equal(404, QuasiHttpProtocolUtils.StatusCodeClientErrorNotFound);
            Assert.Equal(405, QuasiHttpProtocolUtils.StatusCodeClientErrorMethodNotAllowed);
            Assert.Equal(413, QuasiHttpProtocolUtils.StatusCodeClientErrorPayloadTooLarge);
            Assert.Equal(414, QuasiHttpProtocolUtils.StatusCodeClientErrorURITooLong);
            Assert.Equal(415, QuasiHttpProtocolUtils.StatusCodeClientErrorUnsupportedMediaType);
            Assert.Equal(422, QuasiHttpProtocolUtils.StatusCodeClientErrorUnprocessableEntity);
            Assert.Equal(429, QuasiHttpProtocolUtils.StatusCodeClientErrorTooManyRequests);
        }

        [Fact]
        public void TestMergeProcessingOptions1()
        {
            IQuasiHttpProcessingOptions preferred = null;
            IQuasiHttpProcessingOptions fallback = null;
            var actual = QuasiHttpProtocolUtils.MergeProcessingOptions(
                preferred, fallback);
            Assert.Null(actual);
        }


        [Fact]
        public void TestMergeProcessingOptions2()
        {
            var preferred = new DefaultQuasiHttpProcessingOptions
            {
                ExtraConnectivityParams = new Dictionary<string, object>
                {
                    { "scheme", "tht" }
                },
                MaxHeadersSize = 10,
                ResponseBufferingEnabled = false,
                ResponseBodyBufferingSizeLimit = -1,
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
                ResponseBufferingEnabled = true,
                ResponseBodyBufferingSizeLimit = 40,
                TimeoutMillis = -1
            };
            var actual = QuasiHttpProtocolUtils.MergeProcessingOptions(
                preferred, fallback);
            var expected = new DefaultQuasiHttpProcessingOptions
            {
                ExtraConnectivityParams = new Dictionary<string, object>
                {
                    { "scheme", "tht" },
                    { "two", 2 }
                },
                MaxHeadersSize = 10,
                ResponseBufferingEnabled = false,
                ResponseBodyBufferingSizeLimit = 40,
                TimeoutMillis = -1
            };
            ComparisonUtils.CompareProcessingOptions(expected, actual);
        }

        [Theory]
        [MemberData(nameof(CreateTestDetermineEffectiveNonZeroIntegerOptionData))]
        public void TestDetermineEffectiveNonZeroIntegerOption(int? preferred,
            int? fallback1, int defaultValue, int expected)
        {
            var actual = QuasiHttpProtocolUtils.DetermineEffectiveNonZeroIntegerOption(
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
            var actual = QuasiHttpProtocolUtils.DetermineEffectivePositiveIntegerOption(preferred, fallback1,
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
            var actual = QuasiHttpProtocolUtils.DetermineEffectiveOptions(preferred, fallback);
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

        [Theory]
        [MemberData(nameof(CreateTestDetermineEffectiveBooleanOptionData))]
        public void TestDetermineEffectiveBooleanOption(bool? preferred,
            bool? fallback1, bool defaultValue, bool expected)
        {
            var actual = QuasiHttpProtocolUtils.DetermineEffectiveBooleanOption(
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

            preferred = true;
            fallback1 = true;
            defaultValue = true;
            expected = true;
            testData.Add(new object[] { preferred, fallback1, defaultValue,
                expected });

            preferred = false;
            fallback1 = false;
            defaultValue = false;
            expected = false;
            testData.Add(new object[] { preferred, fallback1, defaultValue,
                expected });

            return testData;
        }

        [Theory]
        [MemberData(nameof(CreateTestGetEnvVarAsBooleanData))]
        public void TestGetEnvVarAsBoolean(IDictionary<string, object> environment,
            string key, bool? expected)
        {
            var actual = QuasiHttpProtocolUtils.GetEnvVarAsBoolean(environment,
                key);
            Assert.Equal(expected, actual);
        }

        public static List<object[]> CreateTestGetEnvVarAsBooleanData()
        {
            var testData = new List<object[]>();

            var environment = new Dictionary<string, object>
            {
                { "d", "de" },
                { "2", false }
            };
            string key = "2";
            testData.Add(new object[] { environment, key, false });

            environment = null;
            key = "k1";
            testData.Add(new object[] { environment, key, null });

            environment = new Dictionary<string, object>
            {
                { "d2", "TRUE" }, { "e", "ghana" }
            };
            key = "f";
            testData.Add(new object[] { environment, key, null });

            environment = new Dictionary<string, object>
            {
                { "ty2", "TRUE" }, { "c", new object() }
            };
            key = "ty2";
            testData.Add(new object[] { environment, key, true });

            environment = new Dictionary<string, object>
            {
                { "d2", true }, { "e", "ghana" }
            };
            key = "d2";
            testData.Add(new object[] { environment, key, true });

            environment = new Dictionary<string, object>
            {
                { "d", "TRue" }, { "e", "ghana" }
            };
            key = "d";
            testData.Add(new object[] { environment, key, true });

            environment = new Dictionary<string, object>
            {
                { "d", "FALSE" }, { "e", "ghana" }
            };
            key = "d";
            testData.Add(new object[] { environment, key, false });

            environment = new Dictionary<string, object>
            {
                { "d", "45" }, { "e", "ghana" }, { "ert", "False" }
            };
            key = "ert";
            testData.Add(new object[] { environment, key, false });

            return testData;
        }

        [Theory]
        [MemberData(nameof(CreateTestGetEnvVarAsBooleanForErrorsData))]
        public void TestGetEnvVarAsBooleanForErrors(IDictionary<string, object> environment,
            string key)
        {
            Assert.ThrowsAny<Exception>(() =>
                QuasiHttpProtocolUtils.GetEnvVarAsBoolean(environment, key));
        }

        public static List<object[]> CreateTestGetEnvVarAsBooleanForErrorsData()
        {
            var testData = new List<object[]>();

            var environment = new Dictionary<string, object>
            {
                { "d", "de" },
                { "2", false }
            };
            string key = "d";
            testData.Add(new object[] { environment, key });

            environment = new Dictionary<string, object>
            {
                { "c", "" }
            };
            key = "c";
            testData.Add(new object[] { environment, key });

            environment = new Dictionary<string, object>
            {
                { "d2", "TRUE" }, { "e", new List<string>() }
            };
            key = "e";
            testData.Add(new object[] { environment, key });

            environment = new Dictionary<string, object>
            {
                { "k1", 1 }
            };
            key = "k1";
            testData.Add(new object[] { environment, key });

            environment = new Dictionary<string, object>
            {
                { "k1", 0 }
            };
            key = "k1";
            testData.Add(new object[] { environment, key });

            return testData;
        }

        [Fact]
        public async Task TestEncodeBodyToTransport1()
        {
            var contentLength = -1;
            var stream = new MemoryStream(
                MiscUtils.StringToBytes("data bits and bytes"));
            var expected = "01,0000000019" +
                "data bits and bytes" +
                "01,0000000000";
            var environment = new Dictionary<string, object>();

            var actualStream = QuasiHttpProtocolUtils.EncodeBodyToTransport(
                contentLength, stream, environment);

            Assert.NotSame(stream, actualStream);
            var actual = MiscUtils.BytesToString(
                (await MiscUtils.ReadAllBytes(actualStream)).ToArray());
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestEncodeBodyToTransport2()
        {
            var contentLength = -1;
            var stream = new MemoryStream(
                MiscUtils.StringToBytes("data bits and bytes"));
            var environment = new Dictionary<string, object>
            {
                {
                    QuasiHttpProtocolUtils.EnvKeySkipRawBodyEncoding,
                    true
                }
            };

            var actualStream = QuasiHttpProtocolUtils.EncodeBodyToTransport(
                contentLength, stream, environment);

            Assert.Same(stream, actualStream);
        }

        [Fact]
        public void TestEncodeBodyToTransport3()
        {
            var contentLength = 3;
            var stream = new MemoryStream(new byte[] { 1, 3, 2 });
            IDictionary<string, object> environment = null;

            var actualStream = QuasiHttpProtocolUtils.EncodeBodyToTransport(
                contentLength, stream, environment);

            Assert.Same(stream, actualStream);
        }

        /// <summary>
        /// Assert that zero content length causes body not to be processed.
        /// </summary>
        [Fact]
        public void TestEncodeBodyToTransport4()
        {
            var contentLength = 0;
            var stream = new MemoryStream(new byte[] { 1, 3, 2 });
            var environment = new Dictionary<string, object>
            {
                {
                    QuasiHttpProtocolUtils.EnvKeySkipRawBodyEncoding,
                    false
                }
            };

            var actualStream = QuasiHttpProtocolUtils.EncodeBodyToTransport(
                contentLength, stream, environment);

            Assert.Null(actualStream);
        }

        /// <summary>
        /// Assert that positive content length is not enforced
        /// </summary>
        [Fact]
        public void TestEncodeBodyToTransport5()
        {
            var contentLength = 13;
            var stream = new MemoryStream(new byte[] { 1, 3, 2 });
            IDictionary<string, object> environment = null;

            var actualStream = QuasiHttpProtocolUtils.EncodeBodyToTransport(
                contentLength, stream, environment);

            Assert.Same(stream, actualStream);
        }

        /// <summary>
        /// Assert that no error occurs with null body due to zero
        /// content length.
        /// </summary>
        [Fact]
        public void TestEncodeBodyToTransport6()
        {
            var contentLength = 0;
            Stream stream = null;
            IDictionary<string, object> environment = null;

            var actualStream = QuasiHttpProtocolUtils.EncodeBodyToTransport(
                contentLength, stream, environment);

            Assert.Null(actualStream);
        }

        [Fact]
        public async Task TestEncodeBodyToTransport7()
        {
            static async IAsyncEnumerable<byte[]> Generate()
            {
                yield return MiscUtils.StringToBytes("data bits and bytes");
                yield return MiscUtils.StringToBytes(",data bits and bytes");
            }
            var contentLength = -1;
            var stream = new AsyncEnumerableBackedStream(Generate());
            var expected = "01,0000000019" +
                "data bits and bytes" +
                "01,0000000020" +
                ",data bits and bytes" +
                "01,0000000000";
            var environment = new Dictionary<string, object>();

            var actualStream = QuasiHttpProtocolUtils.EncodeBodyToTransport(
                contentLength, stream, environment);

            Assert.NotSame(stream, actualStream);
            var actual = MiscUtils.BytesToString(
                (await MiscUtils.ReadAllBytes(actualStream)).ToArray());
            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task TestEncodeBodyToTransport8()
        {
            var contentLength = -1;
            var stream = new MemoryStream();
            var expected = "01,0000000000";
            var environment = new Dictionary<string, object>();

            var actualStream = QuasiHttpProtocolUtils.EncodeBodyToTransport(
                contentLength, stream, environment);

            Assert.NotSame(stream, actualStream);
            var actual = MiscUtils.BytesToString(
                (await MiscUtils.ReadAllBytes(actualStream)).ToArray());
            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task TestDecodeBodyFromTransport1()
        {
            // arrange
            var expected = "ice";
            long contentLength = 3;
            var stream = ComparisonUtils.CreateRandomizedChunkStream(
                MiscUtils.StringToBytes(expected));
            var environment = new Dictionary<string, object>();

            // act
            var actualStream = QuasiHttpProtocolUtils.DecodeBodyFromTransport(
                contentLength, stream, environment);

            // assert
            Assert.NotSame(stream, actualStream);
            var actual = MiscUtils.BytesToString(
                (await MiscUtils.ReadAllBytes(actualStream)).ToArray());
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestDecodeBodyFromTransport2()
        {
            // arrange
            var expected = "ice";
            long contentLength = 35;
            var stream = new MemoryStream(MiscUtils.StringToBytes(expected));
            var environment = new Dictionary<string, object>
            {
                {
                    QuasiHttpProtocolUtils.EnvKeySkipRawBodyDecoding,
                    true
                }
            };

            // act
            var actualStream = QuasiHttpProtocolUtils.DecodeBodyFromTransport(
                contentLength, stream, environment);

            // assert
            Assert.Same(stream, actualStream);
        }

        [Fact]
        public async Task TestDecodeBodyFromTransport3()
        {
            var contentLength = -1;
            var srcData = "01,0000000019" +
                "data bits and bytes" +
                "01,0000000000";
            var stream = ComparisonUtils.CreateRandomizedChunkStream(
                MiscUtils.StringToBytes(srcData));
            var environment = new Dictionary<string, object>();
            var expected = "data bits and bytes";

            var actualStream = QuasiHttpProtocolUtils.DecodeBodyFromTransport(
                contentLength, stream, environment);

            Assert.NotSame(stream, actualStream);
            var actual = MiscUtils.BytesToString(
                (await MiscUtils.ReadAllBytes(actualStream)).ToArray());
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestDecodeBodyFromTransport4()
        {
            var contentLength = -1;
            var srcData = "01,0000000019" +
                "data bits and bytes" +
                "01,0000000000";
            var stream = new MemoryStream(
                MiscUtils.StringToBytes(srcData));
            var environment = new Dictionary<string, object>
            {
                {
                    QuasiHttpProtocolUtils.EnvKeySkipRawBodyDecoding,
                    true
                }
            };

            var actualStream = QuasiHttpProtocolUtils.DecodeBodyFromTransport(
                contentLength, stream, environment);

            Assert.Same(stream, actualStream);
        }

        /// <summary>
        /// Test that zero content length returns null.
        /// </summary>
        [Fact]
        public void TestDecodeBodyFromTransport5()
        {
            var contentLength = 0;
            var stream = new MemoryStream(
                MiscUtils.StringToBytes("abefioo"));
            var environment = new Dictionary<string, object>
            {
                {
                    QuasiHttpProtocolUtils.EnvKeySkipRawBodyDecoding,
                    true
                }
            };

            var actualStream = QuasiHttpProtocolUtils.DecodeBodyFromTransport(
                contentLength, stream, environment);

            Assert.Null(actualStream);
        }

        [Fact]
        public async Task TestDecodeBodyFromTransport6()
        {
            // arrange
            long contentLength = 4;
            var stream = new MemoryStream(MiscUtils.StringToBytes("abefioo"));
            var environment = new Dictionary<string, object>
            {
                {
                    QuasiHttpProtocolUtils.EnvKeySkipRawBodyDecoding,
                    false
                }
            };
            var expected = "abef";

            // act
            var actualStream = QuasiHttpProtocolUtils.DecodeBodyFromTransport(
                contentLength, stream, environment);

            // assert
            Assert.NotSame(stream, actualStream);
            var actual = MiscUtils.BytesToString(
                (await MiscUtils.ReadAllBytes(actualStream)).ToArray());
            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task TestDecodeBodyFromTransport7()
        {
            var contentLength = -1;
            var srcData = "01,0000000019" +
                "data bits and bytes" +
                "01,0000000020" +
                ",data bits and bytes" +
                "01,0000000000";
            var stream = ComparisonUtils.CreateRandomizedChunkStream(
                MiscUtils.StringToBytes(srcData));
            var environment = new Dictionary<string, object>();
            var expected = "data bits and bytes,data bits and bytes";

            var actualStream = QuasiHttpProtocolUtils.DecodeBodyFromTransport(
                contentLength, stream, environment);

            Assert.NotSame(stream, actualStream);
            var actual = MiscUtils.BytesToString(
                (await MiscUtils.ReadAllBytes(actualStream)).ToArray());
            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task TestDecodeBodyFromTransportForErrors1()
        {
            // arrange
            long contentLength = -1;
            var stream = new MemoryStream(MiscUtils.StringToBytes("abefioo"));
            Dictionary<string, object> environment = null;

            // act
            var actualStream = QuasiHttpProtocolUtils.DecodeBodyFromTransport(
                contentLength, stream, environment);

            // assert
            Assert.NotSame(stream, actualStream);

            // act
            await Assert.ThrowsAsync<ChunkDecodingException>(() =>
            {
                return MiscUtils.ReadAllBytes(actualStream);
            });
        }

        [Fact]
        public async Task TestDecodeBodyFromTransportForErrors2()
        {
            // arrange
            long contentLength = 20;
            var stream = new MemoryStream(MiscUtils.StringToBytes("abe"));
            var environment = new Dictionary<string, object>
            {
                {
                    QuasiHttpProtocolUtils.EnvKeySkipRawBodyDecoding,
                    false
                }
            };

            // act
            var actualStream = QuasiHttpProtocolUtils.DecodeBodyFromTransport(
                contentLength, stream, environment);

            // assert
            Assert.NotSame(stream, actualStream);

            // act
            var actualEx = await Assert.ThrowsAsync<CustomIOException>(() =>
            {
                return MiscUtils.ReadAllBytes(actualStream);
            });
            Assert.Contains($"length of {contentLength}", actualEx.Message);
        }
    }
}
