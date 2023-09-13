using Kabomu.Abstractions;
using Kabomu.Exceptions;
using Kabomu.ProtocolImpl;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Xunit;

namespace Kabomu.Tests.ProtocolImpl
{
    public class ProtocolUtilsInternalTest
    {
        [Theory]
        [MemberData(nameof(CreateTestGetEnvVarAsBooleanData))]
        public void TestGetEnvVarAsBoolean(IDictionary<string, object> environment,
            string key, bool? expected)
        {
            var actual = ProtocolUtilsInternal.GetEnvVarAsBoolean(environment,
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
                ProtocolUtilsInternal.GetEnvVarAsBoolean(environment, key));
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
        public async Task TestWrapTimeoutTask1()
        {
            await ProtocolUtilsInternal.WrapTimeoutTask(null, "");
        }

        [Fact]
        public async Task TestWrapTimeoutTask2()
        {
            var task = Task.FromResult(false);
            await ProtocolUtilsInternal.WrapTimeoutTask(task, "");
        }

        [Fact]
        public async Task TestWrapTimeoutTask3()
        {
            var task = Task.FromResult(true);
            var actualEx = await Assert.ThrowsAsync<QuasiHttpException>(() =>
            {
                return ProtocolUtilsInternal.WrapTimeoutTask(task, "te");
            });
            Assert.Equal("te", actualEx.Message);
            Assert.Equal(QuasiHttpException.ReasonCodeTimeout, actualEx.ReasonCode);
        }

        [Fact]
        public async Task TestWrapTimeoutTask4()
        {
            var task = Task.FromResult(true);
            var actualEx = await Assert.ThrowsAsync<QuasiHttpException>(() =>
            {
                return ProtocolUtilsInternal.WrapTimeoutTask(task, "recv");
            });
            Assert.Equal("recv", actualEx.Message);
            Assert.Equal(QuasiHttpException.ReasonCodeTimeout, actualEx.ReasonCode);
        }

        [Fact]
        public async Task TestWrapTimeoutTask5()
        {
            var task = Task.FromException<bool>(new ArgumentException("th"));
            var actualEx = await Assert.ThrowsAsync<ArgumentException>(() =>
            {
                return ProtocolUtilsInternal.WrapTimeoutTask(task, "te");
            });
            Assert.Equal("th", actualEx.Message);
        }

        [Fact]
        public async Task TestWrapTimeoutTask6()
        {
            var task = Task.FromException<bool>(
                new InvalidOperationException("2gh"));
            var actualEx = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            {
                return ProtocolUtilsInternal.WrapTimeoutTask(task, "tfe");
            });
            Assert.Equal("2gh", actualEx.Message);
        }

