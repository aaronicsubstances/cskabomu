using Kabomu.Common;
using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.ChunkedTransfer;
using Kabomu.QuasiHttp.Client;
using Kabomu.QuasiHttp.EntityBody;
using Kabomu.Tests.Shared.Common;
using Kabomu.Tests.Shared.QuasiHttp;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.QuasiHttp.Client
{
    public class DefaultSendProtocolInternalTest
    {
        public DefaultSendProtocolInternalTest()
        {
            
        }

        private static ICustomWriter SetUpReceivingOfRequestToBeWritten(
            IQuasiHttpMutableRequest request, ICustomWritable delegateWritableForBody,
            MemoryStream headerReceiver, MemoryStream bodyReceiver)
        {
            var backingWriters = new List<ICustomWriter>();
            var helpingWriter = new SequenceCustomWriter
            {
                Writers = backingWriters
            };
            backingWriters.Add(new StreamCustomReaderWriter(headerReceiver));
            if ((request.Body?.ContentLength ?? 0) != 0)
            {
                backingWriters.Add(new StreamCustomReaderWriter(bodyReceiver));
                // replace DummyQuasiHttpBody with real body.
                var writable = new LambdaBasedCustomWritable
                {
                    WritableFunc = async writer =>
                    {
                        helpingWriter.SwitchOver();
                        await delegateWritableForBody.WriteBytesTo(writer);
                    }
                };
                request.Body = new CustomWritableBackedBody(writable)
                {
                    ContentLength = request.Body.ContentLength,
                    ContentType = request.Body.ContentType
                };
            }
            return helpingWriter;
        }

        private static async Task<SequenceCustomReader> SerializeResponseToBeRead(
            IQuasiHttpResponse res, byte[] resBodyBytes)
        {
            var resChunk = new LeadChunk
            {
                Version = LeadChunk.Version01,
                HttpVersion = res.HttpVersion,
                StatusCode = res.StatusCode,
                HttpStatusMessage = res.HttpStatusMessage,
                Headers = res.Headers,
                ContentLength = res.Body?.ContentLength ?? 0,
                ContentType = res.Body?.ContentType
            };
            var helpingReaders = new List<ICustomReader>();
            var headerStream = new MemoryStream();
            var headerReader = new StreamCustomReaderWriter(headerStream);
            await ChunkedTransferUtils.WriteLeadChunk(headerReader,
                resChunk);
            headerStream.Position = 0; // reset for reading.
            helpingReaders.Add(headerReader);
            if (res.Body != null)
            {
                var resBodyStream = new MemoryStream();
                ICustomWriter resBodyWriter = new StreamCustomReaderWriter(
                    resBodyStream);
                if (resChunk.ContentLength < 0)
                {
                    resBodyWriter = new ChunkEncodingCustomWriter(resBodyWriter);
                }
                await resBodyWriter.WriteBytes(resBodyBytes, 0, resBodyBytes.Length);
                await resBodyWriter.CustomDispose(); // will dispose resBodyStream as well
                helpingReaders.Add(new StreamCustomReaderWriter(
                    new MemoryStream(resBodyStream.ToArray())));
            }
            return new SequenceCustomReader
            {
                Readers = helpingReaders
            };
        }

        [Fact]
        public async Task TestSendForDependencyErrors()
        {
            await Assert.ThrowsAsync<MissingDependencyException>(() =>
            {
                var instance = new DefaultSendProtocolInternal
                {
                    Request = new DefaultQuasiHttpRequest()
                };
                return instance.Send();
            });
            await Assert.ThrowsAsync<ExpectationViolationException>(() =>
            {
                var instance = new DefaultSendProtocolInternal
                {
                    Transport = new DemoQuasiHttpTransport(null)
                };
                return instance.Send();
            });
        }

        [Theory]
        [MemberData(nameof(CreateTestSendData))]
        public async Task TestSend(
            object connection, int maxChunkSize, bool responseBufferingEnabled,
            IQuasiHttpMutableRequest request, byte[] expectedReqBodyBytes,
            IQuasiHttpResponse expectedResponse, byte[] expectedResBodyBytes)
        {
            // prepare response for reading.
            var helpingReader = await SerializeResponseToBeRead(
                expectedResponse, expectedResBodyBytes);

            // prepare to receive request to be written
            var headerReceiver = new MemoryStream();
            var bodyReceiver = new MemoryStream();
            ICustomWritable bodyWritable = null;
            if (expectedReqBodyBytes != null)
            {
                bodyWritable = new LambdaBasedCustomWritable
                {
                    WritableFunc = async (writer) =>
                    {
                        await writer.WriteBytes(expectedReqBodyBytes, 0,
                            expectedReqBodyBytes.Length);
                    }
                };
            }
            var helpingWriter = SetUpReceivingOfRequestToBeWritten(
                request, bodyWritable, headerReceiver,
                bodyReceiver);

            // set up instance
            var transport = new DemoQuasiHttpTransport2(connection,
                helpingReader, helpingWriter)
            {
                ReleaseIndicator = new CancellationTokenSource()
            };
            var instance = new DefaultSendProtocolInternal
            {
                Request = request,
                Transport = transport,
                Connection = connection,
                MaxChunkSize = maxChunkSize,
                ResponseBufferingEnabled = responseBufferingEnabled,
                ResponseBodyBufferingSizeLimit = 100,
                EnsureNonNullResponse = true
            };

            // set up expected request headers
            var expectedReqChunk = new LeadChunk
            {
                Version = LeadChunk.Version01,
                Method = request.Method,
                RequestTarget = request.Target,
                HttpVersion = request.HttpVersion,
                Headers = request.Headers,
                ContentLength = request.Body?.ContentLength ?? 0,
                ContentType = request.Body?.ContentType,
            };

            // act.
            var actualResponse = await instance.Send();
            var wasTransportReleased = transport.ReleaseIndicator.IsCancellationRequested;

            // begin assert.
            Assert.NotNull(actualResponse);
            Assert.NotNull(actualResponse.Response);
            Assert.Equal(responseBufferingEnabled, actualResponse.ResponseBufferingApplied);

            // assert read response.
            await ComparisonUtils.CompareResponses(expectedResponse, actualResponse.Response,
                expectedResBodyBytes);

            // assert written request, and work around disposed
            // request receiving streams.
            ICustomReader reqHeaderReader = new StreamCustomReaderWriter(
                new MemoryStream(headerReceiver.ToArray()));
            var actualReqChunk = await ChunkedTransferUtils.ReadLeadChunk(
                reqHeaderReader, 0);
            // verify all contents of headerReceiver was used
            // before comparing lead chunks
            Assert.Equal(0, await reqHeaderReader.ReadBytes(new byte[1], 0, 1));
            ComparisonUtils.CompareLeadChunks(expectedReqChunk, actualReqChunk);

            ICustomReader reqBodyReader = new StreamCustomReaderWriter(
                new MemoryStream(bodyReceiver.ToArray()));
            if (expectedReqChunk.ContentLength < 0)
            {
                reqBodyReader = new ChunkDecodingCustomReader(reqBodyReader);
            }
            var actualReqBodyBytes = await IOUtils.ReadAllBytes(reqBodyReader);
            if (request.Body == null)
            {
                Assert.Empty(actualReqBodyBytes);
            }
            else
            {
                Assert.Equal(expectedReqBodyBytes, actualReqBodyBytes);
            }

            // verify cancel expectations
            if (expectedResponse.Body != null)
            {
                Assert.Equal(responseBufferingEnabled, wasTransportReleased);
            }
            else
            {
                Assert.False(wasTransportReleased);
            }
            await instance.Cancel();
            Assert.True(transport.ReleaseIndicator.IsCancellationRequested);
        }

        public static List<object[]> CreateTestSendData()
        {
            var testData = new List<object[]>();

            // next...
            object connection = "vgh";
            int maxChunkSize = 115;
            bool responseBufferingEnabled = true;
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
            request.Body = new DummyQuasiHttpBody
            {
                ContentLength = reqBodyBytes.Length,
                ContentType = "text/plain"
            };

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
            expectedResponse.Body = new ByteBufferBody(expectedResBodyBytes)
            {
                ContentType = "image/png"
            };
            testData.Add(new object[] { connection, maxChunkSize, responseBufferingEnabled, request, reqBodyBytes,
                expectedResponse, expectedResBodyBytes });

            // next...
            connection = 123;
            maxChunkSize = 90;
            responseBufferingEnabled = false;
            request = new DefaultQuasiHttpRequest
            {
                Target = "/p"
            };
            reqBodyBytes = null;

            expectedResponse = new DefaultQuasiHttpResponse
            {
                StatusCode = DefaultQuasiHttpResponse.StatusCodeClientError,
                HttpStatusMessage = "not found"
            };
            expectedResBodyBytes = null;
            testData.Add(new object[] { connection, maxChunkSize, responseBufferingEnabled, request, reqBodyBytes,
                expectedResponse, expectedResBodyBytes });

            // next...
            connection = new object();
            maxChunkSize = 100;
            responseBufferingEnabled = true;
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
                StatusCode = DefaultQuasiHttpResponse.StatusCodeServerError,
                HttpStatusMessage = "server error",
                Headers = new Dictionary<string, IList<string>>
                {
                    { "x", new List<string>{ "A" } },
                    { "y", new List<string>{ "B1", "B2", "C1", "C2", "C3" } }
                }
            };
            expectedResBodyBytes = ByteUtils.StringToBytes("<a>this is news</a>");
            expectedResponse.Body = new ByteBufferBody(expectedResBodyBytes)
            {
                ContentLength = -1,
                ContentType = "application/xml"
            };
            testData.Add(new object[] { connection, maxChunkSize, responseBufferingEnabled, request, reqBodyBytes,
                expectedResponse, expectedResBodyBytes });

            // next...
            // zero content length in request
            connection = "..";
            maxChunkSize = 50;
            responseBufferingEnabled = false;
            request = new DefaultQuasiHttpRequest();
            reqBodyBytes = new byte[0];
            request.Body = new DummyQuasiHttpBody
            {
                ContentLength = 0
            };

            expectedResponse = new DefaultQuasiHttpResponse
            {
                StatusCode = 200,
                Headers = new Dictionary<string, IList<string>>()
            };
            expectedResBodyBytes = new byte[1];
            expectedResponse.Body = new ByteBufferBody(expectedResBodyBytes);
            testData.Add(new object[] { connection, maxChunkSize, responseBufferingEnabled, request, reqBodyBytes,
                expectedResponse, expectedResBodyBytes });

            // next...
            // exceed buffering limit of 100 specified in test method
            connection = true;
            maxChunkSize = 40;
            responseBufferingEnabled = false;
            request = new DefaultQuasiHttpRequest();
            reqBodyBytes = null;

            expectedResponse = new DefaultQuasiHttpResponse
            {
                StatusCode = 200,
                Headers = new Dictionary<string, IList<string>>()
            };
            expectedResBodyBytes = ByteUtils.StringToBytes("dk".PadRight(120));
            expectedResponse.Body = new ByteBufferBody(expectedResBodyBytes)
            {
                ContentLength = -1,
                ContentType = "text/plain"
            };
            testData.Add(new object[] { connection, maxChunkSize, responseBufferingEnabled, request, reqBodyBytes,
                expectedResponse, expectedResBodyBytes });

            return testData;
        }

        [Fact]
        public async Task TestSendForAbortOnRequestBodyReadError()
        {
            // arrange.
            object connection = "drew";
            int maxChunkSize = 80;
            var request = new DefaultQuasiHttpRequest();

            // prepare response for reading.
            var helpingReader = new LambdaBasedCustomReader
            {
                ReadFunc = (data, offset, length) =>
                {
                    // wait forever
                    return new TaskCompletionSource<int>().Task;
                }
            };

            // prepare to receive request to be written
            var headerReceiver = new MemoryStream();
            var backingWriters = new List<ICustomWriter>();
            backingWriters.Add(new StreamCustomReaderWriter(headerReceiver));
            backingWriters.Add(new LambdaBasedCustomWriter
            {
                WriteFunc = (data, offset, length) =>
                {
                    throw new NotImplementedException();
                }
            });
            var helpingWriter = new SequenceCustomWriter
            {
                Writers = backingWriters
            };
            var writable = new LambdaBasedCustomWritable
            {
                WritableFunc = async writer =>
                {
                    helpingWriter.SwitchOver();
                    await writer.WriteBytes(new byte[1], 0, 1);
                }
            };
            request.Body = new CustomWritableBackedBody(writable)
            {
                ContentLength = -1
            };

            // set up instance
            var transport = new DemoQuasiHttpTransport2(connection,
                helpingReader, helpingWriter)
            {
                ReleaseIndicator = new CancellationTokenSource()
            };
            var instance = new DefaultSendProtocolInternal
            {
                Request = request,
                Transport = transport,
                Connection = connection,
                MaxChunkSize = maxChunkSize
            };

            // set up expected request headers
            var expectedReqChunk = new LeadChunk
            {
                Version = LeadChunk.Version01,
                Method = request.Method,
                RequestTarget = request.Target,
                HttpVersion = request.HttpVersion,
                Headers = request.Headers,
                ContentLength = request.Body?.ContentLength ?? 0,
                ContentType = request.Body?.ContentType,
            };

            // act.
            await Assert.ThrowsAsync<NotImplementedException>(() =>
                instance.Send());

            // assert written request, and work around disposed
            // request receiving streams.
            ICustomReader reqHeaderReader = new StreamCustomReaderWriter(
                new MemoryStream(headerReceiver.ToArray()));
            var actualReqChunk = await ChunkedTransferUtils.ReadLeadChunk(
                reqHeaderReader, 0);
            // verify all contents of headerReceiver was used
            // before comparing lead chunks
            Assert.Equal(0, await reqHeaderReader.ReadBytes(new byte[1], 0, 1));
            ComparisonUtils.CompareLeadChunks(expectedReqChunk, actualReqChunk);

            await instance.Cancel();
            Assert.True(transport.ReleaseIndicator.IsCancellationRequested);
        }

        [Fact]
        public async Task TestSendForAbortOnResponseBodyReadError()
        {
            // arrange.
            object connection = "drew";
            int maxChunkSize = 80;
            var request = new DefaultQuasiHttpRequest();
            var responseBodyBytes = ByteUtils.StringToBytes("dkd".PadLeft(50));
            var response = new DefaultQuasiHttpResponse
            {
                Body = new ByteBufferBody(responseBodyBytes)
            };

            // prepare response for reading.
            var helpingReader = await SerializeResponseToBeRead(
                response, responseBodyBytes);

            // prepare to receive request to be written
            var headerReceiver = new MemoryStream();
            var backingWriters = new List<ICustomWriter>();
            backingWriters.Add(new StreamCustomReaderWriter(headerReceiver));
            var helpingWriter = new SequenceCustomWriter
            {
                Writers = backingWriters
            };

            // set up instance
            var transport = new DemoQuasiHttpTransport2(connection,
                helpingReader, helpingWriter)
            {
                ReleaseIndicator = new CancellationTokenSource()
            };
            IDictionary<string, object> actualReqEnv = null;
            var instance = new DefaultSendProtocolInternal
            {
                Request = request,
                Transport = transport,
                Connection = connection,
                MaxChunkSize = maxChunkSize,
                ResponseBufferingEnabled = true,
                ResponseBodyBufferingSizeLimit = 40
            };

            // set up expected request headers
            var expectedReqChunk = new LeadChunk
            {
                Version = LeadChunk.Version01,
                Method = request.Method,
                RequestTarget = request.Target,
                HttpVersion = request.HttpVersion,
                Headers = request.Headers,
                ContentLength = request.Body?.ContentLength ?? 0,
                ContentType = request.Body?.ContentType,
            };

            // act.
            var actualEx = await Assert.ThrowsAsync<CustomIOException>(() => instance.Send());
            Assert.Contains("limit of", actualEx.Message);

            Assert.Null(actualReqEnv);

            // assert written request, and work around disposed
            // request receiving streams.
            ICustomReader reqHeaderReader = new StreamCustomReaderWriter(
                new MemoryStream(headerReceiver.ToArray()));
            var actualReqChunk = await ChunkedTransferUtils.ReadLeadChunk(
                reqHeaderReader, 0);
            // verify all contents of headerReceiver was used
            // before comparing lead chunks
            Assert.Equal(0, await reqHeaderReader.ReadBytes(new byte[1], 0, 1));
            ComparisonUtils.CompareLeadChunks(expectedReqChunk, actualReqChunk);

            await instance.Cancel();
            Assert.True(transport.ReleaseIndicator.IsCancellationRequested);
        }

        [Fact]
        public async Task TestSendForNullResponse1()
        {
            // arrange.
            object connection = "127.pcid";
            int maxChunkSize = 800;
            var request = new DefaultQuasiHttpRequest();

            // prepare to receive request to be written
            var headerReceiver = new MemoryStream();
            var backingWriters = new List<ICustomWriter>();
            backingWriters.Add(new StreamCustomReaderWriter(headerReceiver));
            var helpingWriter = new SequenceCustomWriter
            {
                Writers = backingWriters
            };

            var helpingReader = new SequenceCustomReader();

            // set up instance
            var transport = new DemoQuasiHttpTransport2(connection,
                helpingReader, helpingWriter)
            {
                ReleaseIndicator = new CancellationTokenSource()
            };
            var instance = new DefaultSendProtocolInternal
            {
                Request = request,
                Transport = transport,
                Connection = connection,
                MaxChunkSize = maxChunkSize,
                ResponseBufferingEnabled = true,
                ResponseBodyBufferingSizeLimit = 40,
                EnsureNonNullResponse = false
            };

            // set up expected request headers
            var expectedReqChunk = new LeadChunk
            {
                Version = LeadChunk.Version01,
                Method = request.Method,
                RequestTarget = request.Target,
                HttpVersion = request.HttpVersion,
                Headers = request.Headers,
                ContentLength = request.Body?.ContentLength ?? 0,
                ContentType = request.Body?.ContentType,
            };

            // act.
            var actualResponse = await instance.Send();

            // assert
            Assert.Null(actualResponse);

            // assert written request, and work around disposed
            // request receiving streams.
            ICustomReader reqHeaderReader = new StreamCustomReaderWriter(
                new MemoryStream(headerReceiver.ToArray()));
            var actualReqChunk = await ChunkedTransferUtils.ReadLeadChunk(
                reqHeaderReader, 0);
            // verify all contents of headerReceiver was used
            // before comparing lead chunks
            Assert.Equal(0, await reqHeaderReader.ReadBytes(new byte[1], 0, 1));
            ComparisonUtils.CompareLeadChunks(expectedReqChunk, actualReqChunk);

            await instance.Cancel();
            Assert.True(transport.ReleaseIndicator.IsCancellationRequested);
        }

        [Fact]
        public async Task TestSendForNullResponse2()
        {
            // arrange.
            object connection = "127.xct";
            int maxChunkSize = 8000;
            var request = new DefaultQuasiHttpRequest();

            // prepare to receive request to be written
            var headerReceiver = new MemoryStream();
            var backingWriters = new List<ICustomWriter>();
            backingWriters.Add(new StreamCustomReaderWriter(headerReceiver));
            var helpingWriter = new SequenceCustomWriter
            {
                Writers = backingWriters
            };

            var helpingReader = new SequenceCustomReader();

            // set up instance
            var transport = new DemoQuasiHttpTransport2(connection,
                helpingReader, helpingWriter)
            {
                ReleaseIndicator = new CancellationTokenSource()
            };
            var instance = new DefaultSendProtocolInternal
            {
                Request = request,
                Transport = transport,
                Connection = connection,
                MaxChunkSize = maxChunkSize,
                ResponseBufferingEnabled = true,
                ResponseBodyBufferingSizeLimit = 40,
                EnsureNonNullResponse = false
            };

            // set up expected request headers
            var expectedReqChunk = new LeadChunk
            {
                Version = LeadChunk.Version01,
                Method = request.Method,
                RequestTarget = request.Target,
                HttpVersion = request.HttpVersion,
                Headers = request.Headers,
                ContentLength = request.Body?.ContentLength ?? 0,
                ContentType = request.Body?.ContentType,
            };

            // act.
            var actualEx = await Assert.ThrowsAsync<QuasiHttpRequestProcessingException>(() =>
                instance.Send());

            // assert
            Assert.Contains("no response", actualEx.Message);

            // assert written request, and work around disposed
            // request receiving streams.
            ICustomReader reqHeaderReader = new StreamCustomReaderWriter(
                new MemoryStream(headerReceiver.ToArray()));
            var actualReqChunk = await ChunkedTransferUtils.ReadLeadChunk(
                reqHeaderReader, 0);
            // verify all contents of headerReceiver was used
            // before comparing lead chunks
            Assert.Equal(0, await reqHeaderReader.ReadBytes(new byte[1], 0, 1));
            ComparisonUtils.CompareLeadChunks(expectedReqChunk, actualReqChunk);

            await instance.Cancel();
            Assert.True(transport.ReleaseIndicator.IsCancellationRequested);
        }

        [Fact]
        public async Task TestSendForRequestBodyTransferIfResponseHasNoBody()
        {
            var connection = "sth";
            var maxChunkSize = 95;
            var responseBufferingEnabled = true;
            var reqEnv = new Dictionary<string, object>
            {
                { "1", "two" },
                { "two", "3" }
            };
            var request = new DefaultQuasiHttpRequest
            {
                HttpVersion = "1.1",
                Target = "/bread"
            };
            var reqBodyBytes = ByteUtils.StringToBytes("<a>this is news</a>");
            request.Body = new DummyQuasiHttpBody
            {
                ContentLength = reqBodyBytes.Length,
                ContentType = "application/xml"
            };

            var expectedResponse = new DefaultQuasiHttpResponse
            {
                HttpVersion = "1.0",
                StatusCode = DefaultQuasiHttpResponse.StatusCodeOk,
                HttpStatusMessage = "ok"
            };
            // prepare response for reading.
            var helpingReader = await SerializeResponseToBeRead(
                expectedResponse, null);
            var memoryPipe = new MemoryPipeCustomReaderWriter();
            helpingReader.Readers.Insert(0, memoryPipe);

            // prepare to receive request to be written
            var headerReceiver = new MemoryStream();
            var bodyReceiver = new MemoryStream();
            var bodyWritable = new LambdaBasedCustomWritable
            {
                WritableFunc = async (writer) =>
                {
                    await Task.Delay(200);
                    await memoryPipe.DeferCustomDispose(() =>
                        writer.WriteBytes(reqBodyBytes, 0,
                            reqBodyBytes.Length));
                }
            };
            var helpingWriter = SetUpReceivingOfRequestToBeWritten(
                request, bodyWritable, headerReceiver,
                bodyReceiver);

            // set up instance
            var transport = new DemoQuasiHttpTransport2(connection,
                helpingReader, helpingWriter)
            {
                ReleaseIndicator = new CancellationTokenSource()
            };
            var instance = new DefaultSendProtocolInternal
            {
                Request = request,
                Transport = transport,
                Connection = connection,
                MaxChunkSize = maxChunkSize,
                ResponseBufferingEnabled = responseBufferingEnabled,
                ResponseBodyBufferingSizeLimit = 100,
                EnsureNonNullResponse = true
            };

            // set up expected request headers
            var expectedReqChunk = new LeadChunk
            {
                Version = LeadChunk.Version01,
                Method = request.Method,
                RequestTarget = request.Target,
                HttpVersion = request.HttpVersion,
                Headers = request.Headers,
                ContentLength = request.Body?.ContentLength ?? 0,
                ContentType = request.Body?.ContentType,
            };

            // act.
            var actualResponse = await instance.Send();
            var wasTransportReleased = transport.ReleaseIndicator.IsCancellationRequested;

            // begin assert.
            Assert.NotNull(actualResponse);
            Assert.NotNull(actualResponse.Response);
            Assert.Equal(responseBufferingEnabled, actualResponse.ResponseBufferingApplied);

            // assert read response.
            await ComparisonUtils.CompareResponses(expectedResponse, actualResponse.Response,
                null);

            // assert written request, and work around disposed
            // request receiving streams.
            ICustomReader reqHeaderReader = new StreamCustomReaderWriter(
                new MemoryStream(headerReceiver.ToArray()));
            var actualReqChunk = await ChunkedTransferUtils.ReadLeadChunk(
                reqHeaderReader, 0);
            // verify all contents of headerReceiver was used
            // before comparing lead chunks
            Assert.Equal(0, await reqHeaderReader.ReadBytes(new byte[1], 0, 1));
            ComparisonUtils.CompareLeadChunks(expectedReqChunk, actualReqChunk);

            ICustomReader reqBodyReader = new StreamCustomReaderWriter(
                new MemoryStream(bodyReceiver.ToArray()));
            var actualReqBodyBytes = await IOUtils.ReadAllBytes(reqBodyReader);
            Assert.Equal(reqBodyBytes, actualReqBodyBytes);

            // verify cancel expectations
            Assert.False(wasTransportReleased);
            await instance.Cancel();
            Assert.True(transport.ReleaseIndicator.IsCancellationRequested);
        }
    }
}
