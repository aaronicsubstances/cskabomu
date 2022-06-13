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
    public class ReceiveProtocolTest
    {
        [Theory]
        [MemberData(nameof(CreateTestOnReceiveData))]
        public void TestOnReceive(object connection, int maxChunkSize,
            IQuasiHttpRequest request, byte[] requestBodyBytes,
            IQuasiHttpResponse expectedResponse, byte[] expectedResponseBodyBytes)
        {
            // arrange.
            var eventLoop = new TestEventLoopApi();
            var reqChunk = new LeadChunk
            {
                Version = LeadChunk.Version01,
                Path = request.Path,
                Headers = request.Headers,
                ContentLength = request.Body?.ContentLength ?? 0,
                ContentType = request.Body?.ContentType,
                HttpVersion = request.HttpVersion,
                HttpMethod = request.HttpMethod
            };

            var expectedResChunk = new LeadChunk
            {
                Version = LeadChunk.Version01,
                StatusIndicatesSuccess = expectedResponse.StatusIndicatesSuccess,
                StatusIndicatesClientError = expectedResponse.StatusIndicatesClientError,
                StatusMessage = expectedResponse.StatusMessage,
                Headers = expectedResponse.Headers,
                ContentLength = expectedResponse.Body?.ContentLength ?? 0,
                ContentType = expectedResponse.Body?.ContentType,
                HttpVersion = expectedResponse.HttpVersion,
                HttpStatusCode = expectedResponse.HttpStatusCode
            };

            var inputStream = new MemoryStream();
            var serializedReq = reqChunk.Serialize();
            MiscUtils.WriteChunk(serializedReq, (data, offset, length) =>
                inputStream.Write(data, offset, length));
            if (requestBodyBytes != null)
            {
                var reqBodyChunk = new SubsequentChunk
                {
                    Version = LeadChunk.Version01,
                    Data = requestBodyBytes,
                    DataLength = requestBodyBytes.Length
                }.Serialize();
                MiscUtils.WriteChunk(reqBodyChunk, (data, offset, length) =>
                    inputStream.Write(data, offset, length));
                // write trailing empty chunk.
                var emptyBodyChunk = new SubsequentChunk
                {
                    Version = LeadChunk.Version01
                }.Serialize();
                MiscUtils.WriteChunk(emptyBodyChunk, (data, offset, length) =>
                    inputStream.Write(data, offset, length));
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
            var instance = new ReceiveProtocol();
            instance.Connection = connection;
            IQuasiHttpRequest actualRequest = null;
            var cbCalled = false;
            IQuasiHttpApplication app = new ConfigurableQuasiHttpApplication
            {
                ProcessRequestCallback = (req, resCb) =>
                {
                    Assert.False(cbCalled);
                    actualRequest = req;
                    resCb.Invoke(null, expectedResponse);
                    cbCalled = true;
                    // just for testing correct cancelling of callback waits, repeat
                    resCb.Invoke(null, expectedResponse);
                }
            };
            instance.Parent = new TestParentTransferProtocol(instance)
            {
                Application = app,
                Transport = transport,
                Mutex = eventLoop
            };

            // act
            instance.OnReceive();

            // assert
            Assert.True(cbCalled);
            ComparisonUtils.CompareRequests(eventLoop, maxChunkSize, request, actualRequest,
                requestBodyBytes);
            Assert.True(((TestParentTransferProtocol)instance.Parent).AbortCalled);
            var actualRes = outputStream.ToArray();
            Assert.NotEmpty(actualRes);
            int actualResChunkLength = ByteUtils.DeserializeInt16BigEndian(actualRes, 0);
            var actualResChunk = LeadChunk.Deserialize(actualRes, 2, actualResChunkLength);
            LeadChunkTest.CompareChunks(expectedResChunk, actualResChunk);
            var actualResponseBodyLen = actualRes.Length - 2 - actualResChunkLength;
            if (expectedResponseBodyBytes == null)
            {
                Assert.Equal(0, actualResponseBodyLen);
            }
            else
            {
                var actualResBodyBytes = MiscUtils.ReadChunkedBody(actualRes, actualResChunkLength + 2,
                    actualResponseBodyLen);
                Assert.Equal(expectedResponseBodyBytes, actualResBodyBytes);
            }
        }

        public static List<object[]> CreateTestOnReceiveData()
        {
            var testData = new List<object[]>();

            object connection = "vgh";
            int maxChunkSize = 100;
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
            maxChunkSize = 95;
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
            maxChunkSize = 90;
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
            maxChunkSize = 150;
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
            expectedResBodyBytes =  Encoding.UTF8.GetBytes("<a>this is news</a>");
            expectedResponse.Body = new StringBody("<a>this is news</a>", "application/xml");
            testData.Add(new object[] { connection, maxChunkSize, request, reqBodyBytes,
                expectedResponse, expectedResBodyBytes });

            return testData;
        }
    }
}
