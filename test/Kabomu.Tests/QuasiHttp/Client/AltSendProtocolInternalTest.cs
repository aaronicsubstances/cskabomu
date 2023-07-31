using Kabomu.Common;
using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.Client;
using Kabomu.QuasiHttp.EntityBody;
using Kabomu.QuasiHttp.Transport;
using Kabomu.Tests.Shared.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
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
            {
                var instance = new AltSendProtocolInternal
                {
                    ResponseTask = Task.FromResult<IQuasiHttpResponse>(
                        new DefaultQuasiHttpResponse())
                };
                return instance.Send();
            });
        }

        [Fact]
        public async Task TestSendForNoResponse()
        {
            var transport = new HelperQuasiHttpAltTransport();
            var instance = new AltSendProtocolInternal
            {
                TransportBypass = transport,
                ResponseTask = Task.FromResult<IQuasiHttpResponse>(null)
            };
            var actual = await instance.Send();
            Assert.Null(actual);
        }

        [Fact]
        public async Task TestSendEnsuresCloseOnReceivingErrorResponse1()
        {
            var expectedResponse = new DefaultQuasiHttpResponse
            {
                CancellationTokenSource = new CancellationTokenSource(),
                Body = new ErrorQuasiHttpBody()
            };
            var transport = new HelperQuasiHttpAltTransport();
            var instance = new AltSendProtocolInternal
            {
                TransportBypass = transport,
                ResponseTask = Task.FromResult<IQuasiHttpResponse>(expectedResponse),
                SendCancellationHandle = new object(),
                ResponseBufferingEnabled = true
            };
            await Assert.ThrowsAsync<NotImplementedException>(() =>
            {
                return instance.Send();
            });
            Assert.True(expectedResponse.CancellationTokenSource.IsCancellationRequested);

            await instance.Cancel();
            Assert.Same(instance.SendCancellationHandle,
                transport.ActualCancellationHandle);
        }

        [Fact]
        public async Task TestSendEnsuresCloseOnReceivingErrorResponse2()
        {
            var expectedResponse = new DefaultQuasiHttpResponse
            {
                CancellationTokenSource = new CancellationTokenSource(),
                Body = new StringBody("too much!")
            };
            var transport = new HelperQuasiHttpAltTransport();
            var instance = new AltSendProtocolInternal
            {
                TransportBypass = transport,
                ResponseTask = Task.FromResult<IQuasiHttpResponse>(expectedResponse),
                SendCancellationHandle = new object(),
                ResponseBufferingEnabled = true,
                ResponseBodyBufferingSizeLimit = 5
            };
            var actualEx= await Assert.ThrowsAsync<CustomIOException>(() =>
            {
                return instance.Send();
            });
            Assert.Contains("limit of 5", actualEx.Message);
            Assert.True(expectedResponse.CancellationTokenSource.IsCancellationRequested);

            await instance.Cancel();
            Assert.Same(instance.SendCancellationHandle,
                transport.ActualCancellationHandle);
        }

        [Fact]
        public async Task TestSendResponseBufferingDisabledAndBodyPresent1()
        {
            var expectedResponse = new DefaultQuasiHttpResponse
            {
                Body = new StringBody("tea"),
                CancellationTokenSource = new CancellationTokenSource()
            };
            var transport = new HelperQuasiHttpAltTransport();
            var instance = new AltSendProtocolInternal
            {
                ResponseTask = Task.FromResult<IQuasiHttpResponse>(expectedResponse),
                ResponseBufferingEnabled = false,
                TransportBypass = transport
            };
            var res = await instance.Send();
            Assert.False(expectedResponse.CancellationTokenSource.IsCancellationRequested);
            Assert.Same(expectedResponse, res?.Response);
            Assert.Equal(false, res?.ResponseBufferingApplied);

            // test successful cancellation due to null cancellation handle
            await instance.Cancel();
            Assert.Null(transport.ActualCancellationHandle);
        }

        [Fact]
        public async Task TestSendResponseBufferingDisabledAndBodyPresent2()
        {
            var expectedResponse = new DefaultQuasiHttpResponse
            {
                Body = new StringBody("tea"),
                CancellationTokenSource = new CancellationTokenSource(),
                Environment = new Dictionary<string, object>
                {
                    { TransportUtils.ResEnvKeyResponseBufferingApplied, true }
                }
            };
            var transport = new HelperQuasiHttpAltTransport();
            var instance = new AltSendProtocolInternal
            {
                ResponseTask = Task.FromResult<IQuasiHttpResponse>(expectedResponse),
                SendCancellationHandle = new CancellationTokenSource(),
                ResponseBufferingEnabled = false,
                TransportBypass = transport
            };
            var res = await instance.Send();
            Assert.True(expectedResponse.CancellationTokenSource.IsCancellationRequested);
            Assert.Same(expectedResponse, res?.Response);
            Assert.Equal(true, res?.ResponseBufferingApplied);

            await instance.Cancel();
            Assert.Same(instance.SendCancellationHandle,
                transport.ActualCancellationHandle);
        }

        [Fact]
        public async Task TestSendResponseBufferingDisabledAndBodyAbsent()
        {
            var expectedResponse = new DefaultQuasiHttpResponse
            {
                CancellationTokenSource = new CancellationTokenSource(),
                Environment = new Dictionary<string, object>
                {
                    { TransportUtils.ResEnvKeyResponseBufferingApplied, null }
                }
            };
            var transport = new HelperQuasiHttpAltTransport();
            var instance = new AltSendProtocolInternal
            {
                ResponseTask = Task.FromResult<IQuasiHttpResponse>(expectedResponse),
                ResponseBufferingEnabled = false,
                TransportBypass = transport,
                SendCancellationHandle = new List<string>()
            };
            var res = await instance.Send();
            Assert.True(expectedResponse.CancellationTokenSource.IsCancellationRequested);
            Assert.Same(expectedResponse, res?.Response);
            Assert.Equal(false, res?.ResponseBufferingApplied);

            await instance.Cancel();
            Assert.Same(instance.SendCancellationHandle,
                transport.ActualCancellationHandle);
        }

        [Fact]
        public async Task TestSendResponseBufferingEnabledAndBodyAbsent1()
        {
            var expectedResponse = new DefaultQuasiHttpResponse
            {
                CancellationTokenSource = new CancellationTokenSource(),
                Environment = new Dictionary<string, object>
                {
                    { TransportUtils.ResEnvKeyResponseBufferingApplied, true }
                }
            };
            var transport = new HelperQuasiHttpAltTransport();
            var instance = new AltSendProtocolInternal
            {
                ResponseTask = Task.FromResult<IQuasiHttpResponse>(expectedResponse),
                ResponseBufferingEnabled = false,
                TransportBypass = transport
            };
            var res = await instance.Send();
            Assert.True(expectedResponse.CancellationTokenSource.IsCancellationRequested);
            Assert.Same(expectedResponse, res?.Response);
            Assert.Equal(true, res?.ResponseBufferingApplied);

            // test successful cancellation due to null cancellation handle
            await instance.Cancel();
            Assert.Null(transport.ActualCancellationHandle);
        }

        [Fact]
        public async Task TestSendResponseBufferingEnabledAndBodyAbsent2()
        {
            var expectedResponse = new DefaultQuasiHttpResponse
            {
                CancellationTokenSource = new CancellationTokenSource()
            };
            var transport = new HelperQuasiHttpAltTransport();
            var instance = new AltSendProtocolInternal
            {
                ResponseTask = Task.FromResult<IQuasiHttpResponse>(expectedResponse),
                ResponseBufferingEnabled = true,
                TransportBypass = transport
            };
            var res = await instance.Send();
            Assert.True(expectedResponse.CancellationTokenSource.IsCancellationRequested);
            Assert.Same(expectedResponse, res?.Response);
            Assert.Equal(false, res?.ResponseBufferingApplied);

            // test successful cancellation due to null cancellation handle
            await instance.Cancel();
            Assert.Null(transport.ActualCancellationHandle);
        }

        [Theory]
        [MemberData(nameof(CreateTestSendResponseBufferingEnabledAndBodyPresentData))]
        public async Task TestSendResponseBufferingEnabledAndBodyPresent(
            int responseBodyBufferingLimit, object sendCancellationHandle,
            byte[] expectedResBodyBytes, DefaultQuasiHttpResponse expectedResponse)
        {
            var transport = new HelperQuasiHttpAltTransport();
            var instance = new AltSendProtocolInternal
            {
                ResponseTask = Task.FromResult<IQuasiHttpResponse>(expectedResponse),
                TransportBypass = transport,
                SendCancellationHandle = sendCancellationHandle,
                ResponseBufferingEnabled = true,
                ResponseBodyBufferingSizeLimit = responseBodyBufferingLimit,
            };
            var response = await instance.Send();
            if (expectedResponse.CancellationTokenSource != null)
            {
                Assert.True(expectedResponse.CancellationTokenSource.IsCancellationRequested);
            }
            Assert.Equal(true, response?.ResponseBufferingApplied);

            await ComparisonUtils.CompareResponses(
                expectedResponse, response?.Response, expectedResBodyBytes);

            await instance.Cancel();
            Assert.Same(sendCancellationHandle, transport.ActualCancellationHandle);
        }

        public static List<object[]> CreateTestSendResponseBufferingEnabledAndBodyPresentData()
        {
            var testData = new List<object[]>();

            int responseBodyBufferingLimit = 0;
            object sendCancellationHandle = null;
            byte[] expectedResBodyBytes = new byte[0];
            var expectedResponse = new DefaultQuasiHttpResponse
            {
                Body = new ByteBufferBody(expectedResBodyBytes)
            };
            testData.Add(new object[] { responseBodyBufferingLimit, sendCancellationHandle,
                expectedResBodyBytes, expectedResponse });

            responseBodyBufferingLimit = 0;
            sendCancellationHandle = new object();
            expectedResBodyBytes = new byte[0];
            expectedResponse = new DefaultQuasiHttpResponse
            {
                StatusCode = 200,
                HttpVersion = "",
                Headers = new Dictionary<string, IList<string>>(),
                Body = new ByteBufferBody(expectedResBodyBytes)
                {
                    ContentLength = -1
                }
            };
            testData.Add(new object[] { responseBodyBufferingLimit, sendCancellationHandle,
                expectedResBodyBytes, expectedResponse });

            sendCancellationHandle = new object();
            expectedResBodyBytes = new byte[]{ (byte)'a', (byte)'b', (byte)'c', (byte)'d',
                (byte)'e', (byte)'f' };
            responseBodyBufferingLimit = expectedResBodyBytes.Length;
            expectedResponse = new DefaultQuasiHttpResponse
            {
                Body = new ByteBufferBody(expectedResBodyBytes)
                {
                    ContentLength = -1
                }
            };
            testData.Add(new object[] { responseBodyBufferingLimit, sendCancellationHandle,
                expectedResBodyBytes, expectedResponse });

            sendCancellationHandle = null;
            expectedResBodyBytes = new byte[]{ (byte)'a', (byte)'b', (byte)'c', (byte)'d',
                (byte)'e', (byte)'f' };
            responseBodyBufferingLimit = expectedResBodyBytes.Length;
            expectedResponse = new DefaultQuasiHttpResponse
            {
                Body = new ByteBufferBody(expectedResBodyBytes)
                {
                    ContentLength = -1
                }
            };
            testData.Add(new object[] { responseBodyBufferingLimit, sendCancellationHandle,
                expectedResBodyBytes, expectedResponse });

            sendCancellationHandle = new object();
            expectedResBodyBytes = new byte[]{ (byte)'a', (byte)'b', (byte)'c', (byte)'d',
                (byte)'e', (byte)'f' };
            responseBodyBufferingLimit = 8;
            expectedResponse = new DefaultQuasiHttpResponse
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
            testData.Add(new object[] { responseBodyBufferingLimit, sendCancellationHandle,
                expectedResBodyBytes, expectedResponse });

            return testData;
        }

        class HelperQuasiHttpAltTransport : IQuasiHttpAltTransport
        {
            public object ActualCancellationHandle { get; set; }

            public void CancelSendRequest(object sendCancellationHandle)
            {
                ActualCancellationHandle = sendCancellationHandle;
            }

            public (Task<IQuasiHttpResponse>, object) ProcessSendRequest(
                IQuasiHttpRequest request, IConnectivityParams connectivityParams)
            {
                throw new NotImplementedException();
            }

            public (Task<IQuasiHttpResponse>, object) ProcessSendRequest(
                Func<IDictionary<string, object>, Task<IQuasiHttpRequest>> requestFunc,
                IConnectivityParams connectivityParams)
            {
                throw new NotImplementedException();
            }
        }

        class ErrorQuasiHttpBody : IQuasiHttpBody
        {
            public long ContentLength => throw new NotImplementedException();

            public string ContentType => throw new NotImplementedException();

            public ICustomReader Reader => throw new NotImplementedException();

            public Task CustomDispose()
            {
                throw new NotImplementedException();
            }

            public Task WriteBytesTo(ICustomWriter writer)
            {
                throw new NotImplementedException();
            }
        }
    }
}
