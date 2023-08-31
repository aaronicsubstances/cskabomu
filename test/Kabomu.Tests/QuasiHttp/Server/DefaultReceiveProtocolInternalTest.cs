using Kabomu.Common;
using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.ChunkedTransfer;
using Kabomu.QuasiHttp.EntityBody;
using Kabomu.QuasiHttp.Server;
using Kabomu.Tests.Shared.Common;
using Kabomu.Tests.Shared.QuasiHttp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.QuasiHttp.Server
{
    public class DefaultReceiveProtocolInternalTest
    {
        public DefaultReceiveProtocolInternalTest()
        {

        }

        private static ICustomWriter SetUpReceivingOfResponseToBeWritten(
            IQuasiHttpMutableResponse response, byte[] expectedResBodyBytes,
            MemoryStream headerReceiver, MemoryStream bodyReceiver)
        {
            var backingWriters = new List<object>();
            var helpingWriter = new SequenceCustomWriter
            {
                Writers = backingWriters
            };
            backingWriters.Add(headerReceiver);
            if ((response.Body?.ContentLength ?? 0) != 0)
            {
                backingWriters.Add(bodyReceiver);
                // update body with writable.
                ((LambdaBasedQuasiHttpBody)response.Body).SelfWritable = new LambdaBasedCustomWritable
                {
                    WritableFunc = async writer =>
                    {
                        helpingWriter.SwitchOver();
                        await IOUtils.WriteBytes(writer, expectedResBodyBytes, 0,
                            expectedResBodyBytes.Length);
                    }
                };
            }
            return helpingWriter;
        }

        private static async Task<ICustomReader> SerializeRequestToBeRead(
            IQuasiHttpRequest req, byte[] reqBodyBytes)
        {
            var reqChunk = CustomChunkedTransferCodec.CreateFromRequest(req);
            var helpingReaders = new List<object>();
            var headerStream = new MemoryStream();
            await new CustomChunkedTransferCodec().WriteLeadChunk(headerStream,
                reqChunk, CustomChunkedTransferCodec.HardMaxChunkSizeLimit);
            headerStream.Position = 0; // reset for reading.
            helpingReaders.Add(headerStream);
            if (req.Body != null)
            {
                var reqBodyStream = new MemoryStream();
                object reqBodyWriter = reqBodyStream;
                var endWrites = false;
                if (reqChunk.ContentLength < 0)
                {
                    reqBodyWriter = new ChunkEncodingCustomWriter(reqBodyWriter);
                    endWrites = true;
                }
                await IOUtils.WriteBytes(reqBodyWriter,
                    reqBodyBytes, 0, reqBodyBytes.Length);
                if (endWrites)
                {
                    await ((ChunkEncodingCustomWriter)reqBodyWriter).EndWrites();
                }
                reqBodyStream.Position = 0; // reset for reading.
                helpingReaders.Add(reqBodyStream);
            }
            return new SequenceCustomReader
            {
                Readers = helpingReaders
            };
        }

        private static async Task<MemoryStream> CreateRequestStream(IQuasiHttpRequest req,
            int maxChunkSize, int bodyMaxChunkSize)
        {
            var reqWriter = new MemoryStream();
            await new CustomChunkedTransferCodec().WriteLeadChunk(reqWriter,
                CustomChunkedTransferCodec.CreateFromRequest(req), maxChunkSize);
            if (req.Body != null)
            {
                if (req.Body.ContentLength < 0)
                {
                    var encoder = new ChunkEncodingCustomWriter(reqWriter,
                        bodyMaxChunkSize);
                    await req.Body.WriteBytesTo(encoder);
                    await encoder.EndWrites();
                }
                else
                {
                    await req.Body.WriteBytesTo(reqWriter);
                }
            }
            reqWriter.Position = 0; // reset for reading.
            return reqWriter;
        }

        [Fact]
        public async Task TestReceiveForDependencyErrors()
        {
            await Assert.ThrowsAsync<MissingDependencyException>(() =>
            {
                var instance = new DefaultReceiveProtocolInternal
                {
                    Transport = new DemoQuasiHttpTransport(null, null, null)
                };
                return instance.Receive();
            });

            await Assert.ThrowsAsync<MissingDependencyException>(() =>
            {
                var instance = new DefaultReceiveProtocolInternal
                {
                    Application = new ConfigurableQuasiHttpApplication()
                };
                return instance.Receive();
            });
        }

        [Fact]
        public async Task TestNoReaderForRequestHeadersError()
        {
            object connection = new List<object>();
            var application = new ConfigurableQuasiHttpApplication
            {
                ProcessRequestCallback = async (request) =>
                {
                    return new DefaultQuasiHttpResponse();
                }
            };
            var resStream = new LambdaBasedCustomReaderWriter
            {
                WriteFunc = (data, offset, length)
                    => Task.CompletedTask
            };
            var transport = new DemoQuasiHttpTransport(connection,
                null, resStream);
            var instance = new DefaultReceiveProtocolInternal
            {
                Application = application,
                Transport = transport,
                Connection = connection
            };

            var actualEx = await Assert.ThrowsAsync<QuasiHttpRequestProcessingException>(() =>
                instance.Receive());
            Assert.Contains("no reader for connection", actualEx.Message);
        }

        [Fact]
        public async Task TestNoWriterForResponseHeadersError()
        {
            object connection = new List<object>();
            var request = new DefaultQuasiHttpRequest();
            var reqStream = await CreateRequestStream(request,
                0, 0);
            var application = new ConfigurableQuasiHttpApplication
            {
                ProcessRequestCallback = async (request) =>
                {
                    return new DefaultQuasiHttpResponse();
                }
            };
            var transport = new DemoQuasiHttpTransport(connection,
                reqStream, null);
            var instance = new DefaultReceiveProtocolInternal
            {
                Application = application,
                Transport = transport,
                Connection = connection
            };

            var actualEx = await Assert.ThrowsAsync<QuasiHttpRequestProcessingException>(() =>
                instance.Receive());
            Assert.Contains("no writer for connection", actualEx.Message);
        }

        [Fact]
        public async Task TestReceiveForRejectionOfNullResponses()
        {
            var connection = "example";
            var request = new DefaultQuasiHttpRequest();
            IQuasiHttpResponse expectedResponse = null;

            var helpingReader = await SerializeRequestToBeRead(
                request, null);
            var headerReceiver = new MemoryStream();

            var transport = new DemoQuasiHttpTransport(connection, helpingReader,
                headerReceiver);

            var app = new ConfigurableQuasiHttpApplication
            {
                ProcessRequestCallback = (req) =>
                {
                    return Task.FromResult(expectedResponse);
                }
            };
            var instance = new DefaultReceiveProtocolInternal
            {
                Application = app,
                Transport = transport,
                MaxChunkSize = 50,
                Connection = connection
            };

            var ex = await Assert.ThrowsAsync<QuasiHttpRequestProcessingException>(() =>
            {
                return instance.Receive();
            });
            Assert.Contains("no response", ex.Message);
            Assert.Equal(0, transport.ReleaseCallCount);

            await instance.Cancel();
            Assert.Equal(1, transport.ReleaseCallCount);
        }

        [Fact]
        public async Task TestReceiveEnsuresReleaseOnNonNullResponse()
        {
            var connection = new List<string> { "example" };
            var request = new DefaultQuasiHttpRequest();
            var responseReleaseCallCount = 0;
            var expectedResponse = new ConfigurableQuasiHttpResponse
            {
                ReleaseFunc = async () =>
                {
                    responseReleaseCallCount++;
                    throw new Exception("should be ignored");
                },
                Body = new LambdaBasedQuasiHttpBody
                {
                    ContentLength = -1,
                    ReaderFunc = () => throw new NotImplementedException()
                }
            };

            var helpingReader = await SerializeRequestToBeRead(
                request, null);
            var headerReceiver = new MemoryStream();

            var transport = new DemoQuasiHttpTransport(connection, helpingReader,
                headerReceiver);

            var app = new ConfigurableQuasiHttpApplication
            {
                ProcessRequestCallback = (req) =>
                {
                    return Task.FromResult((IQuasiHttpResponse)expectedResponse);
                }
            };
            var instance = new DefaultReceiveProtocolInternal
            {
                Application = app,
                Transport = transport,
                MaxChunkSize = 50,
                Connection = connection
            };

            // set up expected response headers
            var expectedResChunk = CustomChunkedTransferCodec.CreateFromResponse(expectedResponse);

            await Assert.ThrowsAsync<NotImplementedException>(() =>
            {
                return instance.Receive();
            });

            Assert.Equal(1, responseReleaseCallCount);
            Assert.Equal(0, transport.ReleaseCallCount);

            // assert written response
            headerReceiver.Position = 0;
            var actualResChunk = await new CustomChunkedTransferCodec().ReadLeadChunk(
                headerReceiver);
            // verify all contents of headerReceiver was used
            // before comparing lead chunks
            Assert.Equal(0, await IOUtils.ReadBytes(headerReceiver,
                new byte[1], 0, 1));
            ComparisonUtils.CompareLeadChunks(expectedResChunk, actualResChunk);

            await instance.Cancel();
            Assert.Equal(1, transport.ReleaseCallCount);
        }

        [Theory]
        [MemberData(nameof(CreateTestReceiveData))]
        public async Task TestReceive(object connection, int maxChunkSize,
            IQuasiHttpRequest request, byte[] requestBodyBytes, IDictionary<string, object> reqEnv,
            DefaultQuasiHttpResponse expectedResponse, byte[] expectedResponseBodyBytes)
        {
            // prepare request for reading.
            var helpingReader = await SerializeRequestToBeRead(
                request, requestBodyBytes);

            // prepare to receive response to be written
            var headerReceiver = new MemoryStream();
            var bodyReceiver = new MemoryStream();
            var helpingWriter = SetUpReceivingOfResponseToBeWritten(
                expectedResponse, expectedResponseBodyBytes, headerReceiver,
                bodyReceiver);

            // set up instance
            var transport = new DemoQuasiHttpTransport(connection,
                helpingReader, helpingWriter);
            IQuasiHttpRequest actualRequest = null;
            var application = new ConfigurableQuasiHttpApplication
            {
                ProcessRequestCallback = req =>
                {
                    actualRequest = req;
                    return Task.FromResult<IQuasiHttpResponse>(expectedResponse);
                }
            };
            var instance = new DefaultReceiveProtocolInternal
            {
                Application = application,
                Transport = transport,
                Connection = connection,
                MaxChunkSize = maxChunkSize,
                RequestEnvironment = reqEnv
            };

            // set up expected response headers
            var expectedResChunk = CustomChunkedTransferCodec.CreateFromResponse(expectedResponse);

            // act
            var recvResult = await instance.Receive();
 
            // begin assert.
            Assert.Null(recvResult);
            Assert.Equal(0, transport.ReleaseCallCount);

            // assert read request.
            await ComparisonUtils.CompareRequests(request, actualRequest,
                requestBodyBytes);
            Assert.Equal(reqEnv, actualRequest.Environment);

            // assert written response
            headerReceiver.Position = 0;
            var actualResChunk = await new CustomChunkedTransferCodec().ReadLeadChunk(
                headerReceiver, CustomChunkedTransferCodec.HardMaxChunkSizeLimit);
            // verify all contents of headerReceiver was used
            // before comparing lead chunks
            Assert.Equal(0, await IOUtils.ReadBytes(headerReceiver,
                new byte[1], 0, 1));
            ComparisonUtils.CompareLeadChunks(expectedResChunk, actualResChunk);

            bodyReceiver.Position = 0;
            object resBodyReader = bodyReceiver;
            if (expectedResChunk.ContentLength < 0)
            {
                resBodyReader = new ChunkDecodingCustomReader(resBodyReader);
            }
            var actualResBodyBytes = await IOUtils.ReadAllBytes(resBodyReader);
            if (expectedResponse.Body != null)
            {
                Assert.Equal(expectedResponseBodyBytes, actualResBodyBytes);
            }
            else
            {
                Assert.Empty(actualResBodyBytes);
            }

            // verify cancel expectations
            await instance.Cancel();
            Assert.Equal(1, transport.ReleaseCallCount);
        }

        public static List<object[]> CreateTestReceiveData()
        {
            var testData = new List<object[]>();

            // NB: all request bodies are specified with LambdaBasedQuasiHttpBody class
            // through just the ContentLength property.
            // body will be created as an ISelfWritable from any reqBodyBytes
            // as long as ContentLength is not zero.

            // next...
            object connection = "vgh";
            int maxChunkSize = 100;
            IDictionary<string, object> reqEnv = null;
            var request = new DefaultQuasiHttpRequest
            {
                Method = "POST",
                Target = "/koobi",
                Headers = new Dictionary<string, IList<string>>
                {
                    { "variant", new List<string>{ "sea", "drive" } }
                }
            };
            var reqBodyBytes = ByteUtils.StringToBytes("this is our king");
            request.Body = new ByteBufferBody(reqBodyBytes);

            var expectedResponse = new DefaultQuasiHttpResponse
            {
                StatusCode = 200,
                HttpStatusMessage = "ok",
                Headers = new Dictionary<string, IList<string>>
                {
                    { "dkt", new List<string>{ "bb" } }
                },
            };
            byte[] expectedResBodyBytes = ByteUtils.StringToBytes("and this is our queen");
            expectedResponse.Body = new LambdaBasedQuasiHttpBody
            {
                ContentLength = expectedResBodyBytes.Length
            };
            testData.Add(new object[] { connection, maxChunkSize, request, reqBodyBytes, reqEnv,
                expectedResponse, expectedResBodyBytes });

            // next...
            connection = 123;
            maxChunkSize = 95;
            reqEnv = new Dictionary<string, object>
            {
                { "is_ssl", "true" }
            };
            request = new DefaultQuasiHttpRequest
            {
                Target = "/p"
            };
            reqBodyBytes = null;

            expectedResponse = new DefaultQuasiHttpResponse
            {
                StatusCode = 400,
                HttpStatusMessage = "not found"
            };
            expectedResBodyBytes = null;
            testData.Add(new object[] { connection, maxChunkSize, request, reqBodyBytes, reqEnv,
                expectedResponse, expectedResBodyBytes });

            // next...
            connection = null;
            maxChunkSize = 90;
            reqEnv = new Dictionary<string, object>();
            request = new DefaultQuasiHttpRequest
            {
                HttpVersion = "1.1",
                Target = "/bread"
            };
            reqBodyBytes = ByteUtils.StringToBytes("<a>this is news</a>");
            request.Body = new ByteBufferBody(reqBodyBytes)
            {
                ContentLength = -1
            };

            expectedResponse = new DefaultQuasiHttpResponse
            {
                HttpVersion = "1.1",
                StatusCode = 500,
                HttpStatusMessage = "server error"
            };
            expectedResBodyBytes = null;
            testData.Add(new object[] { connection, maxChunkSize, request, reqBodyBytes, reqEnv,
                expectedResponse, expectedResBodyBytes });

            // next...
            connection = new object();
            maxChunkSize = 150;
            reqEnv = new Dictionary<string, object>
            {
                { "r", 2 }, { "tea", new byte[3] }
            };
            request = new DefaultQuasiHttpRequest
            {
                Target = "/fxn",
                Headers = new Dictionary<string, IList<string>>
                {
                    { "x", new List<string>() },
                    { "a", new List<string>{ "A" } },
                    { "bb", new List<string>{ "B1", "B2" } },
                    { "ccc", new List<string>{ "C1", "C2", "C3" } }
                }
            };
            reqBodyBytes = null;

            expectedResponse = new DefaultQuasiHttpResponse
            {
                StatusCode = 200,
                HttpStatusMessage = "ok",
                Headers = new Dictionary<string, IList<string>>
                {
                    { "x", new List<string>{ "A" } },
                    { "y", new List<string>{ "B1", "B2", "C1", "C2", "C3" } }
                }
            };
            expectedResBodyBytes = ByteUtils.StringToBytes("<a>this is news</a>");
            expectedResponse.Body = new LambdaBasedQuasiHttpBody
            {
                ContentLength = expectedResBodyBytes.Length
            };
            testData.Add(new object[] { connection, maxChunkSize, request, reqBodyBytes, reqEnv,
                expectedResponse, expectedResBodyBytes });

            // next...
            connection = new List<object>();
            maxChunkSize = 150_000;
            reqEnv = null;
            request = new DefaultQuasiHttpRequest
            {
                Target = "/fxn".PadLeft(70_000)
            };
            reqBodyBytes = new byte[80_000];
            request.Body = new ByteBufferBody(reqBodyBytes)
            {
                ContentLength = -1
            };

            expectedResponse = new DefaultQuasiHttpResponse
            {
                HttpStatusMessage = "ok".PadLeft(90_000),
            };
            expectedResBodyBytes = new byte[100_000];
            expectedResponse.Body = new LambdaBasedQuasiHttpBody
            {
                ContentLength = -1
            };
            testData.Add(new object[] { connection, maxChunkSize, request, reqBodyBytes, reqEnv,
                expectedResponse, expectedResBodyBytes });

            return testData;
        }

        [Fact]
        public async Task TestReceiveInvolvingNotSendingResponse()
        {
            var connection = "fire and forget example";
            var request = new DefaultQuasiHttpRequest();

            var helpingReader = await SerializeRequestToBeRead(
                request, null);
            var headerReceiver = new MemoryStream();

            var transport = new DemoQuasiHttpTransport(connection, helpingReader,
                headerReceiver);
            var responseReleaseCallCount = 0;
            var expectedResponse = new ConfigurableQuasiHttpResponse
            {
                Environment = new Dictionary<string, object>
                {
                    { QuasiHttpUtils.ResEnvKeySkipResponseSending, true }
                },
                ReleaseFunc = async () =>
                {
                    responseReleaseCallCount++;
                }
            };
            var app = new ConfigurableQuasiHttpApplication
            {
                ProcessRequestCallback = (req) =>
                {
                    return Task.FromResult((IQuasiHttpResponse)expectedResponse);
                }
            };
            var instance = new DefaultReceiveProtocolInternal
            {
                Application = app,
                Transport = transport,
                MaxChunkSize = 50,
                Connection = connection
            };

            var recvResult = await instance.Receive();
            Assert.Null(recvResult);
            Assert.Equal(1, responseReleaseCallCount);
            Assert.Equal(0, transport.ReleaseCallCount);
            Assert.Empty(headerReceiver.ToArray());

            await instance.Cancel();
            Assert.Equal(1, transport.ReleaseCallCount);
        }

        [Fact]
        public async Task TestRequestHeadersExceedMaxChunkSizeError()
        {
            object connection = new List<object>();
            var request = new DefaultQuasiHttpRequest
            {
                Target = "/fxn".PadLeft(70_000)
            };
            var reqStream = await CreateRequestStream(request,
                100_000, 0);
            var application = new ConfigurableQuasiHttpApplication
            {
                ProcessRequestCallback = async (request) =>
                {
                    return new DefaultQuasiHttpResponse();
                }
            };
            var resStream = new LambdaBasedCustomReaderWriter
            {
                WriteFunc = (data, offset, length)
                    => Task.CompletedTask
            };
            var transport = new DemoQuasiHttpTransport(connection,
                reqStream, resStream);
            var instance = new DefaultReceiveProtocolInternal
            {
                Application = application,
                Transport = transport,
                Connection = connection,
                MaxChunkSize = 65_000
            };

            var actualEx = await Assert.ThrowsAsync<ChunkDecodingException>(() =>
                instance.Receive());
            Assert.Contains("quasi http headers", actualEx.Message);
            Assert.NotNull(actualEx.InnerException);
            Assert.Contains("chunk size exceeding max", actualEx.InnerException.Message);
        }

        [Fact]
        public async Task TestResponseHeadersExceedMaxChunkSizeError()
        {
            object connection = new List<object>();
            var request = new DefaultQuasiHttpRequest();
            var reqStream = await CreateRequestStream(request,
                0, 0);
            var application = new ConfigurableQuasiHttpApplication
            {
                ProcessRequestCallback = async (request) =>
                {
                    return new DefaultQuasiHttpResponse
                    {
                        Headers = new Dictionary<string, IList<string>>
                        {
                            { "h1", new List<string>{ "1".PadLeft(90) } }
                        }
                    };
                }
            };
            var resStream = new LambdaBasedCustomReaderWriter
            {
                WriteFunc = (data, offset, length)
                    => Task.CompletedTask
            };
            var transport = new DemoQuasiHttpTransport(connection,
                reqStream, resStream);
            var instance = new DefaultReceiveProtocolInternal
            {
                Application = application,
                Transport = transport,
                Connection = connection,
                MaxChunkSize = 81
            };

            var actualEx = await Assert.ThrowsAsync<ChunkEncodingException>(() =>
                instance.Receive());
            Assert.Contains("quasi http headers exceed max", actualEx.Message);
        }
    }
}
