using Kabomu.Common;
using Kabomu.Internals;
using Kabomu.QuasiHttp;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace Kabomu.Tests.Internals
{
    public class ByteReceiveProtocolTest
    {
        [Theory]
        [MemberData(nameof(CreateTestOnReceiveData))]
        public void TestOnReceive(object connection, int maxChunkSize,
            IQuasiHttpRequest request, string requestBodyStr,
            IQuasiHttpResponse expectedResponse, string expectedResponseBodyStr)
        {
            // arrange.
            var eventLoop = new TestEventLoopApi();
            var reqPdu = new TransferPdu
            {
                Version = TransferPdu.Version01,
                PduType = TransferPdu.PduTypeRequest,
                Path = request.Path,
                Headers = request.Headers,
                HasContent = request.Body != null,
                ContentType = request.Body?.ContentType
            };

            var expectedResPdu = new TransferPdu
            {
                Version = TransferPdu.Version01,
                PduType = TransferPdu.PduTypeResponse,
                StatusIndicatesSuccess = expectedResponse.StatusIndicatesSuccess,
                StatusIndicatesClientError = expectedResponse.StatusIndicatesClientError,
                StatusMessage = expectedResponse.StatusMessage,
                Headers = expectedResponse.Headers,
                HasContent = expectedResponse.Body != null,
                ContentType = expectedResponse.Body?.ContentType
            };

            var inputStream = new MemoryStream();
            inputStream.Write(reqPdu.Serialize());
            if (requestBodyStr != null)
            {
                inputStream.Write(Encoding.UTF8.GetBytes(requestBodyStr));
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
                    // test handling of repeated callback invocations
                    cb.Invoke(null);
                }
            };
            var instance = new ByteReceiveProtocol();
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
                requestBodyStr);
            Assert.True(((TestParentTransferProtocol)instance.Parent).AbortCalled);
            var actualRes = outputStream.ToArray();
            Assert.NotEmpty(actualRes);
            int actualResPduLength = (int)ByteUtils.DeserializeUpToInt64BigEndian(actualRes, 0, 4);
            var actualResPdu = TransferPdu.Deserialize(actualRes, 4, actualResPduLength);
            TransferPduTest.ComparePdus(expectedResPdu, actualResPdu);
            var actualResponseBodyLen = actualRes.Length - 4 - actualResPduLength;
            if (expectedResponseBodyStr == null)
            {
                Assert.Equal(0, actualResponseBodyLen);
            }
            else
            {
                var actualResponseBodyStr = Encoding.UTF8.GetString(actualRes, 4 + actualResPduLength,
                    actualResponseBodyLen);
                Assert.Equal(expectedResponseBodyStr, actualResponseBodyStr);
            }
        }

        public static List<object[]> CreateTestOnReceiveData()
        {
            var testData = new List<object[]>();

            object connection = "vgh";
            int maxChunkSize = 4;
            var request = new DefaultQuasiHttpRequest
            {
                Path = "/koobi",
                Headers = new Dictionary<string, List<string>>
                {
                    { "variant", new List<string>{ "sea", "drive" } }
                }
            };
            var reqBodyStr = "this is our king";
            var reqBodyBytes = Encoding.UTF8.GetBytes(reqBodyStr);
            request.Body = new ByteBufferBody(reqBodyBytes, 0, reqBodyBytes.Length,
                "text/plain");

            var expectedResponse = new DefaultQuasiHttpResponse
            {
                StatusIndicatesSuccess = true,
                StatusMessage = "ok",
                Headers = new Dictionary<string, List<string>>
                {
                    { "dkt", new List<string>{ "bb" } }
                },
            };
            var expectedResBodyStr = "and this is our queen";
            byte[] expectedResBodyBytes = Encoding.UTF8.GetBytes(expectedResBodyStr);
            expectedResponse.Body = new ByteBufferBody(expectedResBodyBytes, 0, expectedResBodyBytes.Length,
                "image/png");
            testData.Add(new object[] { connection, maxChunkSize, request, reqBodyStr,
                expectedResponse, expectedResBodyStr });

            connection = 123;
            maxChunkSize = 10;
            request = new DefaultQuasiHttpRequest
            {
                Path = "/p"
            };
            reqBodyStr = null;

            expectedResponse = new DefaultQuasiHttpResponse
            {
                StatusIndicatesSuccess = false,
                StatusIndicatesClientError = true,
                StatusMessage = "not found"
            };
            expectedResBodyStr = null;
            testData.Add(new object[] { connection, maxChunkSize, request, reqBodyStr,
                expectedResponse, expectedResBodyStr });

            connection = null;
            maxChunkSize = 1;
            request = new DefaultQuasiHttpRequest
            {
                Path = "/bread"
            };
            reqBodyStr = "<a>this is news</a>";
            request.Body = new StringBody(reqBodyStr, "application/xml");

            expectedResponse = new DefaultQuasiHttpResponse
            {
                StatusIndicatesSuccess = false,
                StatusIndicatesClientError = false,
                StatusMessage = "server error"
            };
            expectedResBodyStr = null;
            testData.Add(new object[] { connection, maxChunkSize, request, reqBodyStr,
                expectedResponse, expectedResBodyStr });

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
            reqBodyStr = null;

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
            expectedResBodyStr = "<a>this is news</a>";
            expectedResponse.Body = new StringBody(expectedResBodyStr, "application/xml");
            testData.Add(new object[] { connection, maxChunkSize, request, reqBodyStr,
                expectedResponse, expectedResBodyStr });

            return testData;
        }
    }
}
