using Kabomu.Common;
using Kabomu.Internals;
using Kabomu.QuasiHttp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace Kabomu.Tests.Internals
{
    public class ByteSendProtocolTest
    {
        [Theory]
        [MemberData(nameof(CreateTestOnSendData))]
        public void TestOnSend(object connection, int maxChunkSize,
            IQuasiHttpRequest expectedRequest, string expectedRequestBodyStr,
            IQuasiHttpResponse response, string responseBodyStr)
        {
            // arrange.
            var eventLoop = new TestEventLoopApi();
            var expectedReqPdu = new TransferPdu
            {
                Version = TransferPdu.Version01,
                PduType = TransferPdu.PduTypeRequest,
                Path = expectedRequest.Path,
                Headers = expectedRequest.Headers,
                ContentLength = expectedRequest.Body?.ContentLength ?? 0,
                ContentType = expectedRequest.Body?.ContentType,
            };

            var resPdu = new TransferPdu
            {
                Version = TransferPdu.Version01,
                PduType = TransferPdu.PduTypeResponse,
                StatusIndicatesSuccess = response.StatusIndicatesSuccess,
                StatusIndicatesClientError = response.StatusIndicatesClientError,
                StatusMessage = response.StatusMessage,
                Headers = response.Headers,
                ContentLength = response.Body?.ContentLength ?? 0,
                ContentType = response.Body?.ContentType
            };

            var inputStream = new MemoryStream();
            inputStream.Write(resPdu.Serialize(true));
            if (responseBodyStr != null)
            {
                inputStream.Write(Encoding.UTF8.GetBytes(responseBodyStr));
            }
            inputStream.Position = 0; // rewind read pointer.
            var outputStream = new MemoryStream();
            IQuasiHttpTransport transport = new TestQuasiHttpTransport(connection,
                inputStream, outputStream, maxChunkSize);
            var instance = new ByteSendProtocol();
            instance.Connection = connection;
            IQuasiHttpResponse actualResponse = null;
            var cbCalled = false;
            instance.SendCallback = (e, res) =>
            {
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
            CompareResponses(eventLoop, maxChunkSize, response, actualResponse,
                responseBodyStr);
            var actualReq = outputStream.ToArray();
            Assert.NotEmpty(actualReq);
            int actualReqPduLength = (int)ByteUtils.DeserializeUpToInt64BigEndian(actualReq, 0, 4);
            var actualReqPdu = TransferPdu.Deserialize(actualReq, 4, actualReqPduLength);
            TransferPduTest.ComparePdus(expectedReqPdu, actualReqPdu);
            var actualRequestBodyLen = actualReq.Length - 4 - actualReqPduLength;
            if (expectedRequestBodyStr == null)
            {
                Assert.Equal(0, actualRequestBodyLen);
            }
            else
            {
                var actualRequestBodyStr = Encoding.UTF8.GetString(actualReq, 4 + actualReqPduLength,
                    actualRequestBodyLen);
                Assert.Equal(expectedRequestBodyStr, actualRequestBodyStr);
            }
        }

        public static List<object[]> CreateTestOnSendData()
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

        private static void CompareResponses(IMutexApi mutex, int maxChunkSize,
            IQuasiHttpResponse expected, IQuasiHttpResponse actual,
            string expectedResBodyStr)
        {
            Assert.Equal(expected.StatusIndicatesSuccess, actual.StatusIndicatesSuccess);
            Assert.Equal(expected.StatusIndicatesClientError, actual.StatusIndicatesClientError);
            Assert.Equal(expected.StatusMessage, actual.StatusMessage);
            TransferPduTest.CompareHeaders(expected.Headers, actual.Headers);
            if (expectedResBodyStr == null)
            {
                Assert.Null(actual.Body);
            }
            else
            {
                Assert.NotNull(actual.Body);
                Assert.Equal(expected.Body.ContentLength, actual.Body.ContentLength);
                Assert.Equal(expected.Body.ContentType, actual.Body.ContentType);
                byte[] actualResBodyBytes = null;
                var cbCalled = false;
                TransportUtils.ReadBodyToEnd(expected.Body, mutex, maxChunkSize, (e, data) =>
                {
                    Assert.Null(e);
                    actualResBodyBytes = data;
                    cbCalled = true;
                });
                Assert.True(cbCalled);
                var actualResBodyStr = Encoding.UTF8.GetString(actualResBodyBytes, 0,
                    actualResBodyBytes.Length);
                Assert.Equal(expectedResBodyStr, actualResBodyStr);
            }
        }

        private class TestParentTransferProtocol : IParentTransferProtocol
        {
            private readonly ITransferProtocol _expectedTransfer;

            public TestParentTransferProtocol(ITransferProtocol expectedTransfer)
            {
                _expectedTransfer = expectedTransfer;
            }

            public int DefaultTimeoutMillis { get; set; }

            public IQuasiHttpApplication Application { get; set; }

            public IQuasiHttpTransport Transport { get; set; }

            public IMutexApi Mutex { get; set; }

            public UncaughtErrorCallback ErrorHandler { get; set; }

            public void AbortTransfer(ITransferProtocol transfer, Exception e)
            {
                Assert.Equal(_expectedTransfer, transfer);
                Assert.Null(e);
            }
        }

        private class TestQuasiHttpTransport : IQuasiHttpTransport
        {
            private readonly object _expectedConnection;
            private readonly MemoryStream _inputStream;
            private readonly MemoryStream _outputStream;

            public TestQuasiHttpTransport(object expectedConnection, MemoryStream inputStream,
                MemoryStream outputStream, int maxChunkSize)
            {
                _expectedConnection = expectedConnection;
                _inputStream = inputStream;
                _outputStream = outputStream;
                MaxMessageOrChunkSize = maxChunkSize;
            }

            public int MaxMessageOrChunkSize { get; }

            public bool IsByteOriented => true;

            public bool DirectSendRequestProcessingEnabled => throw new NotImplementedException();

            public void ProcessSendRequest(object remoteEndpoint, IQuasiHttpRequest request,
                Action<Exception, IQuasiHttpResponse> cb)
            {
                throw new NotImplementedException();
            }

            public void AllocateConnection(object remoteEndpoint, Action<Exception, object> cb)
            {
                throw new NotImplementedException();
            }

            public void ReleaseConnection(object connection)
            {
                throw new NotImplementedException();
            }

            public void SendMessage(object connection, byte[] data, int offset, int length,
                Action<Action<bool>> cancellationEnquirer, Action<Exception> cb)
            {
                throw new NotImplementedException();
            }

            public void ReadBytes(object connection, byte[] data, int offset, int length, Action<Exception, int> cb)
            {
                Assert.Equal(_expectedConnection, connection);
                var bytesRead = _inputStream.Read(data, offset, length);
                cb.Invoke(null, bytesRead);
            }

            public void WriteBytes(object connection, byte[] data, int offset, int length, Action<Exception> cb)
            {
                Assert.Equal(_expectedConnection, connection);
                _outputStream.Write(data, offset, length);
                cb.Invoke(null);
            }
        }
    }
}
