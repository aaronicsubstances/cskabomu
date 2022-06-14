using Kabomu.Common;
using Kabomu.Common.Bodies;
using Kabomu.Internals;
using Kabomu.QuasiHttp;
using Kabomu.Tests.Common;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace Kabomu.Tests.Internals
{
    public class SendProtocolTest
    {
        [Theory]
        [MemberData(nameof(CreateTestOnSendData))]
        public void TestOnSend(object connection, int maxChunkSize,
            IQuasiHttpRequest expectedRequest, byte[] expectedRequestBodyBytes,
            IQuasiHttpResponse response, byte[] responseBodyBytes)
        {
            // arrange.
            var eventLoop = new TestEventLoopApi();
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
            var transport = new ConfigurableQuasiHttpTransport
            {
                MaxChunkSize = maxChunkSize,
                AllocateConnectionCallback = (actualRemoteEndpoint, cb) =>
                {
                    Assert.Null(actualRemoteEndpoint);
                    cb.Invoke(null, connection);
                    // test handling of multiple callback invocations.
                    cb.Invoke(null, connection);
                },
                ReleaseConnectionCallback = actualConnection =>
                {
                    Assert.Equal(connection, actualConnection);
                    // nothing to do again.
                },
                ReadBytesCallback = (actualConnection, data, offset, length, cb) =>
                {
                    Assert.Equal(connection, actualConnection);
                    var bytesRead = inputStream.Read(data, offset, length);
                    cb.Invoke(null, bytesRead);
                },
                WriteBytesCallback = (actualConnection, data, offset, length, cb) =>
                {
                    Assert.Equal(connection, actualConnection);
                    outputStream.Write(data, offset, length);
                    cb.Invoke(null);
                }
            };
            var instance = new SendProtocol();
            instance.Connection = connection;
            IQuasiHttpResponse actualResponse = null;
            var cbCalled = false;
            instance.SendCallback = (e, res) =>
            {
                Assert.False(cbCalled);
                Assert.Null(e);
                actualResponse = res;
                cbCalled = true;
            };
            instance.Parent = new TestParentTransferProtocol(instance)
            {
                Transport = transport,
                Mutex = eventLoop
            };

            // act.
            instance.OnSend(expectedRequest);

            // assert.
            Assert.True(cbCalled);
            ComparisonUtils.CompareResponses(eventLoop, maxChunkSize, response, actualResponse,
                responseBodyBytes);
            Assert.True(((TestParentTransferProtocol)instance.Parent).AbortCalled);
            var actualReq = outputStream.ToArray();
            Assert.NotEmpty(actualReq);
            int actualReqChunkLength = ByteUtils.DeserializeInt16BigEndian(actualReq, 0);
            var actualReqChunk = LeadChunk.Deserialize(actualReq, 2, actualReqChunkLength);
            LeadChunkTest.CompareChunks(expectedReqChunk, actualReqChunk);
            var actualRequestBodyLen = actualReq.Length - 2 - actualReqChunkLength;
            if (expectedRequestBodyBytes == null)
            {
                Assert.Equal(0, actualRequestBodyLen);
            }
            else
            {
                byte[] actualReqBodyBytes;
                if (actualReqChunk.ContentLength < 0)
                {
                    actualReqBodyBytes = MiscUtils.ReadChunkedBody(actualReq, actualReqChunkLength + 2,
                        actualRequestBodyLen);
                }
                else
                {
                    actualReqBodyBytes = new byte[actualRequestBodyLen];
                    Array.Copy(actualReq, actualReqChunkLength + 2, actualReqBodyBytes, 0, actualRequestBodyLen);
                }
                Assert.Equal(expectedRequestBodyBytes, actualReqBodyBytes);
            }
        }

        public static List<object[]> CreateTestOnSendData()
        {
            var testData = new List<object[]>();

            object connection = "vgh";
            int maxChunkSize = 115;
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
            request.Body = new ByteBufferBody(reqBodyBytes, 0, reqBodyBytes.Length,
                "text/plain");

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
            expectedResponse.Body = new ByteBufferBody(expectedResBodyBytes, 0, expectedResBodyBytes.Length,
                "image/png");
            testData.Add(new object[] { connection, maxChunkSize, request, reqBodyBytes,
                expectedResponse, expectedResBodyBytes });

            connection = 123;
            maxChunkSize = 90;
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
            testData.Add(new object[] { connection, maxChunkSize, request, reqBodyBytes,
                expectedResponse, expectedResBodyBytes });

            connection = null;
            maxChunkSize = 95;
            request = new DefaultQuasiHttpRequest
            {
                HttpVersion = "1.1",
                Path = "/bread"
            };
            reqBodyBytes = Encoding.UTF8.GetBytes("<a>this is news</a>");
            request.Body = new StringBody("<a>this is news</a>", "application/xml");

            expectedResponse = new DefaultQuasiHttpResponse
            {
                HttpVersion = "1.1",
                StatusIndicatesSuccess = false,
                StatusIndicatesClientError = false,
                StatusMessage = "server error"
            };
            expectedResBodyBytes = null;
            testData.Add(new object[] { connection, maxChunkSize, request, reqBodyBytes,
                expectedResponse, expectedResBodyBytes });

            connection = new object();
            maxChunkSize = 100;
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
            expectedResponse.Body = new StringBody("<a>this is news</a>", "application/xml");
            testData.Add(new object[] { connection, maxChunkSize, request, reqBodyBytes,
                expectedResponse, expectedResBodyBytes });

            return testData;
        }
    }
}
