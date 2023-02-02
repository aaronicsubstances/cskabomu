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

            var ex = await Assert.ThrowsAsync<ExpectationViolationException>(() =>
            {
                var transport = new ConfigurableQuasiHttpTransport
                {
                    ProcessSendRequestCallback = (req, connectivityParams) =>
                    {
                        return (Task.FromResult(new DefaultDirectSendResult() as IDirectSendResult),
                            (object)null);
                    }
                };
                var instance = new AltSendProtocolInternal
                {
                    TransportBypass = transport
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
                    Assert.Same(request, actualRequest);
                    Assert.Same(connectivityParams, actualConnectivityParams);
                    IDirectSendResult result = new DefaultDirectSendResult
                    {
                        Response = response
                    };
                    return (Task.FromResult(result), sendCancellationHandle);
                },
                CancelSendRequestCallback = (actualSendCancellationHandle) =>
                {
                    Assert.False(cancelSendCalled);
                    Assert.Same(sendCancellationHandle, actualSendCancellationHandle);
                    cancelSendCalled = true;
                }
            };
            var instance = new AltSendProtocolInternal
            {
                ConnectivityParams = connectivityParams,
                ResponseBufferingEnabled = true,
                TransportBypass = transport
            };
            await Assert.ThrowsAsync<NotImplementedException>(() =>
            {
                return instance.Send(request);
            });
            Assert.True(response.CloseCalled);

            instance.Cancel();
            Assert.True(cancelSendCalled);
        }

        [Fact]
        public async Task TestSendResponseBufferingDisabledAndBodyPresent()
        {
            var request = new DefaultQuasiHttpRequest();
            var connectivityParams = new DefaultConnectivityParams();
            var expectedResponse = new ErrorQuasiHttpResponse
            {
                Body = new StringBody("tea")
            };
            var transport = new ConfigurableQuasiHttpTransport
            {
                ProcessSendRequestCallback = (actualRequest, actualConnectivityParams) =>
                {
                    Assert.NotSame(request, actualRequest);
                    Assert.Same(connectivityParams, actualConnectivityParams);
                    IDirectSendResult result = new DefaultDirectSendResult
                    {
                        Response = expectedResponse
                    };
                    return (Task.FromResult(result), (object)null);
                },
                CancelSendRequestCallback = _ => Task.FromException(new NotImplementedException())
            };
            var instance = new AltSendProtocolInternal
            {
                ConnectivityParams = connectivityParams,
                ResponseBufferingEnabled = false,
                MaxChunkSize = 10,
                TransportBypass = transport,
                RequestWrappingEnabled = true
            };
            var response = await instance.Send(request);
            Assert.False(expectedResponse.CloseCalled);
            Assert.Same(expectedResponse, response?.Response);
            Assert.Equal(false, response?.ResponseBufferingApplied);

            // test successful cancellation due to null cancellation handle
            instance.Cancel();
        }

        [Fact]
        public async Task TestSendResponseBufferingDisabledAndBodyAbsent()
        {
            var request = new DefaultQuasiHttpRequest();
            IConnectivityParams connectivityParams = null;
            var expectedResponse = new QuasiHttpResponseImpl2();
            var sendCancellationHandle = new object();
            var cancelSendCalled = false;
            var transport = new ConfigurableQuasiHttpTransport
            {
                ProcessSendRequestCallback = (actualRequest, actualConnectivityParams) =>
                {
                    Assert.Same(request, actualRequest);
                    Assert.Same(connectivityParams, actualConnectivityParams);
                    IDirectSendResult result = new DefaultDirectSendResult
                    {
                        Response = expectedResponse,
                        ResponseBufferingApplied = true
                    };
                    return (Task.FromResult(result), sendCancellationHandle);
                },
                CancelSendRequestCallback = (actualSendCancellationHandle) =>
                {
                    Assert.False(cancelSendCalled);
                    Assert.Same(sendCancellationHandle, actualSendCancellationHandle);
                    cancelSendCalled = true;
                }
            };
            var instance = new AltSendProtocolInternal
            {
                ConnectivityParams = connectivityParams,
                ResponseBufferingEnabled = false,
                TransportBypass = transport,
                ResponseWrappingEnabled  = true
            };
            var response = await instance.Send(request);
            Assert.True(expectedResponse.CloseCalled);
            Assert.NotSame(expectedResponse, response?.Response);
            Assert.Equal(true, response?.ResponseBufferingApplied);

            instance.Cancel();
            Assert.True(cancelSendCalled);

            await ComparisonUtils.CompareResponses(instance.MaxChunkSize,
                expectedResponse, response?.Response, null);
        }

        [Fact]
        public async Task TestSendResponseBufferingEnabledAndBodyAbsent()
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
                    Assert.Same(request, actualRequest);
                    Assert.Same(connectivityParams, actualConnectivityParams);
                    IDirectSendResult result = new DefaultDirectSendResult
                    {
                        Response = expectedResponse
                    };
                    return (Task.FromResult(result), sendCancellationHandle);
                },
                CancelSendRequestCallback = (actualSendCancellationHandle) =>
                {
                    Assert.False(cancelSendCalled);
                    Assert.Same(sendCancellationHandle, actualSendCancellationHandle);
                    cancelSendCalled = true;
                }
            };
            var instance = new AltSendProtocolInternal
            {
                ConnectivityParams = connectivityParams,
                ResponseBufferingEnabled = true,
                TransportBypass = transport
            };
            var response = await instance.Send(request);
            Assert.True(expectedResponse.CloseCalled);
            Assert.Same(expectedResponse, response?.Response);
            Assert.Equal(false, response?.ResponseBufferingApplied);

            instance.Cancel();
            Assert.True(cancelSendCalled);
        }

        [Theory]
        [MemberData(nameof(CreateTestSendResponseBufferingEnabledAndBodyPresentData))]
        public async Task TestSendResponseBufferingEnabledAndBodyPresent(int maxChunkSize,
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
                    Assert.Same(request, actualRequest);
                    Assert.Same(connectivityParams, actualConnectivityParams);
                    IDirectSendResult result = new DefaultDirectSendResult
                    {
                        Response = expectedResponse
                    };
                    return (Task.FromResult(result), sendCancellationHandle);
                },
                CancelSendRequestCallback = (actualSendCancellationHandle) =>
                {
                    Assert.False(cancelSendCalled);
                    Assert.Same(sendCancellationHandle, actualSendCancellationHandle);
                    cancelSendCalled = true;
                }
            };
            var instance = new AltSendProtocolInternal
            {
                TransportBypass = transport,
                ConnectivityParams = connectivityParams,
                MaxChunkSize = maxChunkSize,
                ResponseBufferingEnabled = true,
                ResponseBodyBufferingSizeLimit = responseBodyBufferingLimit,
            };
            var response = await instance.Send(request);
            Assert.True(expectedResponse.CloseCalled);
            Assert.Equal(true, response?.ResponseBufferingApplied);
            await ComparisonUtils.CompareResponses(instance.MaxChunkSize,
                expectedResponse, response?.Response, expectedResBodyBytes);

            instance.Cancel();
            Assert.Equal(sendCancellationHandle != null, cancelSendCalled);
        }

        public static List<object[]> CreateTestSendResponseBufferingEnabledAndBodyPresentData()
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
                StatusCode = 200,
                HttpVersion = "",
                Headers = new Dictionary<string, IList<string>>(),
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
                StatusCode = 404,
                HttpStatusMessage = "not found",
                HttpVersion = "1.1",
                Headers = new Dictionary<string, IList<string>>
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

            public string HttpStatusMessage => throw new NotImplementedException();

            public IDictionary<string, IList<string>> Headers => throw new NotImplementedException();

            public int StatusCode => throw new NotImplementedException();

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

            public int StatusCode { get; set; }

            public IDictionary<string, IList<string>> Headers { get; set; }

            public string HttpVersion { get; set; }

            public string HttpStatusMessage { get; set; }

            public bool IsSuccessStatusCode { get; set; }

            public bool IsClientErrorStatusCode { get; set; }

            public bool IsServerErrorStatusCode { get; set; }

            public Task Close()
            {
                CloseCalled = true;
                return Task.CompletedTask;
            }
        }
    }
}