        /*[Fact]
        public async Task TestEncodeBodyToTransport1()
        {
            var isResponse = false;
            var contentLength = -1;
            var stream = new MemoryStream(
                MiscUtils.StringToBytes("data bits and bytes"));
            var expected = "01,0000000019" +
                "data bits and bytes" +
                "01,0000000000";
            var environment = new Dictionary<string, object>();

            var actualStream = ProtocolUtilsInternal.EncodeBodyToTransport(
                isResponse, contentLength, stream);

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
                    QuasiHttpCodec.EnvKeySkipRawBodyEncoding,
                    true
                }
            };

            var actualStream = ProtocolUtilsInternal.EncodeBodyToTransport(
                contentLength, stream, environment);

            Assert.Same(stream, actualStream);
        }

        [Fact]
        public void TestEncodeBodyToTransport3()
        {
            var contentLength = 3;
            var stream = new MemoryStream(new byte[] { 1, 3, 2 });
            IDictionary<string, object> environment = null;

            var actualStream = ProtocolUtilsInternal.EncodeBodyToTransport(
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
                    QuasiHttpCodec.EnvKeySkipRawBodyEncoding,
                    false
                }
            };

            var actualStream = ProtocolUtilsInternal.EncodeBodyToTransport(
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

            var actualStream = ProtocolUtilsInternal.EncodeBodyToTransport(
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
            bool isResponse = true;

            var actualStream = ProtocolUtilsInternal.EncodeBodyToTransport(
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

            var actualStream = ProtocolUtilsInternal.EncodeBodyToTransport(
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

            var actualStream = ProtocolUtilsInternal.EncodeBodyToTransport(
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
            var actualStream = ProtocolUtilsInternal.DecodeBodyFromTransport(
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
                    QuasiHttpCodec.EnvKeySkipRawBodyDecoding,
                    true
                }
            };

            // act
            var actualStream = ProtocolUtilsInternal.DecodeBodyFromTransport(
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

            var actualStream = ProtocolUtilsInternal.DecodeBodyFromTransport(
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
                    QuasiHttpCodec.EnvKeySkipRawBodyDecoding,
                    true
                }
            };

            var actualStream = ProtocolUtilsInternal.DecodeBodyFromTransport(
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
                    QuasiHttpCodec.EnvKeySkipRawBodyDecoding,
                    true
                }
            };

            var actualStream = ProtocolUtilsInternal.DecodeBodyFromTransport(
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
                    QuasiHttpCodec.EnvKeySkipRawBodyDecoding,
                    false
                }
            };
            var expected = "abef";

            // act
            var actualStream = ProtocolUtilsInternal.DecodeBodyFromTransport(
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

            var actualStream = ProtocolUtilsInternal.DecodeBodyFromTransport(
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
            var actualStream = ProtocolUtilsInternal.DecodeBodyFromTransport(
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
                    QuasiHttpCodec.EnvKeySkipRawBodyDecoding,
                    false
                }
            };

            // act
            var actualStream = ProtocolUtilsInternal.DecodeBodyFromTransport(
                contentLength, stream, environment);

            // assert
            Assert.NotSame(stream, actualStream);

            // act
            var actualEx = await Assert.ThrowsAsync<CustomIOException>(() =>
            {
                return MiscUtils.ReadAllBytes(actualStream);
            });
            Assert.Contains($"length of {contentLength}", actualEx.Message);
        }*/

        [Fact]
        public async Task TestCompleteWorkTask1()
        {
            var expected = new DefaultQuasiHttpResponse();
            Task<IQuasiHttpResponse> workTask = Task.FromResult(
                expected as IQuasiHttpResponse);
            Task<IQuasiHttpResponse> timeoutTask = null;
            Task<IQuasiHttpResponse> cancellationTask = null;
            var actual = await ProtocolUtilsInternal.CompleteWorkTask(
                workTask, timeoutTask, cancellationTask);
            Assert.Same(expected, actual);
        }

        [Fact]
        public async Task TestCompleteWorkTask2()
        {
            Task workTask = Task.CompletedTask;
            Task<IQuasiHttpResponse> timeoutTask = null;
            Task<IQuasiHttpResponse> cancellationTask = null;
            await ProtocolUtilsInternal.CompleteWorkTask(
                workTask, timeoutTask, cancellationTask);
        }

        [Fact]
        public async Task TestCompleteWorkTask3()
        {
            var expected = new DefaultQuasiHttpResponse();
            Task<IQuasiHttpResponse> workTask = Task.FromResult(
                expected as IQuasiHttpResponse);
            Task<IQuasiHttpResponse> timeoutTask = null;
            Task<IQuasiHttpResponse> cancellationTask = Task.Delay(
                TimeSpan.FromSeconds(1)).ContinueWith<IQuasiHttpResponse>(_ =>
                {
                    return null;
                });
            var actual = await ProtocolUtilsInternal.CompleteWorkTask(
                workTask, timeoutTask, cancellationTask);
            Assert.Same(expected, actual);
        }

        [Fact]
        public async Task TestCompleteWorkTask4()
        {
            Task<IQuasiHttpResponse> workTask = Task.Delay(
                TimeSpan.FromSeconds(1)).ContinueWith<IQuasiHttpResponse>(_ =>
                {
                    return null;
                });
            Task<IQuasiHttpResponse> timeoutTask = null;
            Task<IQuasiHttpResponse> cancellationTask = Task.FromResult<IQuasiHttpResponse>(
                new DefaultQuasiHttpResponse());
            var actual = await ProtocolUtilsInternal.CompleteWorkTask(
                workTask, timeoutTask, cancellationTask);
            Assert.Null(actual);
        }

