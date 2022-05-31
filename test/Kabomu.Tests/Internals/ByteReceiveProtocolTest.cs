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
        [Fact]
        public void Test1()
        {
            // arrange.
            object connection = "vgh";
            var eventLoop = new TestEventLoopApi();

            var expectedReq = new DefaultQuasiHttpRequest
            {
                Path = "/koobi",
                Headers = new Dictionary<string, List<string>>
                {
                    { "variant", new List<string>{ "sea", "drive" } }
                }
            };
            var reqBodyBytes = Encoding.UTF8.GetBytes("this is our king");
            var expectedReqBody = new ByteBufferBody(reqBodyBytes, 0, reqBodyBytes.Length,
                "text/plain");
            var pdu = new TransferPdu
            {
                Version = TransferPdu.Version01,
                PduType = TransferPdu.PduTypeRequest,
                Path = expectedReq.Path,
                Headers = expectedReq.Headers,
                ContentLength = expectedReqBody.ContentLength,
                ContentType = expectedReqBody.ContentType,
            };

            byte[] resBodyBytes = Encoding.UTF8.GetBytes("and this is our queen");
            var expectedResBody = new ByteBufferBody(resBodyBytes, 0, resBodyBytes.Length,
                "image/png");
            var response = new DefaultQuasiHttpResponse
            {
                StatusIndicatesSuccess = true,
                StatusMessage = "ok",
                Headers = new Dictionary<string, List<string>>
                {
                    { "dkt", new List<string>{ "bb" } }
                },
                Body = expectedResBody
            };
            var expectedResPdu = new TransferPdu
            {
                Version = TransferPdu.Version01,
                PduType = TransferPdu.PduTypeResponse,
                StatusIndicatesSuccess = response.StatusIndicatesSuccess,
                StatusMessage = response.StatusMessage,
                Headers = response.Headers,
                ContentLength = expectedResBody.ContentLength,
                ContentType = expectedResBody.ContentType
            };

            var inputStream = new MemoryStream();
            inputStream.Write(pdu.Serialize(true));
            inputStream.Write(reqBodyBytes);
            inputStream.Position = 0;
            var outputStream = new MemoryStream();
            IQuasiHttpApplication app = new TestQuasiHttpApplication(eventLoop, expectedReq, 
                expectedReqBody.ContentLength, expectedReqBody.ContentType, reqBodyBytes,
                response);
            IQuasiHttpTransport transport = new TestQuasiHttpTransport(connection,
                inputStream, outputStream, 4);
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
            int actualPduLength = (int)ByteUtils.DeserializeUpToInt64BigEndian(actualRes, 0, 4);
            var actualPdu = TransferPdu.Deserialize(actualRes, 4, actualPduLength);
            TransferPduTest.ComparePdus(expectedResPdu, actualPdu);
            var actualBodyBytes = new byte[actualRes.Length - 4 - actualPduLength];
            Array.Copy(actualRes, 4 + actualPduLength, actualBodyBytes, 0, actualBodyBytes.Length);
            if (expectedResBody == null)
            {
                Assert.Empty(actualBodyBytes);
            }
            else
            {
                Assert.Equal(resBodyBytes, actualBodyBytes);
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

        private class TestQuasiHttpApplication : IQuasiHttpApplication
        {
            private readonly IMutexApi _mutex;
            private readonly IQuasiHttpRequest _expectedReq;
            private readonly int _expectedContentLength;
            private readonly string _expectedContentType;
            private readonly byte[] _expectedBodyBytes;
            private readonly IQuasiHttpResponse _response;

            public TestQuasiHttpApplication(IMutexApi mutex, IQuasiHttpRequest expectedReq, 
                int expectedContentLength, string expectedContentType, byte[] expectedBodyBytes,
                IQuasiHttpResponse response)
            {
                _mutex = mutex;
                _expectedReq = expectedReq;
                _expectedContentLength = expectedContentLength;
                _expectedContentType = expectedContentType;
                _expectedBodyBytes = expectedBodyBytes;
                _response = response;
            }

            public void ProcessRequest(IQuasiHttpRequest request, Action<Exception, IQuasiHttpResponse> cb)
            {
                Assert.Equal(_expectedReq.Path, request.Path);
                TransferPduTest.CompareHeaders(_expectedReq.Headers, request.Headers);
                if (_expectedBodyBytes == null)
                {
                    Assert.Null(request.Body);
                }
                else
                {
                    Assert.NotNull(request.Body);
                    Assert.Equal(_expectedContentLength, request.Body.ContentLength);
                    Assert.Equal(_expectedContentType, request.Body.ContentType);
                    var byteStream = new MemoryStream();
                    var data = new byte[2];
                    while (true)
                    {
                        int bytesRead = 0;
                        var cbCalled = false;
                        request.Body.OnDataRead(_mutex, data, 0, data.Length, (e, i) =>
                        {
                            Assert.Null(e);
                            bytesRead = i;
                            cbCalled = true;
                        });
                        Assert.True(cbCalled);
                        Assert.True(bytesRead >= 0);
                        if (bytesRead == 0)
                        {
                            break;
                        }
                        byteStream.Write(data, 0, bytesRead);
                    }
                    var actualBodyBytes = byteStream.ToArray();
                    Assert.Equal(_expectedBodyBytes, actualBodyBytes);
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
