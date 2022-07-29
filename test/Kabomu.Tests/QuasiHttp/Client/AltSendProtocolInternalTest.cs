using Kabomu.Common;
using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.Client;
using Kabomu.QuasiHttp.EntityBody;
using Kabomu.QuasiHttp.Transport;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.QuasiHttp.Client
{
    public class AltSendProtocolInternalTest
    {
        [Fact]
        public async Task TestSendForErrors()
        {
            await Assert.ThrowsAsync<MissingDependencyException>(() =>
                new AltSendProtocolInternal().Send(new DefaultQuasiHttpRequest()));

            var ex = await Assert.ThrowsAnyAsync<Exception>(() =>
            {
                var transport = new ConfigurableQuasiHttpTransport
                {
                    ProcessSendRequestCallback = (req, connectivityParams) =>
                    {
                        return Tuple.Create(Task.FromResult((IQuasiHttpResponse)null), (object)null);
                    }
                };
                var instance = new AltSendProtocolInternal
                {
                    Parent = new object(),
                    TransportBypass = transport,
                    AbortCallback = (parent, res) => Task.CompletedTask
                };
                return instance.Send(new DefaultQuasiHttpRequest());
            });
            Assert.Contains("no response", ex.Message);
        }

        [Fact]
        public async Task TestSendEnsuresCloseOnSuccessfulResponseBodyAccess()
        {
            var request = new DefaultQuasiHttpRequest();
            var connectivityParams = new DefaultConnectivityParams();
            var response = new ErrorQuasiHttpResponse
            {
                Body = new ErrorQuasiHttpBody()
            };
            var sendCancellationHandle = new object();
            var cancelSendCalled = false;
            var transport = new ConfigurableQuasiHttpTransport
            {
                ProcessSendRequestCallback = (actualRequest, actualConnectivityParams) =>
                {
                    Assert.Equal(request, actualRequest);
                    Assert.Equal(connectivityParams, actualConnectivityParams);
                    return Tuple.Create(Task.FromResult((IQuasiHttpResponse)response), sendCancellationHandle);
                },
                CancelSendRequestCallback = (actualSendCancellationHandle) =>
                {
                    Assert.False(cancelSendCalled);
                    Assert.Equal(sendCancellationHandle, actualSendCancellationHandle);
                    cancelSendCalled = true;
                }
            };
            var instance = new AltSendProtocolInternal
            {
                ConnectivityParams = connectivityParams,
                ResponseStreamingEnabled = false,
                TransportBypass = transport
            };
            var cbCallCount = 0;
            instance.AbortCallback = async (parent, res) =>
            {
                cbCallCount++;
            };
            await Assert.ThrowsAsync<NotImplementedException>(() =>
            {
                return instance.Send(request);
            });

            Assert.Equal(0, cbCallCount);
            Assert.True(response.CloseCalled);

            await instance.Cancel();
            Assert.True(cancelSendCalled);
        }

        [Fact]
        public async Task TestSendResponseStreamingEnabledAndBodyPresent()
        {
            var request = new DefaultQuasiHttpRequest();
            var connectivityParams = new DefaultConnectivityParams();
            var expectedResponse = new ErrorQuasiHttpResponse
            {
                Body = new StringBody("")
            };
            var transport = new ConfigurableQuasiHttpTransport
            {
                ProcessSendRequestCallback = (actualRequest, actualConnectivityParams) =>
                {
                    Assert.Equal(request, actualRequest);
                    Assert.Equal(connectivityParams, actualConnectivityParams);
                    return Tuple.Create(Task.FromResult((IQuasiHttpResponse)expectedResponse), (object)null);
                },
                CancelSendRequestCallback = _ => Task.FromException(new NotImplementedException())
            };
            var instance = new AltSendProtocolInternal
            {
                Parent = new object(),
                ConnectivityParams = connectivityParams,
                ResponseStreamingEnabled = true,
                TransportBypass = transport
            };
            var cbCalled = false;
            instance.AbortCallback = async (parent, res) =>
            {
                Assert.False(cbCalled);
                Assert.Equal(instance.Parent, parent);
                Assert.Equal(expectedResponse, res);
                cbCalled = true;
            };
            var response = await instance.Send(request);
            Assert.True(cbCalled);
            Assert.False(expectedResponse.CloseCalled);
            Assert.Equal(expectedResponse, response);

            // test successful cancellation due to null cancellation handle
            await instance.Cancel();
        }

        [Fact]
        public async Task TestSendResponseStreamingEnabledAndBodyAbsent()
        {
            var request = new DefaultQuasiHttpRequest();
            IConnectivityParams connectivityParams = null;
            var expectedResponse = new ErrorQuasiHttpResponse();
            var sendCancellationHandle = new object();
            var cancelSendCalled = false;
            var transport = new ConfigurableQuasiHttpTransport
            {
                ProcessSendRequestCallback = (actualRequest, actualConnectivityParams) =>
                {
                    Assert.Equal(request, actualRequest);
                    Assert.Equal(connectivityParams, actualConnectivityParams);
                    return Tuple.Create(Task.FromResult((IQuasiHttpResponse)expectedResponse), sendCancellationHandle);
                },
                CancelSendRequestCallback = (actualSendCancellationHandle) =>
                {
                    Assert.False(cancelSendCalled);
                    Assert.Equal(sendCancellationHandle, actualSendCancellationHandle);
                    cancelSendCalled = true;
                }
            };
            var instance = new AltSendProtocolInternal
            {
                Parent = new object(),
                ConnectivityParams = connectivityParams,
                ResponseStreamingEnabled = true,
                TransportBypass = transport
            };
            var cbCalled = false;
            instance.AbortCallback = async (parent, res) =>
            {
                Assert.False(cbCalled);
                Assert.Equal(instance.Parent, parent);
                Assert.Equal(expectedResponse, res);
                cbCalled = true;
            };
            var response = await instance.Send(request);
            Assert.True(cbCalled);
            Assert.True(expectedResponse.CloseCalled);
            Assert.Equal(expectedResponse, response);

            await instance.Cancel();
            Assert.True(cancelSendCalled);
        }

        [Fact]
        public async Task TestSendResponseStreamingDisabledAndBodyAbsent()
        {
            var request = new DefaultQuasiHttpRequest();
            var connectivityParams = new DefaultConnectivityParams
            {
                RemoteEndpoint = "localhost",
                ExtraParams = new Dictionary<string, object>
                {
                    { "scheme", "https" }
                }
            };
            var expectedResponse = new ErrorQuasiHttpResponse();
            var sendCancellationHandle = new object();
            var cancelSendCalled = false;
            var transport = new ConfigurableQuasiHttpTransport
            {
                ProcessSendRequestCallback = (actualRequest, actualConnectivityParams) =>
                {
                    Assert.Equal(request, actualRequest);
                    Assert.Equal(connectivityParams, actualConnectivityParams);
                    return Tuple.Create(Task.FromResult((IQuasiHttpResponse)expectedResponse), sendCancellationHandle);
                },
                CancelSendRequestCallback = (actualSendCancellationHandle) =>
                {
                    Assert.False(cancelSendCalled);
                    Assert.Equal(sendCancellationHandle, actualSendCancellationHandle);
                    cancelSendCalled = true;
                }
            };
            var instance = new AltSendProtocolInternal
            {
                Parent = new object(),
                ConnectivityParams = connectivityParams,
                ResponseStreamingEnabled = false,
                TransportBypass = transport
            };
            var cbCalled = false;
            instance.AbortCallback = async (parent, res) =>
            {
                Assert.False(cbCalled);
                Assert.Equal(instance.Parent, parent);
                Assert.Equal(expectedResponse, res);
                cbCalled = true;
            };
            var response = await instance.Send(request);
            Assert.True(cbCalled);
            Assert.True(expectedResponse.CloseCalled);
            Assert.Equal(expectedResponse, response);

            await instance.Cancel();
            Assert.True(cancelSendCalled);
        }

        [Theory]
        [MemberData(nameof(CreateTestSendResponseStreamingDisabledAndBodyPresentData))]
        public async Task TestSendResponseStreamingDisabledAndBodyPresent(int maxChunkSize,
            int responseBodyBufferingLimit, object sendCancellationHandle,
            byte[] expectedResBodyBytes, QuasiHttpResponseImpl2 expectedResponse)
        {
            var request = new DefaultQuasiHttpRequest();
            var connectivityParams = new DefaultConnectivityParams();
            var cancelSendCalled = false;
            var transport = new ConfigurableQuasiHttpTransport
            {
                ProcessSendRequestCallback = (actualRequest, actualConnectivityParams) =>
                {
                    Assert.Equal(request, actualRequest);
                    Assert.Equal(connectivityParams, actualConnectivityParams);
                    return Tuple.Create(Task.FromResult((IQuasiHttpResponse)expectedResponse),
                        sendCancellationHandle);
                },
                CancelSendRequestCallback = (actualSendCancellationHandle) =>
                {
                    Assert.False(cancelSendCalled);
                    Assert.Equal(sendCancellationHandle, actualSendCancellationHandle);
                    cancelSendCalled = true;
                }
            };
            var instance = new AltSendProtocolInternal
            {
                Parent = new object(),
                ConnectivityParams = connectivityParams,
                ResponseStreamingEnabled = false,
                MaxChunkSize = maxChunkSize,
                ResponseBodyBufferingSizeLimit = responseBodyBufferingLimit,
                TransportBypass = transport
            };
            var cbCalled = false;
            IQuasiHttpResponse cbRes = null;
            instance.AbortCallback = async (parent, res) =>
            {
                Assert.False(cbCalled);
                Assert.Equal(instance.Parent, parent);
                cbRes = res;
                cbCalled = true;
            };
            var response = await instance.Send(request);
            Assert.True(cbCalled);
            Assert.True(expectedResponse.CloseCalled);

            Assert.Equal(response, cbRes);
            await ComparisonUtils.CompareResponses(instance.MaxChunkSize,
                expectedResponse, response, expectedResBodyBytes);

            await instance.Cancel();
            Assert.Equal(sendCancellationHandle != null, cancelSendCalled);
        }

        public static List<object[]> CreateTestSendResponseStreamingDisabledAndBodyPresentData()
        {
            var testData = new List<object[]>();

            int maxChunkSize = 1;
            int responseBodyBufferingLimit = 0;
            object sendCancellationHandle = null;
            byte[] expectedResBodyBytes = new byte[0];
            QuasiHttpResponseImpl2 expectedResponse = new QuasiHttpResponseImpl2
            {
                Body = new ByteBufferBody(expectedResBodyBytes)
            };
            testData.Add(new object[] { maxChunkSize, responseBodyBufferingLimit, sendCancellationHandle,
                expectedResBodyBytes, expectedResponse });

            maxChunkSize = 1;
            responseBodyBufferingLimit = 0;
            sendCancellationHandle = new object();
            expectedResBodyBytes = new byte[0];
            expectedResponse = new QuasiHttpResponseImpl2
            {
                StatusIndicatesSuccess = true,
                HttpStatusCode = 200,
                HttpVersion = "",
                Headers = new Dictionary<string, List<string>>(),
                Body = new StringBody(ByteUtils.BytesToString(expectedResBodyBytes, 0,
                    expectedResBodyBytes.Length))
            };
            testData.Add(new object[] { maxChunkSize, responseBodyBufferingLimit, sendCancellationHandle,
                expectedResBodyBytes, expectedResponse });

            maxChunkSize = 2;
            sendCancellationHandle = new object();
            expectedResBodyBytes = new byte[]{ (byte)'a', (byte)'b', (byte)'c', (byte)'d',
                (byte)'e', (byte)'f' };
            responseBodyBufferingLimit = expectedResBodyBytes.Length;
            expectedResponse = new QuasiHttpResponseImpl2
            {
                Body = new StringBody(ByteUtils.BytesToString(expectedResBodyBytes, 0,
                    expectedResBodyBytes.Length))
            };
            testData.Add(new object[] { maxChunkSize, responseBodyBufferingLimit, sendCancellationHandle,
                expectedResBodyBytes, expectedResponse });

            maxChunkSize = 10;
            sendCancellationHandle = null;
            expectedResBodyBytes = new byte[]{ (byte)'a', (byte)'b', (byte)'c', (byte)'d',
                (byte)'e', (byte)'f' };
            responseBodyBufferingLimit = expectedResBodyBytes.Length;
            expectedResponse = new QuasiHttpResponseImpl2
            {
                Body = new StringBody(ByteUtils.BytesToString(expectedResBodyBytes, 0,
                    expectedResBodyBytes.Length))
            };
            testData.Add(new object[] { maxChunkSize, responseBodyBufferingLimit, sendCancellationHandle,
                expectedResBodyBytes, expectedResponse });

            maxChunkSize = 10;
            sendCancellationHandle = new object();
            expectedResBodyBytes = new byte[]{ (byte)'a', (byte)'b', (byte)'c', (byte)'d',
                (byte)'e', (byte)'f' };
            responseBodyBufferingLimit = 8;
            expectedResponse = new QuasiHttpResponseImpl2
            {
                StatusIndicatesClientError = true,
                StatusMessage = "not found",
                HttpStatusCode = 404,
                HttpVersion = "1.1",
                Headers = new Dictionary<string, List<string>>
                {
                    { "one", new List<string>{ "1" } },
                    { "two", new List<string>{ "2", "2" } },
                    { "three", new List<string>{ "3", "3", "3" } },
                    { "four", new List<string>{ "4", "4", "4", "4" } },
                },
                Body = new ByteBufferBody(expectedResBodyBytes)
            };
            testData.Add(new object[] { maxChunkSize, responseBodyBufferingLimit, sendCancellationHandle,
                expectedResBodyBytes, expectedResponse });

            return testData;
        }

        class ErrorQuasiHttpResponse : IQuasiHttpResponse
        {
            public bool CloseCalled { get; set; }

            public IQuasiHttpBody Body { get; set; }

            public bool StatusIndicatesSuccess => throw new NotImplementedException();

            public bool StatusIndicatesClientError => throw new NotImplementedException();

            public string StatusMessage => throw new NotImplementedException();

            public IDictionary<string, List<string>> Headers => throw new NotImplementedException();

            public int HttpStatusCode => throw new NotImplementedException();

            public string HttpVersion => throw new NotImplementedException();

            public Task Close()
            {
                CloseCalled = true;
                return Task.CompletedTask;
            }
        }

        private class ErrorQuasiHttpBody : IQuasiHttpBody
        {
            public long ContentLength => throw new NotImplementedException();

            public string ContentType => throw new NotImplementedException();

            public Task EndRead()
            {
                throw new NotImplementedException();
            }

            public Task<int> ReadBytes(byte[] data, int offset, int bytesToRead)
            {
                throw new NotImplementedException();
            }
        }

        public class QuasiHttpResponseImpl2 : IQuasiHttpResponse
        {
            public bool CloseCalled { get; set; }

            public IQuasiHttpBody Body { get; set; }

            public bool StatusIndicatesSuccess { get; set; }

            public bool StatusIndicatesClientError { get; set; }

            public string StatusMessage { get; set; }

            public IDictionary<string, List<string>> Headers { get; set; }

            public int HttpStatusCode { get; set; }

            public string HttpVersion { get; set; }

            public Task Close()
            {
                CloseCalled = true;
                return Task.CompletedTask;
            }
        }
    }
}
