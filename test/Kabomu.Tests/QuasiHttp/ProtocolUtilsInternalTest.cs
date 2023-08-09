using Kabomu.Common;
using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.EntityBody;
using Kabomu.Tests.Shared.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
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
                { "d2", "TRUE" }, { "e", "ghana" }
            };
            key = "e";
            testData.Add(new object[] { environment, key, null });

            environment = new Dictionary<string, object>
            {
                { "d2", "TRUE" }, { "e", new List<string>() }
            };
            key = "e";
            testData.Add(new object[] { environment, key, null });

            environment = new Dictionary<string, object>
            {
                { "d2", "TRUE" }, { "e", "ghana" }
            };
            key = "d2";
            testData.Add(new object[] { environment, key, true });

            environment = new Dictionary<string, object>
            {
                { "d2", "true" }, { "e", "ghana" }
            };
            key = "d2";
            testData.Add(new object[] { environment, key, true });

            environment = new Dictionary<string, object>
            {
                { "d2", "TRUE" }, { "e", "ghana" }
            };
            key = "d2";
            testData.Add(new object[] { environment, key, true });

            environment = new Dictionary<string, object>
            {
                { "d", "FALSE" }, { "e", "ghana" }
            };
            key = "d";
            testData.Add(new object[] { environment, key, false });

            return testData;
        }

        [Theory]
        [MemberData(nameof(CreateTestCreateEquivalentInMemoryBodyData))]
        public async Task TestCreateEquivalentInMemoryBody(int bufferingLimit,
            IQuasiHttpBody body, byte[] expectedBodyBytes)
        {
            // arrange.
            IQuasiHttpBody expected = new ByteBufferBody(expectedBodyBytes)
            {
                ContentLength = body.ContentLength
            };

            // act.
            var actualResponseBody = await ProtocolUtilsInternal.CreateEquivalentOfUnknownBodyInMemory(body,
                bufferingLimit);
            
            // assert.
            await ComparisonUtils.CompareBodies(expected, actualResponseBody, expectedBodyBytes);
        }

        public static List<object[]> CreateTestCreateEquivalentInMemoryBodyData()
        {
            var testData = new List<object[]>();

            int bufferingLimit = 0;
            byte[] expectedResBodyBytes = new byte[0];
            IQuasiHttpBody responseBody = new ByteBufferBody(expectedResBodyBytes);
            testData.Add(new object[] { bufferingLimit, responseBody, expectedResBodyBytes });

            bufferingLimit = 0;
            expectedResBodyBytes = new byte[0];
            responseBody = new StringBody(ByteUtils.BytesToString(expectedResBodyBytes));
            testData.Add(new object[] { bufferingLimit, responseBody, expectedResBodyBytes });

            expectedResBodyBytes = new byte[]{ (byte)'a', (byte)'b', (byte)'c', (byte)'d',
                (byte)'e', (byte)'f' };
            bufferingLimit = expectedResBodyBytes.Length;
            responseBody = new StringBody(ByteUtils.BytesToString(expectedResBodyBytes))
            {
                ContentLength = -10
            };
            testData.Add(new object[] { bufferingLimit, responseBody, expectedResBodyBytes });

            expectedResBodyBytes = new byte[]{ (byte)'a', (byte)'b', (byte)'c', (byte)'d',
                (byte)'e', (byte)'f' };
            bufferingLimit = expectedResBodyBytes.Length;
            responseBody = new StringBody(ByteUtils.BytesToString(expectedResBodyBytes));
            testData.Add(new object[] { bufferingLimit, responseBody, expectedResBodyBytes });

            bufferingLimit = 8;
            expectedResBodyBytes = new byte[]{ (byte)'a', (byte)'b', (byte)'c', (byte)'d',
                (byte)'e', (byte)'f' };
            responseBody = new ByteBufferBody(expectedResBodyBytes)
            {
                ContentLength = -1
            };
            testData.Add(new object[] { bufferingLimit, responseBody, expectedResBodyBytes });

            // test that over abundance of data works fine.

            bufferingLimit = 4;
            responseBody = new ByteBufferBody(new byte[]{ (byte)'a', (byte)'b', (byte)'c', (byte)'d',
                (byte)'e', (byte)'f' })
            {
                ContentLength = 3
            };
            expectedResBodyBytes = new byte[]{ (byte)'a', (byte)'b', (byte)'c' };
            testData.Add(new object[] { bufferingLimit, responseBody, expectedResBodyBytes });

            return testData;
        }

        [Fact]
        public async Task TestCreateEquivalentInMemoryBodyForErrors1()
        {
            int bufferingLimit = 3;
            var responseBody = new ByteBufferBody(ByteUtils.StringToBytes("xyz!"));
            var actualEx = await Assert.ThrowsAsync<CustomIOException>(() =>
            {
                return ProtocolUtilsInternal.CreateEquivalentOfUnknownBodyInMemory(responseBody,
                    bufferingLimit);
            });
            Assert.Contains($"limit of {bufferingLimit}", actualEx.Message);
        }

        [Fact]
        public async Task TestCreateEquivalentInMemoryBodyForErrors2()
        {
            int bufferingLimit = 30;
            var responseBody = new StringBody("xyz!")
            {
                ContentLength = 5
            };
            var actualEx = await Assert.ThrowsAsync<CustomIOException>(() =>
            {
                return ProtocolUtilsInternal.CreateEquivalentOfUnknownBodyInMemory(responseBody,
                    bufferingLimit);
            });
            Assert.Contains("length of 5", actualEx.Message);
        }

        [Fact]
        public async Task TestTransferBodyToTransport1()
        {
            int maxChunkSize = 6;
            var srcData = "data bits and bytes";
            var writer = new MemoryStream();
            var expected = new byte[] { 0 ,0, 8, 1, 0, (byte)'d',
                (byte)'a', (byte)'t', (byte)'a', (byte)' ', (byte)'b',
                0, 0, 8, 1, 0, (byte)'i', (byte)'t', (byte)'s', (byte)' ',
                (byte)'a', (byte)'n', 0, 0, 8, 1, 0, (byte)'d', (byte)' ',
                (byte)'b', (byte)'y', (byte)'t', (byte)'e', 0, 0, 3, 1, 0,
                (byte)'s', 0, 0, 2, 1, 0
            };
            var body = new StringBody(srcData)
            {
                ContentLength = -1
            };
            await ProtocolUtilsInternal.TransferBodyToTransport(writer,
                maxChunkSize, body);

            Assert.Equal(expected, writer.ToArray());
        }

        [Fact]
        public async Task TestTransferBodyToTransport2()
        {
            int maxChunkSize = 6;
            var writer = new MemoryStream();
            var writerWrapper = new LambdaBasedCustomReaderWriter
            {
                WriteFunc = (data, off, len) =>
                    writer.WriteAsync(data, off, len)
            };
            var expected = "camouflage";
            var body = new StringBody(expected);
            await ProtocolUtilsInternal.TransferBodyToTransport(writerWrapper,
                maxChunkSize, body);

            Assert.Equal(expected, ByteUtils.BytesToString(
                writer.ToArray()));
        }

        /// <summary>
        /// Assert that body with zero content length is not processed,
        /// and check that dispose is not called.
        /// </summary>
        [Fact]
        public async Task TestTransferBodyToTransport3()
        {
            int maxChunkSize = 6;
            var writer = new MemoryStream();
            var writerWrapper = new LambdaBasedCustomReaderWriter
            {
                WriteFunc = (data, off, len) =>
                    writer.WriteAsync(data, off, len)
            };
            var srcData = "ice";
            var reader = new MemoryStream(
                ByteUtils.StringToBytes(srcData));

            var body = new LambdaBasedQuasiHttpBody
            {
                ReaderFunc = () => reader,
                ContentLength = 0,
                ReleaseFunc = async () => await reader.DisposeAsync()
            };
            await ProtocolUtilsInternal.TransferBodyToTransport(writerWrapper,
                maxChunkSize, body);

            Assert.Equal(new byte[0], writer.ToArray());

            // check that release is not called on body during transfer.
            var actual = await IOUtils.ReadAllBytes(reader);
            Assert.Equal(srcData, ByteUtils.BytesToString(actual));
        }

        // 
        /// <summary>
        /// Assert that positive content length is not enforced
        /// </summary>
        [Fact]
        public async Task TestTransferBodyToTransport4()
        {
            int maxChunkSize = -1;
            var writer = new MemoryStream();
            var expected = "frutis and pils";
            var reader = new MemoryStream(
                ByteUtils.StringToBytes(expected));

            var body = new LambdaBasedQuasiHttpBody
            {
                ReaderFunc = () => reader,
                ContentLength = 456
            };
            await ProtocolUtilsInternal.TransferBodyToTransport(writer,
                maxChunkSize, body);

            Assert.Equal(expected, ByteUtils.BytesToString(
                writer.ToArray()));
        }

        /// <summary>
        /// Assert that no error occurs with null body.
        /// </summary>
        [Fact]
        public async Task TestTransferBodyToTransport5()
        {
            int maxChunkSize = 6;
            var writer = new MemoryStream();
            await ProtocolUtilsInternal.TransferBodyToTransport(writer,
                maxChunkSize, null);

            Assert.Equal(new byte[0], writer.ToArray());
        }

        [Fact]
        public async Task TestCreateBodyFromTransport1()
        {
            // arrange
            var srcData = "ice";
            var expectedData = ByteUtils.StringToBytes(srcData);
            var reader = new MemoryStream(expectedData);
            long contentLength = srcData.Length;
            Func<Task> releaseFunc = null;
            int maxChunkSize = 6;
            bool bufferingEnabled = false;
            int bodyBufferingSizeLimit = 2;

            // act
            var body = await ProtocolUtilsInternal.CreateBodyFromTransport(
                reader, contentLength, releaseFunc, maxChunkSize,
                bufferingEnabled, bodyBufferingSizeLimit);

            // assert
            Assert.Equal(contentLength, body.ContentLength);
            var actualData = new byte[body.ContentLength];
            await IOUtils.ReadBytesFully(body.Reader,
                actualData, 0, actualData.Length);
            Assert.Equal(expectedData, actualData);

            // assert no errors on release
            await body.Release();
        }

        [Fact]
        public async Task TestCreateBodyFromTransport2()
        {
            // arrange
            var srcData = "ice";
            var expectedData = ByteUtils.StringToBytes(srcData);
            var srcStream = new MemoryStream(expectedData);
            var reader = new LambdaBasedCustomReaderWriter
            {
                ReadFunc = (data, offset, length) =>
                    srcStream.ReadAsync(data, offset, length)
            };
            long contentLength = srcData.Length;
            var releaseCallCount = 0;
            Func<Task> releaseFunc = () =>
            {
                releaseCallCount++;
                return Task.CompletedTask;
            };
            int maxChunkSize = 6;
            bool bufferingEnabled = true;
            int bodyBufferingSizeLimit = 4;

            // act
            var body = await ProtocolUtilsInternal.CreateBodyFromTransport(
                reader, contentLength, releaseFunc, maxChunkSize,
                bufferingEnabled, bodyBufferingSizeLimit);

            // assert
            Assert.Equal(contentLength, body.ContentLength);
            var actualData = new byte[body.ContentLength];
            await IOUtils.ReadBytesFully(body.Reader,
                actualData, 0, actualData.Length);
            Assert.Equal(expectedData, actualData);

            // assert that transport wasn't released due to buffering.
            await body.Release();
            Assert.Equal(0, releaseCallCount);
        }

        [Fact]
        public async Task TestCreateBodyFromTransport3()
        {
            // arrange
            var srcData = new byte[] { 0 ,0, 11, 1, 0, (byte)'d',
                (byte)'a', (byte)'t', (byte)'a', (byte)' ', (byte)'b',
                (byte)'i', (byte)'t', (byte)'s', 0, 0, 11, 1, 0, (byte)' ',
                (byte)'a', (byte)'n', (byte)'d', (byte)' ', (byte)'b',
                (byte)'y', (byte)'t', (byte)'e', 0, 0, 2, 1, 0
            };
            var expectedData = ByteUtils.StringToBytes("data bits and byte");
            var srcStream = new MemoryStream(srcData);
            var reader = new LambdaBasedCustomReaderWriter
            {
                ReadFunc = (data, offset, length) =>
                    srcStream.ReadAsync(data, offset, length)
            };
            long contentLength = -1;
            Func<Task> releaseFunc = null;
            int maxChunkSize = 6;
            bool bufferingEnabled = true;
            int bodyBufferingSizeLimit = 30;

            // act
            var body = await ProtocolUtilsInternal.CreateBodyFromTransport(
                reader, contentLength, releaseFunc, maxChunkSize,
                bufferingEnabled, bodyBufferingSizeLimit);

            // assert
            Assert.Equal(contentLength, body.ContentLength);
            var actualData = await IOUtils.ReadAllBytes(body.Reader);
            Assert.Equal(expectedData, actualData);

            // assert no errors on release
            await body.Release();
        }

        [Fact]
        public async Task TestCreateBodyFromTransport4()
        {
            // arrange
            var srcData = new byte[] { 0 ,0, 16, 1, 0, (byte)'b',
                (byte)'i', (byte)'t', (byte)'s', (byte)' ',
                (byte)'a', (byte)'n', (byte)'d', (byte)' ', (byte)'b',
                (byte)'y', (byte)'t', (byte)'e', (byte)'s', 0, 0, 2, 1, 0
            };
            var expectedData = ByteUtils.StringToBytes("bits and bytes");
            var reader = new MemoryStream(srcData);
            long contentLength = -1;
            var releaseCallCount = 0;
            Func<Task> releaseFunc = () =>
            {
                releaseCallCount++;
                return Task.CompletedTask;
            };
            int maxChunkSize = 60;
            bool bufferingEnabled = false;
            int bodyBufferingSizeLimit = 3;

            // act
            var body = await ProtocolUtilsInternal.CreateBodyFromTransport(
                reader, contentLength, releaseFunc, maxChunkSize,
                bufferingEnabled, bodyBufferingSizeLimit);

            // assert
            Assert.Equal(contentLength, body.ContentLength);
            var actualData = await IOUtils.ReadAllBytes(body.Reader);
            Assert.Equal(expectedData, actualData);

            // assert that transport was released.
            await body.Release();
            Assert.Equal(1, releaseCallCount);
        }

        /// <summary>
        /// Test that zero content length returns null.
        /// </summary>
        [Fact]
        public async Task TestCreateBodyFromTransport5()
        {
            // arrange
            var srcData = "ice";
            var expectedData = ByteUtils.StringToBytes(srcData);
            var reader = new MemoryStream(expectedData);
            long contentLength = 0;
            var releaseCallCount = 0;
            Func<Task> releaseFunc = () =>
            {
                releaseCallCount++;
                return Task.CompletedTask;
            };
            int maxChunkSize = 6;
            bool bufferingEnabled = true;
            int bodyBufferingSizeLimit = 4;

            // act
            var body = await ProtocolUtilsInternal.CreateBodyFromTransport(
                reader, contentLength, releaseFunc, maxChunkSize,
                bufferingEnabled, bodyBufferingSizeLimit);

            // assert
            Assert.Null(body);

            // assert that transport wasn't released.
            Assert.Equal(0, releaseCallCount);
        }

        [Fact]
        public async Task TestCreateBodyFromTransportForErrors1()
        {
            // arrange
            var srcData = new byte[] { 0 ,0, 16, 1, 0, (byte)'b',
                (byte)'i', (byte)'t', (byte)'s', (byte)' ',
                (byte)'a', (byte)'n', (byte)'d', (byte)' ', (byte)'b',
                (byte)'y', (byte)'t', (byte)'e', (byte)'s', 0, 0, 2, 1, 0
            };
            var expectedData = ByteUtils.StringToBytes("bits and bytes");
            var reader = new MemoryStream(srcData);
            long contentLength = -1;
            Func<Task> releaseFunc = null;
            int maxChunkSize = 60;
            bool bufferingEnabled = true;
            int bodyBufferingSizeLimit = 3;

            // act
            var actualEx = await Assert.ThrowsAsync<CustomIOException>(() =>
            {
                return ProtocolUtilsInternal.CreateBodyFromTransport(
                    reader, contentLength, releaseFunc, maxChunkSize,
                    bufferingEnabled, bodyBufferingSizeLimit);
            });
            Assert.Contains($"limit of {bodyBufferingSizeLimit}", actualEx.Message);
        }

        [Fact]
        public async Task TestCreateBodyFromTransportForErrors2()
        {
            // arrange
            var srcData = "ice";
            var expectedData = ByteUtils.StringToBytes(srcData);
            var reader = new MemoryStream(expectedData);
            long contentLength = 10;
            var releaseCallCount = 0;
            Func<Task> releaseFunc = () =>
            {
                releaseCallCount++;
                return Task.CompletedTask;
            };
            int maxChunkSize = 6;
            bool bufferingEnabled = true;
            int bodyBufferingSizeLimit = 4;

            // act
            var actualEx = await Assert.ThrowsAsync<CustomIOException>(() =>
            {
                return ProtocolUtilsInternal.CreateBodyFromTransport(
                    reader, contentLength, releaseFunc, maxChunkSize,
                    bufferingEnabled, bodyBufferingSizeLimit);
            });
            Assert.Contains($"length of {contentLength}", actualEx.Message);

            // assert that transport wasn't released.
            Assert.Equal(0, releaseCallCount);
        }

        [Fact]
        public async Task TestCompleteRequestProcessing1()
        {
            var expected = new DefaultQuasiHttpResponse();
            Task<IQuasiHttpResponse> workTask = Task.FromResult(
                expected as IQuasiHttpResponse);
            Task<IQuasiHttpResponse> timeoutTask = null;
            Task<IQuasiHttpResponse> cancellationTask = null;
            var actual = await ProtocolUtilsInternal.CompleteRequestProcessing(
                workTask, timeoutTask, cancellationTask);
            Assert.Same(expected, actual);
        }

        [Fact]
        public async Task TestCompleteRequestProcessing2()
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
            var actual = await ProtocolUtilsInternal.CompleteRequestProcessing(
                workTask, timeoutTask, cancellationTask);
            Assert.Same(expected, actual);
        }

        [Fact]
        public async Task TestCompleteRequestProcessing3()
        {
            Task<IQuasiHttpResponse> workTask = Task.Delay(
                TimeSpan.FromSeconds(1)).ContinueWith<IQuasiHttpResponse>(_ =>
                {
                    return null;
                });
            Task<IQuasiHttpResponse> timeoutTask = null;
            Task<IQuasiHttpResponse> cancellationTask = Task.FromResult<IQuasiHttpResponse>(
                new DefaultQuasiHttpResponse());
            var actual = await ProtocolUtilsInternal.CompleteRequestProcessing(
                workTask, timeoutTask, cancellationTask);
            Assert.Null(actual);
        }

        [Fact]
        public async Task TestCompleteRequestProcessing4()
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
            var actual = await ProtocolUtilsInternal.CompleteRequestProcessing(
                workTask, timeoutTask, cancellationTask);
            Assert.Same(expected, actual);
        }

        [Fact]
        public async Task TestCompleteRequestProcessing5()
        {
            Task<IQuasiHttpResponse> workTask = Task.Delay(
                TimeSpan.FromSeconds(2)).ContinueWith<IQuasiHttpResponse>(_ =>
                {
                    return new DefaultQuasiHttpResponse();
                });
            Task<IQuasiHttpResponse> timeoutTask = Task.Delay(
                TimeSpan.FromSeconds(0.5)).ContinueWith<IQuasiHttpResponse>(_ =>
                {
                    throw new QuasiHttpRequestProcessingException("error1");
                });
            Task<IQuasiHttpResponse> cancellationTask = Task.Delay(
                TimeSpan.FromSeconds(1)).ContinueWith<IQuasiHttpResponse>(_ =>
                {
                    return new DefaultQuasiHttpResponse();
                });
            var actualEx = await Assert.ThrowsAsync<QuasiHttpRequestProcessingException>(() =>
            {
                return ProtocolUtilsInternal.CompleteRequestProcessing(
                    workTask, timeoutTask, cancellationTask);
            });
            Assert.Equal("error1", actualEx.Message);
        }

        [Fact]
        public async Task TestCompleteRequestProcessing6()
        {
            Task<IQuasiHttpResponse> workTask = Task.Delay(
                TimeSpan.FromSeconds(0.5)).ContinueWith<IQuasiHttpResponse>(_ =>
                {
                    throw new QuasiHttpRequestProcessingException("error1");
                });
            Task<IQuasiHttpResponse> timeoutTask = Task.Delay(
                TimeSpan.FromSeconds(2)).ContinueWith<IQuasiHttpResponse>(_ =>
                {
                    throw new QuasiHttpRequestProcessingException("error2");
                });
            Task<IQuasiHttpResponse> cancellationTask = Task.Delay(
                TimeSpan.FromSeconds(1)).ContinueWith<IQuasiHttpResponse>(_ =>
                {
                    throw new QuasiHttpRequestProcessingException("error3");
                });
            var actualEx = await Assert.ThrowsAsync<QuasiHttpRequestProcessingException>(() =>
            {
                return ProtocolUtilsInternal.CompleteRequestProcessing(
                    workTask, timeoutTask, cancellationTask);
            });
            Assert.Equal("error1", actualEx.Message);
        }

        [Fact]
        public async Task TestCompleteRequestProcessing7()
        {
            Task<IQuasiHttpResponse> workTask = Task.Delay(
                TimeSpan.FromSeconds(2)).ContinueWith<IQuasiHttpResponse>(_ =>
                {
                    throw new QuasiHttpRequestProcessingException("error1");
                });
            Task<IQuasiHttpResponse> timeoutTask = Task.Delay(
                TimeSpan.FromSeconds(2)).ContinueWith<IQuasiHttpResponse>(_ =>
                {
                    throw new QuasiHttpRequestProcessingException("error2");
                });
            Task<IQuasiHttpResponse> cancellationTask = Task.Delay(
                TimeSpan.FromSeconds(0.5)).ContinueWith<IQuasiHttpResponse>(_ =>
                {
                    throw new QuasiHttpRequestProcessingException("error3");
                });
            var actualEx = await Assert.ThrowsAsync<QuasiHttpRequestProcessingException>(() =>
            {
                return ProtocolUtilsInternal.CompleteRequestProcessing(
                    workTask, timeoutTask, cancellationTask);
            });
            Assert.Equal("error3", actualEx.Message);
        }

        [Fact]
        public async Task TestCompleteRequestProcessingForErrors()
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
                ProtocolUtilsInternal.CompleteRequestProcessing(
                    workTask, timeoutTask, cancellationTask));
        }

        [Fact]
        public void TestSetTimeout1()
        {
            var actual = ProtocolUtilsInternal.SetTimeout<string>(0, null);
            Assert.Equal((null, null), actual);
        }

        [Fact]
        public void TestSetTimeout2()
        {
            var actual = ProtocolUtilsInternal.SetTimeout<string>(-3, null);
            Assert.Equal((null, null), actual);
        }

        [Fact]
        public async Task TestSetTimeout3()
        {
            var expectedMsg = "sea";
            var actualEx = await Assert.ThrowsAsync<QuasiHttpRequestProcessingException>(() =>
            {
                return ProtocolUtilsInternal.SetTimeout<int>(50, expectedMsg).Item1;
            });
            Assert.Equal(QuasiHttpRequestProcessingException.ReasonCodeTimeout,
                actualEx.ReasonCode);
            Assert.Equal(expectedMsg, actualEx.Message);
        }

        [Fact]
        public async Task TestSetTimeout4()
        {
            var (actualTask, timeoutId) = ProtocolUtilsInternal.SetTimeout<string>(500, null);
            await Task.Delay(100);
            timeoutId.Cancel();
            var actual = await actualTask;
            Assert.Null(actual);
        }
    }
}
