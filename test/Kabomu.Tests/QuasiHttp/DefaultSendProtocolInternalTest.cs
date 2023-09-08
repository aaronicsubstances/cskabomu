using Kabomu.Common;
using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.ChunkedTransfer;
using Kabomu.QuasiHttp.Client;
using Kabomu.QuasiHttp.EntityBody;
using Kabomu.Tests.Shared.Common;
using Kabomu.Tests.Shared.QuasiHttp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.QuasiHttp
{
    public class DefaultSendProtocolInternalTest
    {
        public DefaultSendProtocolInternalTest()
        {

        }

        private static ICustomWriter SetUpReceivingOfRequestToBeWritten(
            IQuasiHttpMutableRequest request, ISelfWritable delegateWritableForBody,
            MemoryStream headerReceiver, MemoryStream bodyReceiver)
        {
            var backingWriters = new List<object>();
            var helpingWriter = new SequenceCustomWriter
            {
                Writers = backingWriters
            };
            backingWriters.Add(headerReceiver);
            if ((request.Body?.ContentLength ?? 0) != 0)
            {
                backingWriters.Add(bodyReceiver);
                // update body with writable.
                ((LambdaBasedQuasiHttpBody)request.Body).SelfWritable = new LambdaBasedCustomWritable
                {
                    WritableFunc = async writer =>
                    {
                        helpingWriter.SwitchOver();
                        await delegateWritableForBody.WriteBytesTo(writer);
                    }
                };
            }
            return helpingWriter;
        }

        private static async Task<ICustomReader> SerializeResponseToBeRead(
            IQuasiHttpResponse res, byte[] resBodyBytes)
        {
            var resChunk = CustomChunkedTransferCodec.CreateFromResponse(res);
            var helpingReaders = new List<object>();
            var headerStream = new MemoryStream();
            await new CustomChunkedTransferCodec().WriteLeadChunk(headerStream,
                resChunk, CustomChunkedTransferCodec.HardMaxChunkSizeLimit);
            headerStream.Position = 0; // reset for reading.
            helpingReaders.Add(headerStream);
            if (res.Body != null)
            {
                var resBodyStream = new MemoryStream();
                object resBodyWriter = resBodyStream;
                var endWrites = false;
                if (resChunk.ContentLength < 0)
                {
                    resBodyWriter = new ChunkEncodingCustomWriter(resBodyWriter);
                    endWrites = true;
                }
                await IOUtils.WriteBytes(resBodyWriter,
                    resBodyBytes, 0, resBodyBytes.Length);
                if (endWrites)
                {
                    await ((ChunkEncodingCustomWriter)resBodyWriter).EndWrites();
                }
                resBodyStream.Position = 0; // reset for reading.
                helpingReaders.Add(resBodyStream);
            }
            return new SequenceCustomReader
            {
                Readers = helpingReaders
            };
        }

        private static async Task<MemoryStream> CreateResponseStream(IQuasiHttpResponse res,
            int maxChunkSize, int bodyMaxChunkSize)
        {
            var resWriter = new MemoryStream();
            await new CustomChunkedTransferCodec().WriteLeadChunk(resWriter,
                CustomChunkedTransferCodec.CreateFromResponse(res), maxChunkSize);
            if (res.Body != null)
            {
                if (res.Body.ContentLength < 0)
                {
                    var encoder = new ChunkEncodingCustomWriter(resWriter,
                        bodyMaxChunkSize);
                    await res.Body.WriteBytesTo(encoder);
                    await encoder.EndWrites();
                }
                else
                {
                    await res.Body.WriteBytesTo(resWriter);
                }
            }
            resWriter.Position = 0; // reset for reading.
            return resWriter;
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
                    Transport = new DemoQuasiHttpTransport(null, null, null)
                };
                return instance.Send();
            });
        }

        [Fact]
        public async Task TestNoWriterForRequestHeadersError()
        {
            object connection = new object();
            var resStream = await CreateResponseStream(new DefaultQuasiHttpResponse(),
                0, 0);
            var transport = new DemoQuasiHttpTransport(connection,
                resStream, null);
            var instance = new DefaultSendProtocolInternal
            {
                Request = new DefaultQuasiHttpRequest(),
                Transport = transport,
                Connection = connection
            };

            var actualEx = await Assert.ThrowsAsync<QuasiHttpRequestProcessingException>(() =>
                instance.Send());
            Assert.Contains("no writer for connection", actualEx.Message);
        }

        [Fact]
        public async Task TestNoReaderForResponseHeadersError()
        {
            object connection = new List<object>();
            var reqStream = new LambdaBasedCustomReaderWriter
            {
                WriteFunc = (data, offset, length)
                    => Task.CompletedTask
            };
            var transport = new DemoQuasiHttpTransport(connection,
                null, reqStream);
            var instance = new DefaultSendProtocolInternal
            {
                Request = new DefaultQuasiHttpRequest(),
                Transport = transport,
                Connection = connection
            };

            var actualEx = await Assert.ThrowsAsync<QuasiHttpRequestProcessingException>(() =>
                instance.Send());
            Assert.Contains("no reader for connection", actualEx.Message);
        }

        [Theory]
        [MemberData(nameof(CreateTestSendData))]
        public async Task TestSend(
            object connection, int maxChunkSize, bool responseBufferingEnabled,
            IQuasiHttpMutableRequest expectedRequest,
            DefaultQuasiHttpResponse response, byte[] resBodyBytes)
        {
            // prepare response for reading.
            var helpingReader = await SerializeResponseToBeRead(
                response, resBodyBytes);

            // prepare to receive request to be written
            var headerReceiver = new MemoryStream();
            var bodyReceiver = new MemoryStream();
            var helpingWriter = SetUpReceivingOfRequestToBeWritten(
                expectedRequest, null, headerReceiver,
                bodyReceiver);

            // set up instance
            var transport = new DemoQuasiHttpTransport(connection,
                helpingReader, helpingWriter);
            var instance = new DefaultSendProtocolInternal
            {
                Request = expectedRequest,
                Transport = transport,
                Connection = connection,
                MaxChunkSize = maxChunkSize,
                ResponseBufferingEnabled = responseBufferingEnabled,
                ResponseBodyBufferingSizeLimit = 100,
                EnsureNonNullResponse = true
            };

            // set up expected request headers
            var expectedReqChunk = CustomChunkedTransferCodec.CreateFromRequest(expectedRequest);

            // act.
            var actualResponse = await instance.Send();

            // begin assert.
            Assert.NotNull(actualResponse);
            Assert.NotNull(actualResponse.Response);
            Assert.Equal(0, transport.ReleaseCallCount);
            Assert.Equal(responseBufferingEnabled, actualResponse.ResponseBufferingApplied);

            // assert read response.
            await ComparisonUtils.CompareResponses(response, actualResponse.Response,
                resBodyBytes);

            // assert written request.
            headerReceiver.Position = 0;
            var actualReqChunk = await new CustomChunkedTransferCodec().ReadLeadChunk(
                headerReceiver, CustomChunkedTransferCodec.HardMaxChunkSizeLimit);
            // verify all contents of headerReceiver was used
            // before comparing lead chunks
            Assert.Equal(0, await IOUtils.ReadBytes(headerReceiver,
                new byte[1], 0, 1));
            ComparisonUtils.CompareLeadChunks(expectedReqChunk, actualReqChunk);

            bodyReceiver.Position = 0;
            object reqBodyReader = bodyReceiver;
            if (expectedReqChunk.ContentLength < 0)
            {
                reqBodyReader = new ChunkDecodingCustomReader(bodyReceiver);
            }
            var actualReqBodyBytes = await IOUtils.ReadAllBytes(reqBodyReader);
            Assert.Empty(actualReqBodyBytes);

            // verify cancel expectations
            await instance.Cancel();
            await actualResponse.Response.Release();
            if (actualResponse.Response.Body != null &&
                !responseBufferingEnabled)
            {
                Assert.Equal(2, transport.ReleaseCallCount);
            }
            else
            {
                Assert.Equal(1, transport.ReleaseCallCount);
            }
        }

        public static List<object[]> CreateTestSendData()
        {
            var testData = new List<object[]>();

            // NB: all response bodies are specified with LambdaBasedQuasiHttpBody class
            // through just the ContentLength property.
            // body will be created as an ISelfWritable from any resBodyBytes
            // as long as ContentLength is not zero.

            // next...
            object connection = "vgh";
            int maxChunkSize = 115;
            bool responseBufferingEnabled = false;
            var expectedRequest = new DefaultQuasiHttpRequest
            {
                Method = "POST",
                Target = "/koobi",
                Headers = new Dictionary<string, IList<string>>
                {
                    { "variant", new List<string>{ "sea", "drive" } }
                }
            };

            var response = new DefaultQuasiHttpResponse
            {
                StatusCode = 200,
                HttpStatusMessage = "ok",
                Headers = new Dictionary<string, IList<string>>
                {
                    { "dkt", new List<string>{ "bb" } }
                },
            };
            byte[] resBodyBytes = MiscUtils.StringToBytes("and this is our queen");
            response.Body = new ByteBufferBody(resBodyBytes);
            testData.Add(new object[] { connection, maxChunkSize, responseBufferingEnabled, expectedRequest,
                response, resBodyBytes });

            // next...
            connection = 123;
            maxChunkSize = 90;
            responseBufferingEnabled = false;
            expectedRequest = new DefaultQuasiHttpRequest
            {
                Target = "/p"
            };

            response = new DefaultQuasiHttpResponse
            {
                StatusCode = 400,
                HttpStatusMessage = "not found"
            };
            resBodyBytes = null;
            testData.Add(new object[] { connection, maxChunkSize, responseBufferingEnabled, expectedRequest,
                response, resBodyBytes });

            // next...
            connection = new object();
            maxChunkSize = -1;
            responseBufferingEnabled = true;
            expectedRequest = new DefaultQuasiHttpRequest
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

            response = new DefaultQuasiHttpResponse
            {
                StatusCode = 500,
                HttpStatusMessage = "server error",
                Headers = new Dictionary<string, IList<string>>
                {
                    { "x", new List<string>{ "A" } },
                    { "y", new List<string>{ "B1", "B2", "C1", "C2", "C3" } }
                }
            };
            resBodyBytes = MiscUtils.StringToBytes("<a>this is news</a>");
            response.Body = new ByteBufferBody(resBodyBytes)
            {
                ContentLength = -1
            };
            testData.Add(new object[] { connection, maxChunkSize, responseBufferingEnabled, expectedRequest,
                response, resBodyBytes });

            // next...
            connection = "iowa";
            maxChunkSize = 40;
            responseBufferingEnabled = false;
            expectedRequest = new DefaultQuasiHttpRequest();

            response = new DefaultQuasiHttpResponse
            {
                StatusCode = 600,
            };
            resBodyBytes = MiscUtils.StringToBytes("this is it?".PadLeft(50));
            response.Body = new ByteBufferBody(resBodyBytes);
            testData.Add(new object[] { connection, maxChunkSize, responseBufferingEnabled, expectedRequest,
                response, resBodyBytes });

            // next...
            // zero content length in request
            connection = "..";
            maxChunkSize = 50;
            responseBufferingEnabled = false;
            expectedRequest = new DefaultQuasiHttpRequest();
            expectedRequest.Body = new LambdaBasedQuasiHttpBody
            {
                ContentLength = 0
            };

            response = new DefaultQuasiHttpResponse
            {
                StatusCode = 200,
                Headers = new Dictionary<string, IList<string>>()
            };
            resBodyBytes = new byte[1];
            response.Body = new ByteBufferBody(resBodyBytes);
            testData.Add(new object[] { connection, maxChunkSize, responseBufferingEnabled, expectedRequest,
                response, resBodyBytes });

            // next...
            // exceed buffering limit of 100 specified in test method
            connection = true;
            maxChunkSize = 40;
            responseBufferingEnabled = false;
            expectedRequest = new DefaultQuasiHttpRequest();

            response = new DefaultQuasiHttpResponse
            {
                StatusCode = 200,
                Headers = new Dictionary<string, IList<string>>()
            };
            resBodyBytes = MiscUtils.StringToBytes("dk".PadRight(120));
            response.Body = new ByteBufferBody(resBodyBytes)
            {
                ContentLength = -1
            };
            testData.Add(new object[] { connection, maxChunkSize, responseBufferingEnabled, expectedRequest,
                response, resBodyBytes });

            // next...
            connection = new List<object>();
            maxChunkSize = 150_000;
            responseBufferingEnabled = false;
            expectedRequest = new DefaultQuasiHttpRequest
            {
                Target = "/fxn".PadLeft(70_000)
            };

            response = new DefaultQuasiHttpResponse
            {
                HttpStatusMessage = "ok".PadLeft(90_000),
            };
            resBodyBytes = new byte[100_000];
            response.Body = new ByteBufferBody(resBodyBytes)
            {
                ContentLength = -1
            };
            testData.Add(new object[] { connection, maxChunkSize, responseBufferingEnabled, expectedRequest,
                response, resBodyBytes });

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
            var helpingReader = new LambdaBasedCustomReaderWriter
            {
                ReadFunc = (data, offset, length) =>
                {
                    // wait forever
                    return new TaskCompletionSource<int>().Task;
                }
            };

            // prepare to receive request to be written
            var headerReceiver = new MemoryStream();
            var backingWriters = new List<object>();
            backingWriters.Add(headerReceiver);
            backingWriters.Add(new LambdaBasedCustomReaderWriter
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
            var selfWritable = new LambdaBasedCustomWritable
            {
                WritableFunc = async writer =>
                {
                    helpingWriter.SwitchOver();
                    await IOUtils.WriteBytes(writer,
                        new byte[1], 0, 1);
                }
            };
            request.Body = new LambdaBasedQuasiHttpBody
            {
                SelfWritable = selfWritable
            };

            // set up instance
            var transport = new DemoQuasiHttpTransport(connection,
                helpingReader, helpingWriter);
            var instance = new DefaultSendProtocolInternal
            {
                Request = request,
                Transport = transport,
                Connection = connection,
                MaxChunkSize = maxChunkSize
            };

            // set up expected request headers
            var expectedReqChunk = CustomChunkedTransferCodec.CreateFromRequest(request);

            // act.
            await Assert.ThrowsAsync<NotImplementedException>(() =>
                instance.Send());

            // assert written request
            headerReceiver.Position = 0;
            var actualReqChunk = await new CustomChunkedTransferCodec().ReadLeadChunk(
                headerReceiver);
            // verify all contents of headerReceiver was used
            // before comparing lead chunks
            Assert.Equal(0, await IOUtils.ReadBytes(headerReceiver,
                new byte[1], 0, 1));
            ComparisonUtils.CompareLeadChunks(expectedReqChunk, actualReqChunk);

            await instance.Cancel();
            Assert.Equal(1, transport.ReleaseCallCount);
        }

        [Fact]
        public async Task TestSendForAbortOnResponseBodyReadError()
        {
            // arrange.
            object connection = "drew";
            int maxChunkSize = 80;
            var request = new DefaultQuasiHttpRequest();
            var responseBodyBytes = MiscUtils.StringToBytes("dkd".PadLeft(50));
            var response = new DefaultQuasiHttpResponse
            {
                Body = new ByteBufferBody(responseBodyBytes)
            };

            // prepare response for reading.
            var helpingReader = await SerializeResponseToBeRead(
                response, responseBodyBytes);

            // prepare to receive request to be written
            var headerReceiver = new MemoryStream();
            var backingWriters = new List<object>();
            backingWriters.Add(headerReceiver);
            var helpingWriter = new SequenceCustomWriter
            {
                Writers = backingWriters
            };

            // set up instance
            var transport = new DemoQuasiHttpTransport(connection,
                helpingReader, helpingWriter);
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
            var expectedReqChunk = CustomChunkedTransferCodec.CreateFromRequest(request);

            // act.
            var actualEx = await Assert.ThrowsAsync<CustomIOException>(() => instance.Send());
            Assert.Contains("limit of", actualEx.Message);

            // assert written request
            headerReceiver.Position = 0;
            var actualReqChunk = await new CustomChunkedTransferCodec().ReadLeadChunk(
                headerReceiver);
            // verify all contents of headerReceiver was used
            // before comparing lead chunks
            Assert.Equal(0, await IOUtils.ReadBytes(headerReceiver,
                new byte[1], 0, 1));
            ComparisonUtils.CompareLeadChunks(expectedReqChunk, actualReqChunk);

            await instance.Cancel();
            Assert.Equal(1, transport.ReleaseCallCount);
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
            var backingWriters = new List<object>();
            backingWriters.Add(headerReceiver);
            var helpingWriter = new SequenceCustomWriter
            {
                Writers = backingWriters
            };

            var helpingReader = new SequenceCustomReader();

            // set up instance
            var transport = new DemoQuasiHttpTransport(connection,
                helpingReader, helpingWriter);
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
            var expectedReqChunk = CustomChunkedTransferCodec.CreateFromRequest(request);

            // act.
            var actualResponse = await instance.Send();

            // assert
            Assert.Null(actualResponse);

            // assert written request.
            headerReceiver.Position = 0;
            var actualReqChunk = await new CustomChunkedTransferCodec().ReadLeadChunk(
                headerReceiver, 0);
            // verify all contents of headerReceiver was used
            // before comparing lead chunks
            Assert.Equal(0, await IOUtils.ReadBytes(headerReceiver,
                new byte[1], 0, 1));
            ComparisonUtils.CompareLeadChunks(expectedReqChunk, actualReqChunk);

            await instance.Cancel();
            Assert.Equal(1, transport.ReleaseCallCount);
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
            var backingWriters = new List<object>();
            backingWriters.Add(headerReceiver);
            var helpingWriter = new SequenceCustomWriter
            {
                Writers = backingWriters
            };

            var helpingReader = new SequenceCustomReader();

            // set up instance
            var transport = new DemoQuasiHttpTransport(connection,
                helpingReader, helpingWriter);
            var instance = new DefaultSendProtocolInternal
            {
                Request = request,
                Transport = transport,
                Connection = connection,
                MaxChunkSize = maxChunkSize,
                ResponseBufferingEnabled = true,
                ResponseBodyBufferingSizeLimit = 40,
                EnsureNonNullResponse = true
            };

            // set up expected request headers
            var expectedReqChunk = CustomChunkedTransferCodec.CreateFromRequest(request);

            // act.
            var actualEx = await Assert.ThrowsAsync<QuasiHttpRequestProcessingException>(() =>
                instance.Send());

            // assert
            Assert.Contains("no response", actualEx.Message);

            // assert written request.
            headerReceiver.Position = 0;
            var actualReqChunk = await new CustomChunkedTransferCodec().ReadLeadChunk(
                headerReceiver, 0);
            // verify all contents of headerReceiver was used
            // before comparing lead chunks
            Assert.Equal(0, await IOUtils.ReadBytes(headerReceiver,
                new byte[1], 0, 1));
            ComparisonUtils.CompareLeadChunks(expectedReqChunk, actualReqChunk);

            await instance.Cancel();
            Assert.Equal(1, transport.ReleaseCallCount);
        }

        [Fact]
        public async Task TestSendForRequestBodyTransferIfResponseHasNoBody()
        {
            var connection = "sth";
            var maxChunkSize = 80_000;
            var responseBufferingEnabled = true;
            var expectedRequest = new DefaultQuasiHttpRequest
            {
                HttpVersion = "1.1",
                Target = "/bread"
            };
            var expectedReqBodyBytes = MiscUtils.StringToBytes(
                "data".PadLeft(90_000));
            expectedRequest.Body = new LambdaBasedQuasiHttpBody
            {
                ContentLength = -1
            };

            var response = new DefaultQuasiHttpResponse
            {
                HttpVersion = "1.0",
                StatusCode = 200,
                HttpStatusMessage = "ok"
            };
            // prepare response for reading.
            var helpingReader = await SerializeResponseToBeRead(
                response, expectedReqBodyBytes);
            var tcs = new TaskCompletionSource<int>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var dependentReader = new LambdaBasedCustomReaderWriter
            {
                ReadFunc = (data, offset, length) =>
                {
                    return tcs.Task;
                }
            };
            helpingReader = new SequenceCustomReader
            {
                Readers = new List<object> { dependentReader, helpingReader }
            };

            // prepare to receive request to be written
            var headerReceiver = new MemoryStream();
            var bodyReceiver = new MemoryStream();
            var bodyWritable = new LambdaBasedCustomWritable
            {
                WritableFunc = async (writer) =>
                {
                    await IOUtils.WriteBytes(writer,
                        expectedReqBodyBytes, 0, expectedReqBodyBytes.Length);
                    // wait for enough time for EndWrites() call
                    // inside SetUpReceivingOfRequestToBeWritten() to take effect.
                    _ = Task.Delay(200).ContinueWith(_ =>
                    {
                        tcs.SetResult(0);
                    });
                }
            };
            var helpingWriter = SetUpReceivingOfRequestToBeWritten(
                expectedRequest, bodyWritable, headerReceiver,
                bodyReceiver);

            // set up instance
            var transport = new DemoQuasiHttpTransport(connection,
                helpingReader, helpingWriter);
            var instance = new DefaultSendProtocolInternal
            {
                Request = expectedRequest,
                Transport = transport,
                Connection = connection,
                MaxChunkSize = maxChunkSize,
                ResponseBufferingEnabled = responseBufferingEnabled,
                ResponseBodyBufferingSizeLimit = 100,
                EnsureNonNullResponse = true
            };

            // set up expected request headers
            var expectedReqChunk = CustomChunkedTransferCodec.CreateFromRequest(expectedRequest);

            // act.
            var actualResponse = await instance.Send();

            // begin assert.
            Assert.NotNull(actualResponse);
            Assert.NotNull(actualResponse.Response);
            Assert.Equal(0, transport.ReleaseCallCount);
            Assert.Equal(responseBufferingEnabled, actualResponse.ResponseBufferingApplied);

            // assert read response.
            await ComparisonUtils.CompareResponses(response, actualResponse.Response,
                null);

            // assert written request
            headerReceiver.Position = 0;
            var actualReqChunk = await new CustomChunkedTransferCodec().ReadLeadChunk(
                headerReceiver, 0);
            // verify all contents of headerReceiver was used
            // before comparing lead chunks
            Assert.Equal(0, await IOUtils.ReadBytes(headerReceiver,
                new byte[1], 0, 1));
            ComparisonUtils.CompareLeadChunks(expectedReqChunk, actualReqChunk);

            bodyReceiver.Position = 0;
            var reqBodyReader = new ChunkDecodingCustomReader(bodyReceiver);
            var actualReqBodyBytes = await IOUtils.ReadAllBytes(reqBodyReader);
            Assert.Equal(expectedReqBodyBytes, actualReqBodyBytes);

            // verify cancel expectations
            await instance.Cancel();
            Assert.Equal(1, transport.ReleaseCallCount);
        }

        [Fact]
        public async Task TestSendForRequestBodyTransferIfResponseAlsoHasBody()
        {
            var connection = "sth else";
            var maxChunkSize = -2;
            var responseBufferingEnabled = false;
            var expectedRequest = new DefaultQuasiHttpRequest
            {
                HttpVersion = "1.1",
                Target = "/bread"
            };
            var expectedReqBodyBytes = MiscUtils.StringToBytes(
                "data".PadLeft(90));
            expectedRequest.Body = new LambdaBasedQuasiHttpBody
            {
                ContentLength = -1
            };

            var resBodyBytes = MiscUtils.StringToBytes("sea");
            var response = new DefaultQuasiHttpResponse
            {
                HttpVersion = "1.0",
                StatusCode = 200,
                HttpStatusMessage = "ok",
                Body = new ByteBufferBody(resBodyBytes)
            };
            // prepare response for reading.
            var helpingReader = await SerializeResponseToBeRead(
                response, resBodyBytes);
            var tcs = new TaskCompletionSource<int>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var dependentReader = new LambdaBasedCustomReaderWriter
            {
                ReadFunc = (data, offset, length) =>
                {
                    return tcs.Task;
                }
            };
            helpingReader = new SequenceCustomReader
            {
                Readers = new List<object> { dependentReader, helpingReader }
            };

            // prepare to receive request to be written
            var headerReceiver = new MemoryStream();
            var bodyReceiver = new MemoryStream();
            var bodyWritable = new LambdaBasedCustomWritable
            {
                WritableFunc = async (writer) =>
                {
                    await IOUtils.WriteBytes(writer,
                        expectedReqBodyBytes, 0, expectedReqBodyBytes.Length);
                    // wait for enough time for EndWrites() call
                    // inside SetUpReceivingOfRequestToBeWritten() to take effect.
                    _ = Task.Delay(200).ContinueWith(_ =>
                    {
                        tcs.SetResult(0);
                    });
                }
            };
            var helpingWriter = SetUpReceivingOfRequestToBeWritten(
                expectedRequest, bodyWritable, headerReceiver,
                bodyReceiver);

            // set up instance
            var transport = new DemoQuasiHttpTransport(connection,
                helpingReader, helpingWriter);
            var instance = new DefaultSendProtocolInternal
            {
                Request = expectedRequest,
                Transport = transport,
                Connection = connection,
                MaxChunkSize = maxChunkSize,
                ResponseBufferingEnabled = responseBufferingEnabled,
                ResponseBodyBufferingSizeLimit = 100,
                EnsureNonNullResponse = true
            };

            // set up expected request headers
            var expectedReqChunk = CustomChunkedTransferCodec.CreateFromRequest(expectedRequest);

            // act.
            var actualResponse = await instance.Send();

            // begin assert.
            Assert.NotNull(actualResponse);
            Assert.NotNull(actualResponse.Response);
            Assert.Equal(0, transport.ReleaseCallCount);
            Assert.Equal(responseBufferingEnabled, actualResponse.ResponseBufferingApplied);

            // assert read response.
            await ComparisonUtils.CompareResponses(response, actualResponse.Response,
                resBodyBytes);

            // assert written request
            headerReceiver.Position = 0;
            var actualReqChunk = await new CustomChunkedTransferCodec().ReadLeadChunk(
                headerReceiver, 0);
            // verify all contents of headerReceiver was used
            // before comparing lead chunks
            Assert.Equal(0, await IOUtils.ReadBytes(headerReceiver,
                new byte[1], 0, 1));
            ComparisonUtils.CompareLeadChunks(expectedReqChunk, actualReqChunk);

            bodyReceiver.Position = 0;
            var reqBodyReader = new ChunkDecodingCustomReader(bodyReceiver);
            var actualReqBodyBytes = await IOUtils.ReadAllBytes(reqBodyReader);
            Assert.Equal(expectedReqBodyBytes, actualReqBodyBytes);

            // verify cancel expectations
            await instance.Cancel();
            Assert.Equal(1, transport.ReleaseCallCount);
        }

        [Fact]
        public async Task TestRequestHeadersExceedMaxChunkSizeError()
        {
            object connection = new List<object>();
            var request = new DefaultQuasiHttpRequest
            {
                Target = "/fxn".PadLeft(90)
            };
            var resStream = await CreateResponseStream(
                new DefaultQuasiHttpResponse(), 0, 0);
            var reqStream = new LambdaBasedCustomReaderWriter
            {
                WriteFunc = (data, offset, length)
                    => Task.CompletedTask
            };
            var transport = new DemoQuasiHttpTransport(connection,
                resStream, reqStream);
            var instance = new DefaultSendProtocolInternal
            {
                Request = request,
                Transport = transport,
                Connection = connection,
                MaxChunkSize = 67
            };

            var actualEx = await Assert.ThrowsAsync<ChunkEncodingException>(() =>
                instance.Send());
            Assert.Contains("quasi http headers exceed max", actualEx.Message);
        }

        [Fact]
        public async Task TestResponseHeadersExceedMaxChunkSizeError()
        {
            object connection = new List<object>();
            var response = new DefaultQuasiHttpResponse
            {
                HttpStatusMessage = "ok".PadLeft(82_000)
            };
            var resStream = await CreateResponseStream(response,
                100_000, 0);
            var reqStream = new LambdaBasedCustomReaderWriter
            {
                WriteFunc = (data, offset, length)
                    => Task.CompletedTask
            };
            var transport = new DemoQuasiHttpTransport(connection,
                resStream, reqStream);
            var instance = new DefaultSendProtocolInternal
            {
                Request = new DefaultQuasiHttpRequest(),
                Transport = transport,
                Connection = connection,
                MaxChunkSize = 76_000
            };

            var actualEx = await Assert.ThrowsAsync<ChunkDecodingException>(() =>
                instance.Send());
            Assert.Contains("quasi http headers", actualEx.Message);
            Assert.NotNull(actualEx.InnerException);
            Assert.Contains("chunk size exceeding max", actualEx.InnerException.Message);
        }
    }
}
