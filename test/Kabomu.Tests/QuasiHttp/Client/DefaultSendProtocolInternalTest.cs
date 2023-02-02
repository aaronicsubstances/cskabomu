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
        public async Task TestSend(object connection, int maxChunkSize, bool responseBufferingEnabled,
            IQuasiHttpRequest expectedRequest, byte[] expectedRequestBodyBytes,
            IQuasiHttpResponse response, byte[] responseBodyBytes)
        {
            // arrange.
            var expectedReqChunk = new LeadChunk
            {
                Version = LeadChunk.Version01,
                RequestTarget = expectedRequest.Target,
                Headers = expectedRequest.Headers,
                ContentLength = expectedRequest.Body?.ContentLength ?? 0,
                ContentType = expectedRequest.Body?.ContentType,
                HttpVersion = expectedRequest.HttpVersion,
                Method = expectedRequest.Method
            };

            var inputStream = MiscUtils.CreateResponseInputStream(response, responseBodyBytes);
            var outputStream = new MemoryStream();
            int releaseCallCount = 0;
            var transport = new ConfigurableQuasiHttpTransport
            {
                ReadBytesCallback = async (actualConnection, data, offset, length) =>
                {
                    Assert.Same(connection, actualConnection);
                    var bytesRead = inputStream.Read(data, offset, length);
                    return bytesRead;
                },
                WriteBytesCallback = async (actualConnection, data, offset, length) =>
                {
                    Assert.Same(connection, actualConnection);
                    outputStream.Write(data, offset, length);
                },
                ReleaseConnectionCallback = (actualConnection) =>
                {
                    Assert.Same(connection, actualConnection);
                    releaseCallCount++;
                    return Task.CompletedTask;
                }
            };
            var expectedReleaseCallCount = 0;
            if (response.Body != null && responseBufferingEnabled)
            {
                expectedReleaseCallCount++;
            }
            var instance = new DefaultSendProtocolInternal
            {
                Transport = transport,
                Connection = connection,
                MaxChunkSize = maxChunkSize,
                ResponseBufferingEnabled = responseBufferingEnabled,
                ResponseBodyBufferingSizeLimit = 100
            };

            // act.
            var actualResponse = await instance.Send(expectedRequest);

            // assert.
            Assert.Equal(responseBufferingEnabled, actualResponse?.ResponseBufferingApplied);

            // assert expected behaviour of response closure occured by trying 
            // to determine if connection was released.
            Assert.Equal(expectedReleaseCallCount, releaseCallCount);

            await ComparisonUtils.CompareResponses(maxChunkSize, response, actualResponse.Response,
                responseBodyBytes);

            // test cancellation after considering release call during comparison.
            releaseCallCount = 0;
            instance.Cancel();
            Assert.Equal(connection == null ? 0 : 1, releaseCallCount);

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
            var reqBodyBytes = Encoding.UTF8.GetBytes("this is our king");
            request.Body = new ByteBufferBody(reqBodyBytes, 0, reqBodyBytes.Length)
            {
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
            byte[] expectedResBodyBytes = Encoding.UTF8.GetBytes("and this is our queen");
            expectedResponse.Body = new ByteBufferBody(expectedResBodyBytes, 0, expectedResBodyBytes.Length)
            {
                ContentType = "image/png"
            };
            testData.Add(new object[] { connection, maxChunkSize, responseBufferingEnabled, request, reqBodyBytes,
                expectedResponse, expectedResBodyBytes });

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

            connection = null;
            maxChunkSize = 95;
            responseBufferingEnabled = true;
            request = new DefaultQuasiHttpRequest
            {
                HttpVersion = "1.1",
                Target = "/bread"
            };
            reqBodyBytes = Encoding.UTF8.GetBytes("<a>this is news</a>");
            request.Body = new StringBody("<a>this is news</a>")
            {
                ContentType = "application/xml"
            };

            expectedResponse = new DefaultQuasiHttpResponse
            {
                HttpVersion = "1.1",
                StatusCode = DefaultQuasiHttpResponse.StatusCodeServerError,
                HttpStatusMessage = "server error"
            };
            expectedResBodyBytes = null;
            testData.Add(new object[] { connection, maxChunkSize, responseBufferingEnabled, request, reqBodyBytes,
                expectedResponse, expectedResBodyBytes });

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
            expectedResBodyBytes = Encoding.UTF8.GetBytes("<a>this is news</a>");
            expectedResponse.Body = new StringBody("<a>this is news</a>")
            {
                ContentType = "application/xml"
            };
            testData.Add(new object[] { connection, maxChunkSize, responseBufferingEnabled, request, reqBodyBytes,
                expectedResponse, expectedResBodyBytes });

            // generate response data which exceed 100 bytes buffering limit
            connection = null;
            maxChunkSize = 50;
            responseBufferingEnabled = false;
            request = new DefaultQuasiHttpRequest();
            reqBodyBytes = null;

            expectedResponse = new DefaultQuasiHttpResponse
            {
                StatusCode = 200,
                Headers = new Dictionary<string, IList<string>>()
            };
            expectedResBodyBytes = new byte[110];
            expectedResponse.Body = new ByteBufferBody(expectedResBodyBytes)
            {
                ContentType = "null"
            };
            testData.Add(new object[] { connection, maxChunkSize, responseBufferingEnabled, request, reqBodyBytes,
                expectedResponse, expectedResBodyBytes });

            connection = 79;
            maxChunkSize = 40;
            responseBufferingEnabled = false;
            request = new DefaultQuasiHttpRequest();
            reqBodyBytes = null;

            expectedResponse = new DefaultQuasiHttpResponse
            {
                StatusCode = 200,
                Headers = new Dictionary<string, IList<string>>()
            };
            expectedResBodyBytes = Encoding.UTF8.GetBytes("dk".PadRight(120));
            expectedResponse.Body = new StringBody("dk".PadRight(120))
            {
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
                RequestTarget = expectedRequest.Target,
                Headers = expectedRequest.Headers,
                ContentLength = expectedRequest.Body?.ContentLength ?? 0,
                ContentType = expectedRequest.Body?.ContentType,
                HttpVersion = expectedRequest.HttpVersion,
                Method = expectedRequest.Method
            };
            
            var inputStream = MiscUtils.CreateResponseInputStream(response, null);
            var outputStream = new MemoryStream();
            int releaseCallCount = 0;
            var transport = new ConfigurableQuasiHttpTransport
            {
                ReadBytesCallback = async (actualConnection, data, offset, length) =>
                {
                    Assert.Equal(connection, actualConnection);
                    var bytesRead = inputStream.Read(data, offset, length);
                    // introduce intentional delay to force request read to finish first.
                    await Task.Delay(1);
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
                Transport = transport,
                Connection = connection,
                MaxChunkSize = maxChunkSize,
                ResponseBufferingEnabled = false
            };

            // act.
            await Assert.ThrowsAnyAsync<Exception>(async () =>
            {
                await instance.Send(expectedRequest);
            });

            // assert expected behaviour of response closure occured by trying 
            // to determine if connection was released.
            Assert.Equal(0, releaseCallCount);

            // test cancellation
            instance.Cancel();
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
