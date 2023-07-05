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
            IQuasiHttpBody originalBody, byte[] expectedBodyBytes)
        {
            // arrange.
            IQuasiHttpBody expected = new ByteBufferBody(expectedBodyBytes)
            {
                ContentType = originalBody.ContentType,
                ContentLength = originalBody.ContentLength
            };
            var disposed = false;
            var originalBody1 = originalBody;
            var reader = new LambdaBasedCustomReader
            {
                ReadFunc = (data, offset, length) =>
                {
                    if (disposed)
                    {
                        throw new ObjectDisposedException("reader");
                    }
                    return originalBody1.AsReader().ReadBytes(data, offset, length);
                },
                DisposeFunc = () =>
                {
                    disposed = true;
                    return Task.CompletedTask;
                }
            };
            originalBody = new CustomReaderBackedBody(reader)
            {
                ContentLength = originalBody1.ContentLength,
                ContentType = originalBody1.ContentType
            };

            // act.
            var actualResponseBody = await ProtocolUtilsInternal.CreateEquivalentOfUnknownBodyInMemory(originalBody,
                bufferingLimit);
            
            // assert.
            // check that original response body has been ended.
            await Assert.ThrowsAsync<ObjectDisposedException>(() =>
                originalBody.AsReader().ReadBytes(new byte[1], 0, 1));
            // finally verify content.
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
                ContentLength = -10,
                ContentType = "text/plain"
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
                ContentLength = -1,
                ContentType = "application/octet-stream"
            };
            testData.Add(new object[] { bufferingLimit, responseBody, expectedResBodyBytes });

            // test that over abundance of data works fine.

            bufferingLimit = 4;
            responseBody = new ByteBufferBody(new byte[]{ (byte)'a', (byte)'b', (byte)'c', (byte)'d',
                (byte)'e', (byte)'f' })
            {
                ContentType = "application/json",
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
            await Assert.ThrowsAsync<DataBufferLimitExceededException>(() =>
            {
                return ProtocolUtilsInternal.CreateEquivalentOfUnknownBodyInMemory(responseBody,
                    bufferingLimit);
            });
        }

        [Fact]
        public async Task TestCreateEquivalentInMemoryBodyForErrors2()
        {
            int bufferingLimit = 30;
            var responseBody = new StringBody("xyz!")
            {
                ContentLength = 5
            };
            await Assert.ThrowsAsync<ContentLengthNotSatisfiedException>(() =>
            {
                return ProtocolUtilsInternal.CreateEquivalentOfUnknownBodyInMemory(responseBody,
                    bufferingLimit);
            });
        }

        [Fact]
        public async Task TestTransferBodyToTransport1()
        {
            object connection = "davi";
            int maxChunkSize = 6;
            var transport = new DemoQuasiHttpTransport(connection,
                null, null);

            var srcData = "data bits and bytes";
            var reader = new DemoCustomReaderWriter(
                ByteUtils.StringToBytes(srcData));

            var expected = new byte[] { 0 ,0, 8, 1, 0, (byte)'d',
                (byte)'a', (byte)'t', (byte)'a', (byte)' ', (byte)'b',
                0, 0, 8, 1, 0, (byte)'i', (byte)'t', (byte)'s', (byte)' ',
                (byte)'a', (byte)'n', 0, 0, 8, 1, 0, (byte)'d', (byte)' ',
                (byte)'b', (byte)'y', (byte)'t', (byte)'e', 0, 0, 3, 1, 0,
                (byte)'s', 0, 0, 2, 1, 0
            };
            var body = new CustomReaderBackedBody(reader)
            {
                ContentLength = -1
            };
            await ProtocolUtilsInternal.TransferBodyToTransport(transport, connection,
                maxChunkSize, body);

            Assert.Equal(expected, transport.BufferStream.ToArray());
        }

        [Fact]
        public async Task TestTransferBodyToTransport2()
        {
            object connection = "david";
            int maxChunkSize = 6;
            var transport = new DemoQuasiHttpTransport(connection,
                null, null);

            var expected = "camouflage";
            var reader = new DemoCustomReaderWriter(
                ByteUtils.StringToBytes(expected));

            var body = new CustomReaderBackedBody(reader)
            {
                ContentLength = expected.Length
            };
            await ProtocolUtilsInternal.TransferBodyToTransport(transport, connection,
                maxChunkSize, body);

            Assert.Equal(expected, ByteUtils.BytesToString(
                transport.BufferStream.ToArray()));
        }

        // 
        /// <summary>
        /// Assert that positive content length is not enforced
        /// </summary>
        [Fact]
        public async Task TestTransferBodyToTransport4()
        {
            object connection = new object();
            int maxChunkSize = -1;
            var transport = new DemoQuasiHttpTransport(connection,
                null, null);

            var expected = "frutis and pils";
            var reader = new DemoCustomReaderWriter(
                ByteUtils.StringToBytes(expected));

            var body = new CustomReaderBackedBody(reader)
            {
                ContentLength = 456
            };
            await ProtocolUtilsInternal.TransferBodyToTransport(transport, connection,
                maxChunkSize, body);

            Assert.Equal(expected, ByteUtils.BytesToString(
                transport.BufferStream.ToArray()));
        }

        /// <summary>
        /// Assert that body with zero content length is not processed,
        /// and check that dispose is not called.
        /// </summary>
        [Fact]
        public async Task TestTransferBodyToTransport3()
        {
            object connection = 34;
            int maxChunkSize = 6;
            var transport = new DemoQuasiHttpTransport(connection,
                null, null);

            var srcData = "ice";
            var reader = new DemoCustomReaderWriter(
                ByteUtils.StringToBytes(srcData));

            var body = new CustomReaderBackedBody(reader)
            {
                ContentLength = 0
            };
            await ProtocolUtilsInternal.TransferBodyToTransport(transport, connection,
                maxChunkSize, body);

            Assert.Equal(new byte[0], transport.BufferStream.ToArray());

            // check that dispose is not called on body during transfer.
            var actual = await IOUtils.ReadAllBytes(reader);
            Assert.Equal(srcData, ByteUtils.BytesToString(actual));

            await Assert.ThrowsAsync<ObjectDisposedException>(() =>
                IOUtils.ReadAllBytes(reader));
        }

        [Fact]
        public async Task TestCreateBodyFromTransport1()
        {
            // arrange
            object connection = 34;
            int maxChunkSize = 6;
            var srcData = "ice";
            var expectedData = ByteUtils.StringToBytes(srcData);
            var transport = new DemoQuasiHttpTransport(connection,
                expectedData, null);
            bool releaseConnection = false;
            string contentType = "text/plain";
            long contentLength = srcData.Length;
            bool bufferingEnabled = false;
            int bodyBufferingSizeLimit = 2;

            // act
            var body = await ProtocolUtilsInternal.CreateBodyFromTransport(
                transport, connection, releaseConnection, maxChunkSize,
                contentType, contentLength, bufferingEnabled,
                bodyBufferingSizeLimit);

            // assert
            Assert.Equal(contentType, body.ContentType);
            Assert.Equal(contentLength, body.ContentLength);
            var actualData = new byte[body.ContentLength];
            await IOUtils.ReadBytesFully(body.Reader,
                actualData, 0, actualData.Length);
            Assert.Equal(expectedData, actualData);

            // assert that transport wasn't released.
            Assert.Equal(0, await transport.ReadBytes(connection, new byte[1], 0, 1));
        }

        [Fact]
        public async Task TestCreateBodyFromTransport2()
        {
            // arrange
            object connection = 34;
            int maxChunkSize = 6;
            var srcData = "ice";
            var expectedData = ByteUtils.StringToBytes(srcData);
            var transport = new DemoQuasiHttpTransport(connection,
                expectedData, null);
            bool releaseConnection = true;
            string contentType = "text/plain";
            long contentLength = srcData.Length;
            bool bufferingEnabled = true;
            int bodyBufferingSizeLimit = 4;

            // act
            var body = await ProtocolUtilsInternal.CreateBodyFromTransport(
                transport, connection, releaseConnection, maxChunkSize,
                contentType, contentLength, bufferingEnabled,
                bodyBufferingSizeLimit);

            // assert
            Assert.Equal(contentType, body.ContentType);
            Assert.Equal(contentLength, body.ContentLength);
            var actualData = new byte[body.ContentLength];
            await IOUtils.ReadBytesFully(body.Reader,
                actualData, 0, actualData.Length);
            Assert.Equal(expectedData, actualData);

            // assert that transport was released.
            await Assert.ThrowsAsync<ObjectDisposedException>(() =>
                transport.ReadBytes(connection, new byte[1], 0, 1));
        }

        [Fact]
        public async Task TestCreateBodyFromTransport3()
        {
            // arrange
            object connection = "34d";
            int maxChunkSize = 6;
            var srcData = new byte[] { 0 ,0, 11, 1, 0, (byte)'d',
                (byte)'a', (byte)'t', (byte)'a', (byte)' ', (byte)'b',
                (byte)'i', (byte)'t', (byte)'s', 0, 0, 11, 1, 0, (byte)' ',
                (byte)'a', (byte)'n', (byte)'d', (byte)' ', (byte)'b',
                (byte)'y', (byte)'t', (byte)'e', 0, 0, 2, 1, 0
            };
            var expectedData = ByteUtils.StringToBytes("data bits and byte");
            var transport = new DemoQuasiHttpTransport(connection,
                srcData, null);
            bool releaseConnection = false;
            string contentType = "application/xml";
            long contentLength = -1;
            bool bufferingEnabled = true;
            int bodyBufferingSizeLimit = 30;

            // act
            var body = await ProtocolUtilsInternal.CreateBodyFromTransport(
                transport, connection, releaseConnection, maxChunkSize,
                contentType, contentLength, bufferingEnabled,
                bodyBufferingSizeLimit);

            // assert
            Assert.Equal(contentType, body.ContentType);
            Assert.Equal(contentLength, body.ContentLength);
            var actualData = await IOUtils.ReadAllBytes(body.Reader);
            Assert.Equal(expectedData, actualData);

            // assert that transport wasn't released.
            Assert.Equal(0, await transport.ReadBytes(connection, new byte[1], 0, 1));
        }

        [Fact]
        public async Task TestCreateBodyFromTransport4()
        {
            // arrange
            object connection = "tea";
            int maxChunkSize = 60;
            var srcData = new byte[] { 0 ,0, 16, 1, 0, (byte)'b',
                (byte)'i', (byte)'t', (byte)'s', (byte)' ',
                (byte)'a', (byte)'n', (byte)'d', (byte)' ', (byte)'b',
                (byte)'y', (byte)'t', (byte)'e', (byte)'s', 0, 0, 2, 1, 0
            };
            var expectedData = ByteUtils.StringToBytes("bits and bytes");
            var transport = new DemoQuasiHttpTransport(connection,
                srcData, null);
            bool releaseConnection = true;
            string contentType = null;
            long contentLength = -1;
            bool bufferingEnabled = false;
            int bodyBufferingSizeLimit = 3;

            // act
            var body = await ProtocolUtilsInternal.CreateBodyFromTransport(
                transport, connection, releaseConnection, maxChunkSize,
                contentType, contentLength, bufferingEnabled,
                bodyBufferingSizeLimit);

            // assert
            Assert.Equal(contentType, body.ContentType);
            Assert.Equal(contentLength, body.ContentLength);
            var actualData = await IOUtils.ReadAllBytes(body.Reader);
            Assert.Equal(expectedData, actualData);

            // assert that transport was released.
            await Assert.ThrowsAsync<ObjectDisposedException>(() =>
                transport.ReadBytes(connection, new byte[1], 0, 1));
        }

        [Fact]
        public async Task TestCreateBodyFromTransport5()
        {
            // arrange
            object connection = new object();
            int maxChunkSize = 6;
            var srcData = "ice";
            var expectedData = ByteUtils.StringToBytes(srcData);
            var transport = new DemoQuasiHttpTransport(connection,
                expectedData, null);
            bool releaseConnection = true;
            string contentType = "text/csv";
            long contentLength = 0;
            bool bufferingEnabled = true;
            int bodyBufferingSizeLimit = 4;

            // act
            var body = await ProtocolUtilsInternal.CreateBodyFromTransport(
                transport, connection, releaseConnection, maxChunkSize,
                contentType, contentLength, bufferingEnabled,
                bodyBufferingSizeLimit);

            // assert
            Assert.Null(body);

            // assert that transport wasn't released.
            Assert.Equal(1, await transport.ReadBytes(connection, new byte[1], 0, 1));
        }

        [Fact]
        public async Task TestCreateBodyFromTransportForErrors1()
        {
            // arrange
            object connection = "tea";
            int maxChunkSize = 60;
            var srcData = new byte[] { 0 ,0, 16, 1, 0, (byte)'b',
                (byte)'i', (byte)'t', (byte)'s', (byte)' ',
                (byte)'a', (byte)'n', (byte)'d', (byte)' ', (byte)'b',
                (byte)'y', (byte)'t', (byte)'e', (byte)'s', 0, 0, 2, 1, 0
            };
            var expectedData = ByteUtils.StringToBytes("bits and bytes");
            var transport = new DemoQuasiHttpTransport(connection,
                srcData, null);
            bool releaseConnection = false;
            string contentType = null;
            long contentLength = -1;
            bool bufferingEnabled = true;
            int bodyBufferingSizeLimit = 3;

            // act
            await Assert.ThrowsAsync<DataBufferLimitExceededException>(() =>
            {
                return ProtocolUtilsInternal.CreateBodyFromTransport(
                    transport, connection, releaseConnection, maxChunkSize,
                    contentType, contentLength, bufferingEnabled,
                    bodyBufferingSizeLimit);
            });
        }

        [Fact]
        public async Task TestCreateBodyFromTransportForErrors2()
        {
            // arrange
            object connection = 34;
            int maxChunkSize = 6;
            var srcData = "ice";
            var expectedData = ByteUtils.StringToBytes(srcData);
            var transport = new DemoQuasiHttpTransport(connection,
                expectedData, null);
            bool releaseConnection = true;
            string contentType = "text/plain";
            long contentLength = 10;
            bool bufferingEnabled = true;
            int bodyBufferingSizeLimit = 4;

            // act
            await Assert.ThrowsAsync<ContentLengthNotSatisfiedException>(() =>
            {
                return ProtocolUtilsInternal.CreateBodyFromTransport(
                    transport, connection, releaseConnection, maxChunkSize,
                    contentType, contentLength, bufferingEnabled,
                    bodyBufferingSizeLimit);
            });
        }

        [Fact]
        public async Task TestCompleteRequestProcessing1()
        {
            var expected = new DefaultQuasiHttpResponse();
            Task<IQuasiHttpResponse> workTask = Task.FromResult(
                expected as IQuasiHttpResponse);
            Task<IQuasiHttpResponse> cancellationTask = null;
            string errorMessage = null;
            Action<Exception> errorCallback = null;
            var actual = await ProtocolUtilsInternal.CompleteRequestProcessing(
                workTask, cancellationTask, errorMessage, errorCallback);
            Assert.Same(expected, actual);
        }

        [Fact]
        public async Task TestCompleteRequestProcessing2()
        {
            var expected = new DefaultQuasiHttpResponse();
            Task<IQuasiHttpResponse> workTask = Task.FromResult(
                expected as IQuasiHttpResponse);
            Task<IQuasiHttpResponse> cancellationTask = Task.Delay(
                TimeSpan.FromDays(4)).ContinueWith<IQuasiHttpResponse>(_ =>
                {
                    return null;
                });
            string errorMessage = null;
            Action<Exception> errorCallback = null;
            var actual = await ProtocolUtilsInternal.CompleteRequestProcessing(
                workTask, cancellationTask, errorMessage, errorCallback);
            Assert.Same(expected, actual);
        }

        [Fact]
        public async Task TestCompleteRequestProcessing3()
        {
            Task<IQuasiHttpResponse> workTask = Task.Delay(
                TimeSpan.FromDays(4)).ContinueWith<IQuasiHttpResponse>(_ =>
                {
                    return new DefaultQuasiHttpResponse();
                });
            Task<IQuasiHttpResponse> cancellationTask = Task.FromResult(
                null as IQuasiHttpResponse);
            string errorMessage = null;
            Action<Exception> errorCallback = null;
            var actual = await ProtocolUtilsInternal.CompleteRequestProcessing(
                workTask, cancellationTask, errorMessage, errorCallback);
            Assert.Null(actual);
        }

        [Fact]
        public async Task TestCompleteRequestProcessing4()
        {
            Task<IQuasiHttpResponse> workTask = Task.Delay(
                TimeSpan.FromDays(4)).ContinueWith<IQuasiHttpResponse>(_ =>
                {
                    return new DefaultQuasiHttpResponse();
                });
            Task<IQuasiHttpResponse> cancellationTask = Task.FromException<IQuasiHttpResponse>(
                new QuasiHttpRequestProcessingException("error1"));
            string errorMessage = "should be ignored";
            Action<Exception> errorCallback = e =>
            {
                Assert.Equal("error1", e.Message);
            };
            var actualEx = await Assert.ThrowsAsync<QuasiHttpRequestProcessingException>(() =>
            {
                return ProtocolUtilsInternal.CompleteRequestProcessing(
                    workTask, cancellationTask, errorMessage, errorCallback);
            });
            Assert.Equal("error1", actualEx.Message);
        }

        [Fact]
        public async Task TestCompleteRequestProcessing5()
        {
            Task<IQuasiHttpResponse> workTask = Task.FromException<IQuasiHttpResponse>(
                new InvalidOperationException("error2"));
            Task<IQuasiHttpResponse> cancellationTask = null;
            string errorMessage = "should not be ignored";
            Action<Exception> errorCallback = null;
            var actualEx = await Assert.ThrowsAsync<QuasiHttpRequestProcessingException>(() =>
            {
                return ProtocolUtilsInternal.CompleteRequestProcessing(
                    workTask, cancellationTask, errorMessage, errorCallback);
            });
            Assert.Equal("should not be ignored", actualEx.Message);
            Assert.NotNull(actualEx.InnerException);
            Assert.Equal("error2", actualEx.InnerException.Message);
        }

        [Fact]
        public async Task TestCompleteRequestProcessing6()
        {
            Task<IQuasiHttpResponse> workTask = Task.FromException<IQuasiHttpResponse>(
                new InvalidOperationException("error3a"));
            var cancellationTcs = new TaskCompletionSource<IQuasiHttpResponse>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            string errorMessage = "should be ignored";
            Action<Exception> errorCallback = e =>
            {
                Assert.Equal("should be ignored", e.Message);
                Assert.NotNull(e.InnerException);
                Assert.Equal("error3a", e.InnerException.Message);
                cancellationTcs.SetException(new InvalidOperationException("error3b"));
            };
            var actualEx = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            {
                return ProtocolUtilsInternal.CompleteRequestProcessing(
                    workTask, cancellationTcs.Task, errorMessage, errorCallback);
            });
            Assert.Equal("error3b", actualEx.Message);
        }
    }
}
