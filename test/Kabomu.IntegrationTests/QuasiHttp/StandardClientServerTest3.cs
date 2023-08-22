using Kabomu.Common;
using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.Client;
using Kabomu.QuasiHttp.EntityBody;
using Kabomu.QuasiHttp.Server;
using Kabomu.QuasiHttp.Transport;
using Kabomu.Tests.Shared.Common;
using Kabomu.Tests.Shared.QuasiHttp;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.IntegrationTests.QuasiHttp
{
    public class StandardClientServerTest3
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        [Fact]
        public async Task TestClientBypass1()
        {
            // arrange
            var remoteEndpoint = new object();
            var request = new DefaultQuasiHttpRequest();
            IQuasiHttpRequest actualRequest = null;
            var expectedResponse = new DefaultQuasiHttpResponse
            {
                Body = new StringBody("tead")
            };
            var transportBypass = new DemoTransportBypass
            {
                SendRequestCallback = async (req) =>
                {
                    actualRequest = req;
                    return expectedResponse;
                },
                CreateCancellationHandles = true
            };
            var client = new StandardQuasiHttpClient
            {
                TransportBypass = transportBypass,
                Transport = new MemoryBasedClientTransport(),
                DefaultSendOptions = new DefaultQuasiHttpSendOptions
                {
                    ResponseBufferingEnabled = false,
                    ExtraConnectivityParams = new Dictionary<string, object>
                    {
                        { "1", "1" },
                        { "2", "2,2" }
                    }
                }
            };
            var expectedConnectivityParams = new DefaultQuasiHttpSendOptions
            {
                ExtraConnectivityParams = new Dictionary<string, object>
                {
                    { "1", "1" },
                    { "2", "2,2" }
                },
                TimeoutMillis = 0,
                MaxChunkSize = 0,
                ResponseBufferingEnabled = false,
                ResponseBodyBufferingSizeLimit = 0,
                EnsureNonNullResponse = true
            };

            // act
            var actualResponse = await client.Send(remoteEndpoint, request, null);

            // assert
            ComparisonUtils.CompareConnectivityParams(remoteEndpoint,
                transportBypass.ActualRemoteEndpoint,
                expectedConnectivityParams,
                transportBypass.ActualSendOptions);
            Assert.Same(request, actualRequest);
            Assert.Same(expectedResponse, actualResponse);
            // test that it is not disposed.
            await ComparisonUtils.CompareResponses(expectedResponse,
                actualResponse, null);
            // should be false due to response buffering.
            Assert.False(transportBypass.IsCancellationRequested);
        }

        [Fact]
        public async Task TestClientBypass2()
        {
            // arrange
            var remoteEndpoint = new object();
            var request = new DefaultQuasiHttpRequest();
            IQuasiHttpRequest actualRequest = null;
            var expectedResponse = new DefaultQuasiHttpResponse
            {
                Body = new StringBody("tread")
            };
            var transportBypass = new DemoTransportBypass
            {
                SendRequestCallback = async (req) =>
                {
                    actualRequest = req;
                    return expectedResponse;
                },
                CreateCancellationHandles = true
            };
            var client = new StandardQuasiHttpClient
            {
                TransportBypass = transportBypass,
                DefaultSendOptions = new DefaultQuasiHttpSendOptions
                {
                    ResponseBufferingEnabled = false,
                    ResponseBodyBufferingSizeLimit = 2,
                    EnsureNonNullResponse = false
                }
            };
            Func<IDictionary<string, object>, Task<IQuasiHttpRequest>> requestFunc = (reqEnv) =>
            {
                return Task.FromResult<IQuasiHttpRequest>(request);
            };
            var expectedConnectivityParams = new DefaultQuasiHttpSendOptions
            {
                ExtraConnectivityParams = new Dictionary<string, object>(),
                TimeoutMillis = 0,
                MaxChunkSize = 0,
                ResponseBufferingEnabled = false,
                ResponseBodyBufferingSizeLimit = 2,
                EnsureNonNullResponse = false
            };

            // act
            var result = client.Send(remoteEndpoint, requestFunc, null);
            var actualResponse = await result.ResponseTask;

            // assert
            Assert.Same(request, actualRequest);
            ComparisonUtils.CompareConnectivityParams(
                remoteEndpoint, transportBypass.ActualRemoteEndpoint,
                expectedConnectivityParams, transportBypass.ActualSendOptions);
            Assert.Same(expectedResponse, actualResponse);
            // test that it is not disposed.
            await ComparisonUtils.CompareResponses(expectedResponse,
                actualResponse, null);
            // should be false due to response buffering.
            Assert.False(transportBypass.IsCancellationRequested);
        }

        [Fact]
        public async Task TestClientBypass3()
        {
            // arrange
            var remoteEndpoint = new object();
            var request = new DefaultQuasiHttpRequest();
            IQuasiHttpRequest actualRequest = null;
            var expectedResBodyBytes = ByteUtils.StringToBytes("threads");
            var expectedResponse = new DefaultQuasiHttpResponse
            {
                Body = new StringBody("threads")
            };
            var transportBypass = new DemoTransportBypass
            {
                SendRequestCallback = async (req) =>
                {
                    actualRequest = req;
                    await Task.Delay(500);
                    return expectedResponse;
                }
            };
            var client = new StandardQuasiHttpClient
            {
                TransportBypass = transportBypass
            };
            Func<IDictionary<string, object>, Task<IQuasiHttpRequest>> requestFunc = async (reqEnv) =>
            {
                await Task.Delay(500);
                return request;
            };
            var sendOptions = new DefaultQuasiHttpSendOptions
            {
                TimeoutMillis = 3_000,
                MaxChunkSize = 20,
                ExtraConnectivityParams = new Dictionary<string, object>
                {
                    { "1", "one" },
                    { "2", "two" }
                }
            };
            var expectedConnectivityParams = new DefaultQuasiHttpSendOptions
            {
                ExtraConnectivityParams = new Dictionary<string, object>
                {
                    { "1", "one" },
                    { "2", "two" }
                },
                TimeoutMillis = 3_000,
                MaxChunkSize = 20,
                ResponseBufferingEnabled = true,
                ResponseBodyBufferingSizeLimit = 0,
                EnsureNonNullResponse = true
            };

            // act
            var result = client.Send(remoteEndpoint, requestFunc, sendOptions);
            var actualResponse = await result.ResponseTask;

            // assert
            Assert.Same(request, actualRequest);
            ComparisonUtils.CompareConnectivityParams(
                remoteEndpoint, transportBypass.ActualRemoteEndpoint,
                expectedConnectivityParams, transportBypass.ActualSendOptions);
            await ComparisonUtils.CompareResponses(expectedResponse,
                actualResponse, expectedResBodyBytes);
            Assert.False(transportBypass.IsCancellationRequested);
        }

        [Fact]
        public async Task TestClientBypass4()
        {
            // arrange
            var remoteEndpoint = new object();
            var request = new DefaultQuasiHttpRequest();
            IQuasiHttpRequest actualRequest = null;
            var expectedResBodyBytes = ByteUtils.StringToBytes("reads");
            var expectedResponse = new DefaultQuasiHttpResponse
            {
                Body = new StringBody("reads")
            };
            var transportBypass = new DemoTransportBypass
            {
                SendRequestCallback = async (req) =>
                {
                    actualRequest = req;
                    await Task.Delay(500);
                    return expectedResponse;
                }
            };
            var client = new StandardQuasiHttpClient
            {
                TransportBypass = transportBypass,
                DefaultSendOptions = new DefaultQuasiHttpSendOptions
                {
                    TimeoutMillis = 3_000,
                    ExtraConnectivityParams = new Dictionary<string, object>
                    {
                        { "1", "1" },
                        { "2", "2,2" },
                        { "3", "3,3,3" }
                    }
                }
            };
            var sendOptions = new DefaultQuasiHttpSendOptions
            {
                ExtraConnectivityParams = new Dictionary<string, object>
                {
                    { "1", "one" },
                    { "2", "two" }
                }
            };
            var expectedConnectivityParams = new DefaultQuasiHttpSendOptions
            {
                ExtraConnectivityParams = new Dictionary<string, object>
                {
                    { "1", "one" },
                    { "2", "two" },
                    { "3", "3,3,3" }
                },
                TimeoutMillis = 3_000,
                MaxChunkSize = 0,
                ResponseBufferingEnabled = true,
                ResponseBodyBufferingSizeLimit = 0,
                EnsureNonNullResponse = true
            };

            // act
            var actualResponse = await client.Send(remoteEndpoint, request, sendOptions);
            
            // assert
            Assert.Same(request, actualRequest);
            ComparisonUtils.CompareConnectivityParams(
                remoteEndpoint, transportBypass.ActualRemoteEndpoint,
                expectedConnectivityParams, transportBypass.ActualSendOptions);
            await ComparisonUtils.CompareResponses(expectedResponse,
                actualResponse, expectedResBodyBytes);
            Assert.False(transportBypass.IsCancellationRequested);
        }

        [Fact]
        public async Task TestClientBypass5()
        {
            // arrange
            var remoteEndpoint = new object();
            var request = new DefaultQuasiHttpRequest();
            IQuasiHttpRequest actualRequest = null;
            var expectedResBodyBytes = ByteUtils.StringToBytes("tie");
            var expectedResponse = new DefaultQuasiHttpResponse
            {
                Environment = new Dictionary<string, object>
                {
                    { TransportUtils.ResEnvKeyResponseBufferingApplied, "TRUE" }
                },
                Body = new StringBody("tie")
                {
                    ContentLength = -1
                }
            };
            var transportBypass = new DemoTransportBypass
            {
                SendRequestCallback = async (req) =>
                {
                    actualRequest = req;
                    await Task.Delay(500);
                    return expectedResponse;
                }
            };
            var client = new StandardQuasiHttpClient
            {
                TransportBypass = transportBypass,
                DefaultSendOptions = new DefaultQuasiHttpSendOptions
                {
                    TimeoutMillis = 300,
                    ResponseBodyBufferingSizeLimit = 800,
                    ResponseBufferingEnabled = true
                }
            };
            var sendOptions = new DefaultQuasiHttpSendOptions
            {
                TimeoutMillis = -1,
                ResponseBufferingEnabled = false,
                EnsureNonNullResponse = false,
                ResponseBodyBufferingSizeLimit = 500
            };
            var expectedConnectivityParams = new DefaultQuasiHttpSendOptions
            {
                ExtraConnectivityParams = new Dictionary<string, object>(),
                TimeoutMillis = -1,
                MaxChunkSize = 0,
                ResponseBufferingEnabled = false,
                ResponseBodyBufferingSizeLimit = 500,
                EnsureNonNullResponse = false
            };

            // act
            var actualResponse = await client.Send(remoteEndpoint, request, sendOptions);
            
            // assert
            Assert.Same(request, actualRequest);
            ComparisonUtils.CompareConnectivityParams(
                remoteEndpoint, transportBypass.ActualRemoteEndpoint,
                expectedConnectivityParams, transportBypass.ActualSendOptions);
            Assert.Same(expectedResponse, actualResponse);
            // test that it is not disposed.
            await ComparisonUtils.CompareResponses(expectedResponse,
                actualResponse, expectedResBodyBytes);
            // should be false due to absence of cancellation handle.
            Assert.False(transportBypass.IsCancellationRequested);
        }

        [Fact]
        public async Task TestClientBypass6()
        {
            // arrange
            var remoteEndpoint = new object();
            var request = new DefaultQuasiHttpRequest();
            IQuasiHttpRequest actualRequest = null;
            var expectedResBodyBytes = ByteUtils.StringToBytes("tie");
            var expectedResponse = new DefaultQuasiHttpResponse
            {
                Environment = new Dictionary<string, object>
                {
                    { TransportUtils.ResEnvKeyResponseBufferingApplied, "TRUE" }
                },
                Body = new StringBody("tie")
                {
                    ContentLength = -1
                }
            };
            var transportBypass = new DemoTransportBypass
            {
                SendRequestCallback = async (req) =>
                {
                    actualRequest = req;
                    await Task.Delay(500);
                    return expectedResponse;
                },
                CreateCancellationHandles = true
            };
            var client = new StandardQuasiHttpClient
            {
                TransportBypass = transportBypass,
                DefaultSendOptions = new DefaultQuasiHttpSendOptions
                {
                    TimeoutMillis = 300,
                    ResponseBufferingEnabled = false,
                    EnsureNonNullResponse = true
                }
            };
            var sendOptions = new DefaultQuasiHttpSendOptions
            {
                TimeoutMillis = -1,
                ResponseBufferingEnabled = false,
                ResponseBodyBufferingSizeLimit = 500,
                EnsureNonNullResponse = false
            };
            var expectedConnectivityParams = new DefaultQuasiHttpSendOptions
            {
                ExtraConnectivityParams = new Dictionary<string, object>(),
                TimeoutMillis = -1,
                MaxChunkSize = 0,
                ResponseBufferingEnabled = false,
                ResponseBodyBufferingSizeLimit = 500,
                EnsureNonNullResponse = false
            };

            // act
            var actualResponse = await client.Send(remoteEndpoint, request, sendOptions);

            // assert
            Assert.Same(request, actualRequest);
            ComparisonUtils.CompareConnectivityParams(
                remoteEndpoint, transportBypass.ActualRemoteEndpoint,
                expectedConnectivityParams, transportBypass.ActualSendOptions);
            Assert.Same(expectedResponse, actualResponse);
            // test that it is not disposed.
            await ComparisonUtils.CompareResponses(expectedResponse,
                actualResponse, expectedResBodyBytes);
            Assert.True(transportBypass.IsCancellationRequested);
        }

        [Fact]
        public async Task TestClientBypass7()
        {
            // arrange
            var remoteEndpoint = new object();
            var request = new DefaultQuasiHttpRequest();
            IQuasiHttpRequest actualRequest = null;
            var transportBypass = new DemoTransportBypass
            {
                SendRequestCallback = async (req) =>
                {
                    actualRequest = req;
                    await Task.Delay(500);
                    return null;
                }
            };
            var client = new StandardQuasiHttpClient
            {
                TransportBypass = transportBypass,
                DefaultSendOptions = new DefaultQuasiHttpSendOptions
                {
                    TimeoutMillis = 3_000,
                    ExtraConnectivityParams = new Dictionary<string, object>
                    {
                        { "1", "1" },
                        { "2", "2,2" },
                        { "3", "3,3,3" }
                    },
                    EnsureNonNullResponse = true
                }
            };
            var sendOptions = new DefaultQuasiHttpSendOptions
            {
                ExtraConnectivityParams = new Dictionary<string, object>
                {
                    { "1", "one" },
                    { "2", "two" },
                    { "4", "four" },
                    { TransportUtils.ConnectivityParamFireAndForget, true }
                }
            };
            var expectedConnectivityParams = new DefaultQuasiHttpSendOptions
            {
                ExtraConnectivityParams = new Dictionary<string, object>
                {
                    { "1", "one" },
                    { "2", "two" },
                    { "3", "3,3,3" },
                    { "4", "four" },
                    { TransportUtils.ConnectivityParamFireAndForget, true }
                },
                TimeoutMillis = 3_000,
                MaxChunkSize = 0,
                ResponseBufferingEnabled = true,
                ResponseBodyBufferingSizeLimit = 0,
                EnsureNonNullResponse = true
            };

            // act
            var actualEx = await Assert.ThrowsAsync<QuasiHttpRequestProcessingException>(() =>
                client.Send(remoteEndpoint, request, sendOptions));

            // assert
            Assert.Same(request, actualRequest);
            ComparisonUtils.CompareConnectivityParams(
                remoteEndpoint, transportBypass.ActualRemoteEndpoint,
                expectedConnectivityParams, transportBypass.ActualSendOptions);
            Assert.Contains("no response", actualEx.Message);
            Assert.False(transportBypass.IsCancellationRequested);
        }

        [Fact]
        public async Task TestClientBypass8()
        {
            // arrange
            var remoteEndpoint = new object();
            var request = new DefaultQuasiHttpRequest();
            IQuasiHttpRequest actualRequest = null;
            var transportBypass = new DemoTransportBypass
            {
                CreateCancellationHandles = true,
                SendRequestCallback = async (req) =>
                {
                    actualRequest = req;
                    await Task.Delay(500);
                    return null;
                }
            };
            var client = new StandardQuasiHttpClient
            {
                TransportBypass = transportBypass,
                DefaultSendOptions = new DefaultQuasiHttpSendOptions
                {
                    TimeoutMillis = 3_000,
                    ExtraConnectivityParams = new Dictionary<string, object>
                    {
                        { "1", "1" },
                        { "2", "2,2" },
                        { "3", "3,3,3" }
                    }
                }
            };
            var sendOptions = new DefaultQuasiHttpSendOptions
            {
                ExtraConnectivityParams = new Dictionary<string, object>
                {
                    { "11", "eleven" },
                    { "12", "twelve" },
                    { "4", "four" },
                    { TransportUtils.ConnectivityParamFireAndForget, true }
                }
            };
            var expectedConnectivityParams = new DefaultQuasiHttpSendOptions
            {
                ExtraConnectivityParams = new Dictionary<string, object>
                {
                    { "1", "1" },
                    { "2", "2,2" },
                    { "3", "3,3,3" },
                    { "4", "four" },
                    { "11", "eleven" },
                    { "12", "twelve" },
                    { TransportUtils.ConnectivityParamFireAndForget, true }
                },
                TimeoutMillis = 3_000,
                MaxChunkSize = 0,
                ResponseBufferingEnabled = true,
                ResponseBodyBufferingSizeLimit = 0,
                EnsureNonNullResponse = false
            };

            // act
            var result = client.Send(remoteEndpoint,
                _ => Task.FromResult<IQuasiHttpRequest>(request), sendOptions);
            var actualResponse = await result.ResponseTask;

            // assert
            Assert.Same(request, actualRequest);
            ComparisonUtils.CompareConnectivityParams(
                remoteEndpoint, transportBypass.ActualRemoteEndpoint,
                expectedConnectivityParams, transportBypass.ActualSendOptions);
            Assert.Null(actualResponse);
            Assert.True(transportBypass.IsCancellationRequested);
        }

        [Fact]
        public async Task TestClientBypass9()
        {
            // arrange
            var remoteEndpoint = new object();
            var request = new DefaultQuasiHttpRequest();
            IQuasiHttpRequest actualRequest = null;
            var transportBypass = new DemoTransportBypass
            {
                CreateCancellationHandles = true,
                SendRequestCallback = async (req) =>
                {
                    actualRequest = req;
                    await Task.Delay(500);
                    return null;
                }
            };
            var client = new StandardQuasiHttpClient
            {
                TransportBypass = transportBypass,
                DefaultSendOptions = new DefaultQuasiHttpSendOptions
                {
                    TimeoutMillis = 3_100,
                    MaxChunkSize = 4_000,
                    ResponseBufferingEnabled = false,
                    ExtraConnectivityParams = new Dictionary<string, object>
                    {
                        { "1", "1" },
                        { "2", "2,2" },
                        { "3", "3,3,3" }
                    }
                }
            };
            var sendOptions = new DefaultQuasiHttpSendOptions
            {
                ExtraConnectivityParams = new Dictionary<string, object>
                {
                    { "11", "eleven" },
                    { "12", "twelve" },
                    { "4", "four" }
                },
                MaxChunkSize = 1500,
            };
            var expectedConnectivityParams = new DefaultQuasiHttpSendOptions
            {
                ExtraConnectivityParams = new Dictionary<string, object>
                {
                    { "1", "1" },
                    { "2", "2,2" },
                    { "3", "3,3,3" },
                    { "4", "four" },
                    { "11", "eleven" },
                    { "12", "twelve" }
                },
                TimeoutMillis = 3_100,
                MaxChunkSize = 1500,
                ResponseBufferingEnabled = false,
                ResponseBodyBufferingSizeLimit = 0,
                EnsureNonNullResponse = true
            };

            // act
            var result = client.Send(remoteEndpoint,
                _ => Task.FromResult<IQuasiHttpRequest>(request), sendOptions);
            var actualEx = await Assert.ThrowsAsync<QuasiHttpRequestProcessingException>(
                () => result.ResponseTask);

            // assert
            Assert.Same(request, actualRequest);
            ComparisonUtils.CompareConnectivityParams(
                remoteEndpoint, transportBypass.ActualRemoteEndpoint,
                expectedConnectivityParams, transportBypass.ActualSendOptions);
            Assert.Contains("no response", actualEx.Message);
            Assert.True(transportBypass.IsCancellationRequested);
        }

        [Fact]
        public async Task TestClientBypass10()
        {
            // arrange
            var remoteEndpoint = new object();
            var request = new DefaultQuasiHttpRequest();
            IQuasiHttpRequest actualRequest = null;
            var transportBypass = new DemoTransportBypass
            {
                SendRequestCallback = async (req) =>
                {
                    actualRequest = req;
                    await Task.Delay(500);
                    return null;
                }
            };
            var client = new StandardQuasiHttpClient
            {
                TransportBypass = transportBypass,
                DefaultSendOptions = new DefaultQuasiHttpSendOptions
                {
                    TimeoutMillis = 3_200,
                    ExtraConnectivityParams = new Dictionary<string, object>
                    {
                        { "1", "1" },
                        { "2", "2,2" },
                        { "3", "3,3,3" }
                    },
                    EnsureNonNullResponse = true,
                    ResponseBodyBufferingSizeLimit = 4_300,
                }
            };
            var sendOptions = new DefaultQuasiHttpSendOptions
            {
                ExtraConnectivityParams = new Dictionary<string, object>
                {
                    { "1", "one" },
                    { "2", "two" },
                    { "4", "four" }
                },
                EnsureNonNullResponse = false,
                ResponseBodyBufferingSizeLimit = 700,
            };
            var expectedConnectivityParams = new DefaultQuasiHttpSendOptions
            {
                ExtraConnectivityParams = new Dictionary<string, object>
                {
                    { "1", "one" },
                    { "2", "two" },
                    { "3", "3,3,3" },
                    { "4", "four" }
                },
                TimeoutMillis = 3_200,
                MaxChunkSize = 0,
                ResponseBufferingEnabled = true,
                ResponseBodyBufferingSizeLimit = 700,
                EnsureNonNullResponse = false
            };

            // act
            var actualResponse = await client.Send(remoteEndpoint, request, sendOptions);

            // assert
            Assert.Same(request, actualRequest);
            ComparisonUtils.CompareConnectivityParams(
                remoteEndpoint, transportBypass.ActualRemoteEndpoint,
                expectedConnectivityParams, transportBypass.ActualSendOptions);
            Assert.Null(actualResponse);
            Assert.False(transportBypass.IsCancellationRequested);
        }

        [Fact]
        public async Task TestClientBypassTimeout1()
        {
            // act
            var remoteEndpoint = new object();
            var transportBypass = new DemoTransportBypass
            {
                SendRequestCallback = async (req) =>
                {
                    await Task.Delay(2_500);
                    return new DefaultQuasiHttpResponse();
                }
            };
            var client = new StandardQuasiHttpClient
            {
                TransportBypass = transportBypass,
                Transport = new MemoryBasedClientTransport(),
                DefaultSendOptions = new DefaultQuasiHttpSendOptions
                {
                    TimeoutMillis = 300
                }
            };
            var expectedConnectivityParams = new DefaultQuasiHttpSendOptions
            {
                ExtraConnectivityParams = new Dictionary<string, object>(),
                TimeoutMillis = 300,
                MaxChunkSize = 0,
                ResponseBufferingEnabled = true,
                ResponseBodyBufferingSizeLimit = 0,
                EnsureNonNullResponse = true
            };

            // act and begin asserting
            var actualEx = await Assert.ThrowsAsync<QuasiHttpRequestProcessingException>(() =>
                client.Send(remoteEndpoint, new DefaultQuasiHttpRequest(), null));
            Log.Info(actualEx, "actual error from TestClientBypassTimeout1");

            // assert
            ComparisonUtils.CompareConnectivityParams(
                remoteEndpoint, transportBypass.ActualRemoteEndpoint,
                expectedConnectivityParams, transportBypass.ActualSendOptions);
            Assert.Equal(QuasiHttpRequestProcessingException.ReasonCodeTimeout,
                actualEx.ReasonCode);
            Assert.False(transportBypass.IsCancellationRequested);
        }

        [Fact]
        public async Task TestClientBypassTimeout2()
        {
            // arrange
            var remoteEndpoint = new object();
            var transportBypass = new DemoTransportBypass
            {
                SendRequestCallback = async (req) =>
                {
                    await Task.Delay(2_500);
                    return new DefaultQuasiHttpResponse();
                },
                CreateCancellationHandles = true
            };
            var client = new StandardQuasiHttpClient
            {
                TransportBypass = transportBypass,
                Transport = new MemoryBasedClientTransport(),
                DefaultSendOptions = new DefaultQuasiHttpSendOptions
                {
                    TimeoutMillis = 300,
                    ResponseBufferingEnabled = false
                }
            };
            var expectedConnectivityParams = new DefaultQuasiHttpSendOptions
            {
                ExtraConnectivityParams = new Dictionary<string, object>(),
                TimeoutMillis = 300,
                MaxChunkSize = 0,
                ResponseBufferingEnabled = false,
                ResponseBodyBufferingSizeLimit = 0,
                EnsureNonNullResponse = true
            };

            // act and begin asserting
            var actualEx = await Assert.ThrowsAsync<QuasiHttpRequestProcessingException>(() =>
                client.Send(remoteEndpoint, new DefaultQuasiHttpRequest(), null));
            Log.Info(actualEx, "actual error from TestClientBypassTimeout2");

            // assert
            ComparisonUtils.CompareConnectivityParams(
                remoteEndpoint, transportBypass.ActualRemoteEndpoint,
                expectedConnectivityParams, transportBypass.ActualSendOptions);
            Assert.Equal(QuasiHttpRequestProcessingException.ReasonCodeTimeout,
                actualEx.ReasonCode);
            Assert.True(transportBypass.IsCancellationRequested);
        }

        [Fact]
        public async Task TestClientBypassCancellation()
        {
            // arrange
            var remoteEndpoint = new object();
            var transportBypass = new DemoTransportBypass
            {
                SendRequestCallback = async (req) =>
                {
                    await Task.Delay(2_500);
                    return new DefaultQuasiHttpResponse();
                },
                CreateCancellationHandles = true
            };
            var client = new StandardQuasiHttpClient
            {
                TransportBypass = transportBypass,
                DefaultSendOptions = new DefaultQuasiHttpSendOptions
                {
                    TimeoutMillis = -1
                }
            };
            var expectedConnectivityParams = new DefaultQuasiHttpSendOptions
            {
                ExtraConnectivityParams = new Dictionary<string, object>(),
                TimeoutMillis = -1,
                MaxChunkSize = 0,
                ResponseBufferingEnabled = true,
                ResponseBodyBufferingSizeLimit = 0,
                EnsureNonNullResponse = true
            };

            // act
            var result = client.Send(remoteEndpoint, _ =>
                Task.FromResult<IQuasiHttpRequest>(null), null);
            await Task.Delay(1_000);
            Assert.False(transportBypass.IsCancellationRequested);
            client.CancelSend(result.CancellationHandle);

            // assert
            var actualEx = await Assert.ThrowsAsync<QuasiHttpRequestProcessingException>(() =>
                result.ResponseTask);
            Log.Info(actualEx, "actual error from TestClientBypassCancellation");
            ComparisonUtils.CompareConnectivityParams(
                remoteEndpoint, transportBypass.ActualRemoteEndpoint,
                expectedConnectivityParams, transportBypass.ActualSendOptions);
            Assert.Equal(QuasiHttpRequestProcessingException.ReasonCodeCancelled,
                actualEx.ReasonCode);
            Assert.True(transportBypass.IsCancellationRequested);

            // test that a second cancel does not do anything.
            transportBypass.IsCancellationRequested = false;
            client.CancelSend(result.CancellationHandle);
            actualEx = await Assert.ThrowsAsync<QuasiHttpRequestProcessingException>(() =>
                result.ResponseTask);
            Assert.Equal(QuasiHttpRequestProcessingException.ReasonCodeCancelled,
                actualEx.ReasonCode);
            Assert.False(transportBypass.IsCancellationRequested);
        }

        [Fact]
        public async Task TestClientBypassNoTimeoutDueToCancellation()
        {
            // arrange
            var remoteEndpoint = new object();
            var transportBypass = new DemoTransportBypass
            {
                SendRequestCallback = async (req) =>
                {
                    await Task.Delay(2_500);
                    return new DefaultQuasiHttpResponse();
                },
                CreateCancellationHandles = true
            };
            var client = new StandardQuasiHttpClient
            {
                TransportBypass = transportBypass,
                DefaultSendOptions = new DefaultQuasiHttpSendOptions
                {
                    TimeoutMillis = 4_000,
                    EnsureNonNullResponse = false
                }
            };
            var expectedConnectivityParams = new DefaultQuasiHttpSendOptions
            {
                ExtraConnectivityParams = new Dictionary<string, object>(),
                TimeoutMillis = 4_000,
                MaxChunkSize = 0,
                ResponseBufferingEnabled = true,
                ResponseBodyBufferingSizeLimit = 0,
                EnsureNonNullResponse = false
            };

            // act
            var result = client.Send(remoteEndpoint, _ =>
                Task.FromResult<IQuasiHttpRequest>(null), null);
            await Task.Delay(1_000);
            Assert.False(transportBypass.IsCancellationRequested);
            client.CancelSend(result.CancellationHandle);

            // assert
            var actualEx = await Assert.ThrowsAsync<QuasiHttpRequestProcessingException>(() =>
                result.ResponseTask);
            Log.Info(actualEx, "actual error from TestClientBypassNoTimeoutDueToCancellation");
            ComparisonUtils.CompareConnectivityParams(
                remoteEndpoint, transportBypass.ActualRemoteEndpoint,
                expectedConnectivityParams, transportBypass.ActualSendOptions);
            Assert.Equal(QuasiHttpRequestProcessingException.ReasonCodeCancelled,
                actualEx.ReasonCode);
            Assert.True(transportBypass.IsCancellationRequested);
        }

        [Fact]
        public async Task TestServerBypass1()
        {
            var requestReleaseCallCount = 0;
            var remoteEndpoint = new object();
            var request = new ConfigurableQuasiHttpRequest
            {
                ReleaseFunc = async () =>
                {
                    requestReleaseCallCount++;
                }
            };
            var expectedResponse = new DefaultQuasiHttpResponse();
            IQuasiHttpRequest actualRequest = null;
            var server = new StandardQuasiHttpServer
            {
                Application = new ConfigurableQuasiHttpApplication
                {
                    ProcessRequestCallback = async req =>
                    {
                        actualRequest = req;
                        await Task.Delay(1_200);
                        return expectedResponse;
                    }
                }
            };
            var actualResponse = await server.AcceptRequest(request, null);
            Assert.Same(request, actualRequest);
            Assert.Same(expectedResponse, actualResponse);
            // test that it is not disposed.
            await ComparisonUtils.CompareResponses(expectedResponse,
                actualResponse, null);
            // test that transfer was aborted.
            Assert.Equal(1, requestReleaseCallCount);
        }

        [Fact]
        public async Task TestServerBypass2()
        {
            var requestReleaseCallCount = 0;
            var remoteEndpoint = new object();
            var request = new ConfigurableQuasiHttpRequest
            {
                ReleaseFunc = async () =>
                {
                    requestReleaseCallCount++;
                    throw new Exception("should be ignored");
                }
            };
            IQuasiHttpRequest actualRequest = null;
            var server = new StandardQuasiHttpServer
            {
                Application = new ConfigurableQuasiHttpApplication
                {
                    ProcessRequestCallback = async req =>
                    {
                        actualRequest = req;
                        await Task.Delay(1_200);
                        return null;
                    }
                }
            };
            var actualResponse = await server.AcceptRequest(request, null);
            Assert.Same(request, actualRequest);
            Assert.Null(actualResponse);
            // test that transfer was aborted.
            Assert.Equal(1, requestReleaseCallCount);
        }

        [Fact]
        public async Task TestServerBypassNoTimeout()
        {
            var remoteEndpoint = new object();
            var request = new DefaultQuasiHttpRequest();
            var expectedResBodyBytes = ByteUtils.StringToBytes("ideas");
            var expectedResponse = new DefaultQuasiHttpResponse
            {
                Body = new StringBody("ideas")
            };
            IQuasiHttpRequest actualRequest = null;
            var server = new StandardQuasiHttpServer
            {
                Application = new ConfigurableQuasiHttpApplication
                {
                    ProcessRequestCallback = async req =>
                    {
                        actualRequest = req;
                        await Task.Delay(1_200);
                        return expectedResponse;
                    }
                },
                DefaultProcessingOptions = new DefaultQuasiHttpProcessingOptions
                {
                    TimeoutMillis = 5_000
                }
            };
            var actualResponse = await server.AcceptRequest(request, null);
            Assert.Same(request, actualRequest);
            Assert.Same(expectedResponse, actualResponse);
            // test that it is not disposed.
            await ComparisonUtils.CompareResponses(expectedResponse,
                actualResponse, expectedResBodyBytes);
        }

        [Fact]
        public async Task TestServerBypassTimeout1()
        {
            var remoteEndpoint = new object();
            var server = new StandardQuasiHttpServer
            {
                Application = new ConfigurableQuasiHttpApplication
                {
                    ProcessRequestCallback = async req =>
                    {
                        await Task.Delay(2_800);
                        return new DefaultQuasiHttpResponse();
                    }
                },
                DefaultProcessingOptions = new DefaultQuasiHttpProcessingOptions
                {
                    TimeoutMillis = -1
                }
            };
            var receiveOptions = new DefaultQuasiHttpProcessingOptions
            {
                TimeoutMillis = 1_500
            };
            var requestReleaseCallCount = 0;
            var request = new ConfigurableQuasiHttpRequest
            {
                ReleaseFunc = async () =>
                {
                    requestReleaseCallCount++;
                }
            };
            var actualEx = await Assert.ThrowsAsync<QuasiHttpRequestProcessingException>(() =>
                server.AcceptRequest(request, receiveOptions));
            Log.Info(actualEx, "actual error from TestServerBypassTimeout1");
            Assert.Equal(QuasiHttpRequestProcessingException.ReasonCodeTimeout,
                actualEx.ReasonCode);
            // test that transfer was aborted.
            Assert.Equal(1, requestReleaseCallCount);
        }

        [Fact]
        public async Task TestServerBypassTimeout2()
        {
            var remoteEndpoint = new object();
            var server = new StandardQuasiHttpServer
            {
                Application = new ConfigurableQuasiHttpApplication
                {
                    ProcessRequestCallback = async req =>
                    {
                        await Task.Delay(2_800);
                        return new DefaultQuasiHttpResponse();
                    }
                },
                DefaultProcessingOptions = new DefaultQuasiHttpProcessingOptions
                {
                    TimeoutMillis = 1_500
                }
            };
            var receiveOptions = new DefaultQuasiHttpProcessingOptions
            {
                TimeoutMillis = 1_200
            };
            var actualEx = await Assert.ThrowsAsync<QuasiHttpRequestProcessingException>(() =>
                server.AcceptRequest(new DefaultQuasiHttpRequest(),
                receiveOptions));
            Log.Info(actualEx, "actual error from TestServerBypassTimeout2");
            Assert.Equal(QuasiHttpRequestProcessingException.ReasonCodeTimeout,
                actualEx.ReasonCode);
        }
    }
}
