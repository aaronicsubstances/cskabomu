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
                ContentLength = request.Body?.ContentLength ?? 0,
                ContentType = request.Body?.ContentType,
            };

            var expectedResPdu = new TransferPdu
            {
                Version = TransferPdu.Version01,
                PduType = TransferPdu.PduTypeResponse,
                StatusIndicatesSuccess = expectedResponse.StatusIndicatesSuccess,
                StatusIndicatesClientError = expectedResponse.StatusIndicatesClientError,
                StatusMessage = expectedResponse.StatusMessage,
                Headers = expectedResponse.Headers,
                ContentLength = expectedResponse.Body?.ContentLength ?? 0,
                ContentType = expectedResponse.Body?.ContentType
            };

            var inputStream = new MemoryStream();
            inputStream.Write(reqPdu.Serialize(true));
            if (requestBodyStr != null)
            {
                inputStream.Write(Encoding.UTF8.GetBytes(requestBodyStr));
            }
            inputStream.Position = 0; // rewind read pointer.
            var outputStream = new MemoryStream();
            IQuasiHttpApplication app = new TestQuasiHttpApplication(eventLoop, maxChunkSize,
                request, requestBodyStr, expectedResponse);
            IQuasiHttpTransport transport = new TestQuasiHttpTransport(connection,
                inputStream, outputStream, maxChunkSize);
            var instance = new ByteReceiveProtocol();
            instance.Connection = connection;
            instance.Parent = new TestParentTransferProtocol(instance)
            {
                Application = app,
                Transport = transport,
                Mutex = eventLoop
            };

            // act
            instance.OnReceive();

            // assert
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

        private class TestQuasiHttpApplication : IQuasiHttpApplication
        {
            private readonly IMutexApi _mutex;
            private readonly IQuasiHttpRequest _expectedRequest;
            private readonly string _expectedReqBodyStr;
            private readonly IQuasiHttpResponse _response;
            private readonly int _maxChunkSize;

            public TestQuasiHttpApplication(IMutexApi mutex, int maxChunkSize, IQuasiHttpRequest expectedRequest, 
                string expectedReqBodyStr, IQuasiHttpResponse response)
            {
                _mutex = mutex;
                _maxChunkSize = maxChunkSize;
                _expectedRequest = expectedRequest;
                _expectedReqBodyStr = expectedReqBodyStr;
                _response = response;
            }

            public void ProcessRequest(IQuasiHttpRequest request, Action<Exception, IQuasiHttpResponse> cb)
            {
                Assert.Equal(_expectedRequest.Path, request.Path);
                TransferPduTest.CompareHeaders(_expectedRequest.Headers, request.Headers);
                if (_expectedReqBodyStr == null)
                {
                    Assert.Null(request.Body);
                }
                else
                {
                    Assert.NotNull(request.Body);
                    Assert.Equal(_expectedRequest.Body.ContentLength, request.Body.ContentLength);
                    Assert.Equal(_expectedRequest.Body.ContentType, request.Body.ContentType);
                    byte[] actualReqBodyBytes = null;
                    var cbCalled = false;
                    TransportUtils.ReadBodyToEnd(_expectedRequest.Body, _mutex, _maxChunkSize, (e, data) =>
                    {
                        Assert.Null(e);
                        actualReqBodyBytes = data;
                        cbCalled = true;
                    });
                    Assert.True(cbCalled);
                    var actualReqBodyStr = Encoding.UTF8.GetString(actualReqBodyBytes, 0,
                        actualReqBodyBytes.Length);
                    Assert.Equal(_expectedReqBodyStr, actualReqBodyStr);
                }
                cb.Invoke(null, _response);
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
