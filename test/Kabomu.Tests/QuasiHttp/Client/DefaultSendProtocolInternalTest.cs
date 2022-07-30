using Kabomu.Common;
using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.ChunkedTransfer;
using Kabomu.QuasiHttp.Client;
using Kabomu.QuasiHttp.EntityBody;
using Kabomu.Tests.Internals;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.QuasiHttp.Client
{
    public class DefaultSendProtocolInternalTest
    {
        [Fact]
        public async Task TestSendForDependencyErrors()
        {
            await Assert.ThrowsAsync<MissingDependencyException>(() =>
            {
                var instance = new DefaultSendProtocolInternal();
                return instance.Send(new DefaultQuasiHttpRequest());
            });
        }

        [Theory]
        [MemberData(nameof(CreateTestSendData))]
        public async Task TestSend(object connection, int maxChunkSize, bool responseStreamingEnabled,
            IQuasiHttpRequest expectedRequest, byte[] expectedRequestBodyBytes,
            IQuasiHttpResponse response, byte[] responseBodyBytes)
        {
            // arrange.
            var expectedReqChunk = new LeadChunk
            {
                Version = LeadChunk.Version01,
                Path = expectedRequest.Path,
                Headers = expectedRequest.Headers,
                ContentLength = expectedRequest.Body?.ContentLength ?? 0,
                ContentType = expectedRequest.Body?.ContentType,
                HttpVersion = expectedRequest.HttpVersion,
                HttpMethod = expectedRequest.HttpMethod
            };

            var resChunk = new LeadChunk
            {
                Version = LeadChunk.Version01,
                StatusIndicatesSuccess = response.StatusIndicatesSuccess,
                StatusIndicatesClientError = response.StatusIndicatesClientError,
                StatusMessage = response.StatusMessage,
                Headers = response.Headers,
                ContentLength = response.Body?.ContentLength ?? 0,
                ContentType = response.Body?.ContentType,
                HttpVersion = response.HttpVersion,
                HttpStatusCode = response.HttpStatusCode
            };

            var inputStream = new MemoryStream();
            var serializedRes = resChunk.Serialize();
            MiscUtils.WriteChunk(serializedRes, (data, offset, length) =>
                inputStream.Write(data, offset, length));
            if (responseBodyBytes != null)
            {
                if (response.Body.ContentLength < 0)
                {
                    var resBodyChunk = new SubsequentChunk
                    {
                        Version = LeadChunk.Version01,
                        Data = responseBodyBytes,
                        DataLength = responseBodyBytes.Length
                    }.Serialize();
                    MiscUtils.WriteChunk(resBodyChunk, (data, offset, length) =>
                        inputStream.Write(data, offset, length));
                    // write trailing empty chunk.
                    var emptyBodyChunk = new SubsequentChunk
                    {
                        Version = LeadChunk.Version01
                    }.Serialize();
                    MiscUtils.WriteChunk(emptyBodyChunk, (data, offset, length) =>
                        inputStream.Write(data, offset, length));
                }
                else
                {
                    inputStream.Write(responseBodyBytes);
                }
            }
            inputStream.Position = 0; // rewind read pointer.
            var outputStream = new MemoryStream();
            int releaseCallCount = 0;
            var transport = new ConfigurableQuasiHttpTransport
            {
                ReadBytesCallback = async (actualConnection, data, offset, length) =>
                {
                    Assert.Equal(connection, actualConnection);
                    var bytesRead = inputStream.Read(data, offset, length);
                    return bytesRead;
                },
                WriteBytesCallback = async (actualConnection, data, offset, length) =>
                {
                    Assert.Equal(connection, actualConnection);
                    outputStream.Write(data, offset, length);
                },
                ReleaseConnectionCallback = (actualConnection) =>
                {
                    Assert.Equal(connection, actualConnection);
                    releaseCallCount++;
                    return Task.CompletedTask;
                }
            };
            var expectedReleaseCallCount = 0;
            if (response.Body != null && !responseStreamingEnabled)
            {
                expectedReleaseCallCount++;
            }
            var instance = new DefaultSendProtocolInternal
            {
                Parent = new object(),
                Transport = transport,
                Connection = connection,
                MaxChunkSize = maxChunkSize,
                ResponseStreamingEnabled = responseStreamingEnabled,
                ResponseBodyBufferingSizeLimit = 100
            };
            var errorsSeen = new List<Exception>();
            var responsesSeen = new List<IQuasiHttpResponse>();
            int abortCallCount = 0;
            instance.AbortCallback = (parent, e, res) =>
            {
                Assert.Equal(instance.Parent, parent);
                lock (parent)
                {
                    if (e != null)
                    {
                        errorsSeen.Add(e);
                    }
                    if (res != null)
                    {
                        responsesSeen.Add(res);
                    }
                    abortCallCount++;
                }
                return Task.CompletedTask;
            };

            // act.
            IQuasiHttpResponse actualResponse = await instance.Send(expectedRequest);

            // assert.
            Assert.Equal(1, abortCallCount);
            Assert.Empty(errorsSeen);
            Assert.Equal(new List<IQuasiHttpResponse> { actualResponse }, responsesSeen);

            // assert expected behaviour of response closure occured by trying 
            // to determine if connection was released.
            Assert.Equal(expectedReleaseCallCount, releaseCallCount);

            await ComparisonUtils.CompareResponses(maxChunkSize, response, actualResponse,
                responseBodyBytes);

            // test cancellation after considering release call during comparison.
            releaseCallCount = 0;
            await instance.Cancel();
            Assert.Equal(1, releaseCallCount);

            // finally verify contents of output stream.
            var actualReq = outputStream.ToArray();
            Assert.NotEmpty(actualReq);
            var actualReqChunkLength = (int)ByteUtils.DeserializeUpToInt64BigEndian(actualReq, 0,
                MiscUtils.LengthOfEncodedChunkLength);
            var actualReqChunk = LeadChunk.Deserialize(actualReq,
                MiscUtils.LengthOfEncodedChunkLength, actualReqChunkLength);
            ComparisonUtils.CompareLeadChunks(expectedReqChunk, actualReqChunk);
            var actualRequestBodyLen = actualReq.Length -
                MiscUtils.LengthOfEncodedChunkLength- actualReqChunkLength;
            if (expectedRequestBodyBytes == null)
            {
                Assert.Equal(0, actualRequestBodyLen);
            }
            else
            {
                byte[] actualReqBodyBytes;
                if (actualReqChunk.ContentLength < 0)
                {
                    actualReqBodyBytes = await MiscUtils.ReadChunkedBody(actualReq,
                        actualReqChunkLength + MiscUtils.LengthOfEncodedChunkLength,
                        actualRequestBodyLen);
                }
                else
                {
                    actualReqBodyBytes = new byte[actualRequestBodyLen];
                    Array.Copy(actualReq, actualReqChunkLength + MiscUtils.LengthOfEncodedChunkLength,
                        actualReqBodyBytes, 0, actualRequestBodyLen);
                }
                Assert.Equal(expectedRequestBodyBytes, actualReqBodyBytes);
            }
        }

        public static List<object[]> CreateTestSendData()
        {
            var testData = new List<object[]>();

            object connection = "vgh";
            int maxChunkSize = 115;
            bool responseStreamingEnabled = false;
            var request = new DefaultQuasiHttpRequest
            {
                HttpMethod = "POST",
                Path = "/koobi",
                Headers = new Dictionary<string, List<string>>
                {
                    { "variant", new List<string>{ "sea", "drive" } }
                }
            };
            var reqBodyBytes = Encoding.UTF8.GetBytes("this is our king");
            request.Body = new ByteBufferBody(reqBodyBytes, 0, reqBodyBytes.Length)
            {
                ContentType = "text/plain"
            };

            var expectedResponse = new DefaultQuasiHttpResponse
            {
                StatusIndicatesSuccess = true,
                StatusMessage = "ok",
                HttpStatusCode = 200,
                Headers = new Dictionary<string, List<string>>
                {
                    { "dkt", new List<string>{ "bb" } }
                },
            };
            byte[] expectedResBodyBytes = Encoding.UTF8.GetBytes("and this is our queen");
            expectedResponse.Body = new ByteBufferBody(expectedResBodyBytes, 0, expectedResBodyBytes.Length)
            {
                ContentType = "image/png"
            };
            testData.Add(new object[] { connection, maxChunkSize, responseStreamingEnabled, request, reqBodyBytes,
                expectedResponse, expectedResBodyBytes });

            connection = 123;
            maxChunkSize = 90;
            responseStreamingEnabled = true;
            request = new DefaultQuasiHttpRequest
            {
                Path = "/p"
            };
            reqBodyBytes = null;

            expectedResponse = new DefaultQuasiHttpResponse
            {
                StatusIndicatesSuccess = false,
                StatusIndicatesClientError = true,
                StatusMessage = "not found"
            };
            expectedResBodyBytes = null;
            testData.Add(new object[] { connection, maxChunkSize, responseStreamingEnabled, request, reqBodyBytes,
                expectedResponse, expectedResBodyBytes });

            connection = null;
            maxChunkSize = 95;
            responseStreamingEnabled = false;
            request = new DefaultQuasiHttpRequest
            {
                HttpVersion = "1.1",
                Path = "/bread"
            };
            reqBodyBytes = Encoding.UTF8.GetBytes("<a>this is news</a>");
            request.Body = new StringBody("<a>this is news</a>")
            {
                ContentType = "application/xml"
            };

            expectedResponse = new DefaultQuasiHttpResponse
            {
                HttpVersion = "1.1",
                StatusIndicatesSuccess = false,
                StatusIndicatesClientError = false,
                StatusMessage = "server error"
            };
            expectedResBodyBytes = null;
            testData.Add(new object[] { connection, maxChunkSize, responseStreamingEnabled, request, reqBodyBytes,
                expectedResponse, expectedResBodyBytes });

            connection = new object();
            maxChunkSize = 100;
            responseStreamingEnabled = false;
            request = new DefaultQuasiHttpRequest
            {
                Path = "/fxn",
                Headers = new Dictionary<string, List<string>>
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
                StatusIndicatesSuccess = false,
                StatusIndicatesClientError = false,
                StatusMessage = "server error",
                Headers = new Dictionary<string, List<string>>
                {
                    { "x", new List<string>{ "A" } },
                    { "y", new List<string>{ "B1", "B2", "C1", "C2", "C3" } }
                }
            };
            expectedResBodyBytes = Encoding.UTF8.GetBytes("<a>this is news</a>");
            expectedResponse.Body = new StringBody("<a>this is news</a>")
            {
                ContentType = "application/xml"
            };
            testData.Add(new object[] { connection, maxChunkSize, responseStreamingEnabled, request, reqBodyBytes,
                expectedResponse, expectedResBodyBytes });

            // generate response data which exceed 100 bytes buffering limit
            connection = null;
            maxChunkSize = 50;
            responseStreamingEnabled = true;
            request = new DefaultQuasiHttpRequest();
            reqBodyBytes = null;

            expectedResponse = new DefaultQuasiHttpResponse
            {
                StatusIndicatesSuccess = true,
                Headers = new Dictionary<string, List<string>>()
            };
            expectedResBodyBytes = new byte[110];
            expectedResponse.Body = new ByteBufferBody(expectedResBodyBytes)
            {
                ContentType = "null"
            };
            testData.Add(new object[] { connection, maxChunkSize, responseStreamingEnabled, request, reqBodyBytes,
                expectedResponse, expectedResBodyBytes });

            connection = 79;
            maxChunkSize = 40;
            responseStreamingEnabled = true;
            request = new DefaultQuasiHttpRequest();
            reqBodyBytes = null;

            expectedResponse = new DefaultQuasiHttpResponse
            {
                StatusIndicatesSuccess = true,
                Headers = new Dictionary<string, List<string>>()
            };
            expectedResBodyBytes = Encoding.UTF8.GetBytes("dk".PadRight(120));
            expectedResponse.Body = new StringBody("dk".PadRight(120))
            {
                ContentType = "text/plain"
            };
            testData.Add(new object[] { connection, maxChunkSize, responseStreamingEnabled, request, reqBodyBytes,
                expectedResponse, expectedResBodyBytes });

            return testData;
        }

        [Fact]
        public async Task TestSendForAbortOnRequestBodyReadError()
        {
            // arrange.
            object connection = "drew";
            int maxChunkSize = 80;
            IQuasiHttpRequest expectedRequest = new DefaultQuasiHttpRequest
            {
                Body = new StringBody("closed")
            };
            // cause request body read to fail later on by ending the body now.
            await expectedRequest.Body.EndRead();
            IQuasiHttpResponse response = new DefaultQuasiHttpResponse();

            var expectedReqChunk = new LeadChunk
            {
                Version = LeadChunk.Version01,
                Path = expectedRequest.Path,
                Headers = expectedRequest.Headers,
                ContentLength = expectedRequest.Body?.ContentLength ?? 0,
                ContentType = expectedRequest.Body?.ContentType,
                HttpVersion = expectedRequest.HttpVersion,
                HttpMethod = expectedRequest.HttpMethod
            };

            var resChunk = new LeadChunk
            {
                Version = LeadChunk.Version01,
                StatusIndicatesSuccess = response.StatusIndicatesSuccess,
                StatusIndicatesClientError = response.StatusIndicatesClientError,
                StatusMessage = response.StatusMessage,
                Headers = response.Headers,
                ContentLength = response.Body?.ContentLength ?? 0,
                ContentType = response.Body?.ContentType,
                HttpVersion = response.HttpVersion,
                HttpStatusCode = response.HttpStatusCode
            };

            var inputStream = new MemoryStream();
            var serializedRes = resChunk.Serialize();
            MiscUtils.WriteChunk(serializedRes, (data, offset, length) =>
                inputStream.Write(data, offset, length));
            inputStream.Position = 0; // rewind read pointer.

            var outputStream = new MemoryStream();
            int releaseCallCount = 0;
            var transport = new ConfigurableQuasiHttpTransport
            {
                ReadBytesCallback = async (actualConnection, data, offset, length) =>
                {
                    Assert.Equal(connection, actualConnection);
                    var bytesRead = inputStream.Read(data, offset, length);
                    return bytesRead;
                },
                WriteBytesCallback = async (actualConnection, data, offset, length) =>
                {
                    Assert.Equal(connection, actualConnection);
                    outputStream.Write(data, offset, length);
                },
                ReleaseConnectionCallback = (actualConnection) =>
                {
                    Assert.Equal(connection, actualConnection);
                    releaseCallCount++;
                    return Task.CompletedTask;
                }
            };
            var instance = new DefaultSendProtocolInternal
            {
                Parent = new object(),
                Transport = transport,
                Connection = connection,
                MaxChunkSize = maxChunkSize
            };
            int abortCallCount = 0;
            var errorsSeen = new List<Exception>();
            var responsesSeen = new List<IQuasiHttpResponse>();
            instance.AbortCallback = (parent, e, res) =>
            {
                Assert.Equal(instance.Parent, parent);
                lock (parent)
                {
                    if (e != null)
                    {
                        errorsSeen.Add(e);
                    }
                    if (res != null)
                    {
                        responsesSeen.Add(res);
                    }
                    abortCallCount++;
                }
                return Task.CompletedTask;
            };

            // act.
            IQuasiHttpResponse actualResponse = await instance.Send(expectedRequest);

            // assert.
            Assert.Equal(2, abortCallCount);
            Assert.Single(errorsSeen);
            Assert.Equal(new List<IQuasiHttpResponse> { actualResponse }, responsesSeen);
            await ComparisonUtils.CompareResponses(maxChunkSize, response, actualResponse,
                null);

            // assert expected behaviour of response closure occured by trying 
            // to determine if connection was released.
            Assert.Equal(0, releaseCallCount);

            // test cancellation
            await instance.Cancel();
            Assert.Equal(1, releaseCallCount);

            // finally verify contents of output stream.
            var actualReq = outputStream.ToArray();
            Assert.NotEmpty(actualReq);
            var actualReqChunkLength = (int)ByteUtils.DeserializeUpToInt64BigEndian(actualReq, 0,
                MiscUtils.LengthOfEncodedChunkLength);
            var actualReqChunk = LeadChunk.Deserialize(actualReq,
                MiscUtils.LengthOfEncodedChunkLength, actualReqChunkLength);
            ComparisonUtils.CompareLeadChunks(expectedReqChunk, actualReqChunk);
            var actualRequestBodyLen = actualReq.Length -
                MiscUtils.LengthOfEncodedChunkLength - actualReqChunkLength;
            // since request body could not be read, there should be no request body data
            Assert.Equal(0, actualRequestBodyLen);
        }
    }
}