        [Fact]
        public async Task TestCompleteWorkTask5()
        {
            DefaultQuasiHttpResponse expected = new DefaultQuasiHttpResponse(),
                instance2 = new DefaultQuasiHttpResponse(),
                instance3 = new DefaultQuasiHttpResponse();
            Task<IQuasiHttpResponse> workTask = Task.Delay(
                TimeSpan.FromSeconds(0.75)).ContinueWith<IQuasiHttpResponse>(_ =>
                {
                    return expected;
                });
            Task<IQuasiHttpResponse> timeoutTask = Task.Delay(
                TimeSpan.FromSeconds(0.5)).ContinueWith<IQuasiHttpResponse>(_ =>
                {
                    return instance2;
                });
            Task<IQuasiHttpResponse> cancellationTask = Task.Delay(
                TimeSpan.FromSeconds(1)).ContinueWith<IQuasiHttpResponse>(_ =>
                {
                    return instance3;
                });
            var actual = await ProtocolUtilsInternal.CompleteWorkTask(
                workTask, timeoutTask, cancellationTask);
            Assert.Same(expected, actual);
        }

        [Fact]
        public async Task TestCompleteWorkTask6()
        {
            Task workTask = Task.Delay(TimeSpan.FromSeconds(2));
            Task timeoutTask = Task.Delay(
                TimeSpan.FromSeconds(0.5)).ContinueWith<IQuasiHttpResponse>(_ =>
                {
                    throw new QuasiHttpException("error1");
                });
            Task cancellationTask = Task.Delay(TimeSpan.FromSeconds(1));
            var actualEx = await Assert.ThrowsAsync<QuasiHttpException>(() =>
            {
                return ProtocolUtilsInternal.CompleteWorkTask(
                    workTask, timeoutTask, cancellationTask);
            });
            Assert.Equal("error1", actualEx.Message);
        }

        [Fact]
        public async Task TestCompleteWorkTask7()
        {
            Task<IQuasiHttpResponse> workTask = Task.Delay(
                TimeSpan.FromSeconds(0.5)).ContinueWith<IQuasiHttpResponse>(_ =>
                {
                    throw new QuasiHttpException("error1");
                });
            Task<IQuasiHttpResponse> timeoutTask = Task.Delay(
                TimeSpan.FromSeconds(2)).ContinueWith<IQuasiHttpResponse>(_ =>
                {
                    throw new QuasiHttpException("error2");
                });
            Task<IQuasiHttpResponse> cancellationTask = Task.Delay(
                TimeSpan.FromSeconds(1)).ContinueWith<IQuasiHttpResponse>(_ =>
                {
                    throw new QuasiHttpException("error3");
                });
            var actualEx = await Assert.ThrowsAsync<QuasiHttpException>(() =>
            {
                return ProtocolUtilsInternal.CompleteWorkTask(
                    workTask, timeoutTask, cancellationTask);
            });
            Assert.Equal("error1", actualEx.Message);
        }

        [Fact]
        public async Task TestCompleteWorkTask8()
        {
            Task workTask = Task.Delay(
                TimeSpan.FromSeconds(2)).ContinueWith<IQuasiHttpResponse>(_ =>
                {
                    throw new QuasiHttpException("error1");
                });
            Task timeoutTask = Task.Delay(
                TimeSpan.FromSeconds(2)).ContinueWith<IQuasiHttpResponse>(_ =>
                {
                    throw new QuasiHttpException("error2");
                });
            Task cancellationTask = Task.Delay(
                TimeSpan.FromSeconds(0.5)).ContinueWith<IQuasiHttpResponse>(_ =>
                {
                    throw new QuasiHttpException("error3");
                });
            var actualEx = await Assert.ThrowsAsync<QuasiHttpException>(() =>
            {
                return ProtocolUtilsInternal.CompleteWorkTask(
                    workTask, timeoutTask, cancellationTask);
            });
            Assert.Equal("error3", actualEx.Message);
        }

        [Fact]
        public async Task TestCompleteWorkTaskForErrors()
        {
            Task<IQuasiHttpResponse> workTask = null;
            Task<IQuasiHttpResponse> timeoutTask = Task.FromResult(
                new DefaultQuasiHttpResponse() as IQuasiHttpResponse);
            Task<IQuasiHttpResponse> cancellationTask = Task.Delay(
                TimeSpan.FromSeconds(1)).ContinueWith<IQuasiHttpResponse>(_ =>
                {
                    return null;
                });
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                ProtocolUtilsInternal.CompleteWorkTask(
                    workTask, timeoutTask, cancellationTask));
        }
    }
}
