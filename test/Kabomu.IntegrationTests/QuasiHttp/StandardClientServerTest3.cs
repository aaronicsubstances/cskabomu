using Kabomu.Common;
using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.Client;
using Kabomu.QuasiHttp.EntityBody;
using Kabomu.QuasiHttp.Server;
using Kabomu.QuasiHttp.Transport;
using Kabomu.Tests.Shared.Common;
using Kabomu.Tests.Shared.QuasiHttp;
using NLog;
using NLog.Fluent;
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
                }
            };
            var client = new StandardQuasiHttpClient
            {
                TransportBypass = transportBypass,
                Transport = new MemoryBasedClientTransport(),
                DefaultSendOptions = new DefaultQuasiHttpSendOptions
                {
                    ResponseBufferingEnabled = false
                }
            };
            var actualResponse = await client.Send(remoteEndpoint, request, null);
            Assert.Same(request, actualRequest);
            Assert.Same(expectedResponse, actualResponse);
            // test that it is not disposed.
            await ComparisonUtils.CompareResponses(expectedResponse,
                actualResponse, null);
        }

        [Fact]
        public async Task TestClientBypass2()
        {
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
                }
            };
            var client = new StandardQuasiHttpClient
            {
                TransportBypass = transportBypass,
                DefaultSendOptions = new DefaultQuasiHttpSendOptions
                {
                    ResponseBufferingEnabled = false
                }
            };
            Func<IDictionary<string, object>, Task<IQuasiHttpRequest>> requestFunc = (reqEnv) =>
            {
                return Task.FromResult<IQuasiHttpRequest>(request);
            };
            var result = client.Send2(remoteEndpoint, requestFunc, null);
            var actualResponse = await result.Item1;
            Assert.Same(request, actualRequest);
            Assert.Same(expectedResponse, actualResponse);
            // test that it is not disposed.
            await ComparisonUtils.CompareResponses(expectedResponse,
                actualResponse, null);
        }

        [Fact]
        public async Task TestClientBypass3()
        {
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
                TimeoutMillis = 3_000
            };
            var result = client.Send2(remoteEndpoint, requestFunc, sendOptions);
            var actualResponse = await result.Item1;
            Assert.Same(request, actualRequest);
            await ComparisonUtils.CompareResponses(expectedResponse,
                actualResponse, expectedResBodyBytes);
        }

        [Fact]
        public async Task TestClientBypass4()
        {
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
                    TimeoutMillis = 3_000
                }
            };
            var sendOptions = new DefaultQuasiHttpSendOptions();
            var actualResponse = await client.Send(remoteEndpoint, request, sendOptions);
            Assert.Same(request, actualRequest);
            await ComparisonUtils.CompareResponses(expectedResponse,
                actualResponse, expectedResBodyBytes);
        }

        [Fact]
        public async Task TestClientBypass5()
        {
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
                    TimeoutMillis = 300
                }
            };
            var sendOptions = new DefaultQuasiHttpSendOptions
            {
                TimeoutMillis = -1
            };
            var actualResponse = await client.Send(remoteEndpoint, request, sendOptions);
            Assert.Same(request, actualRequest);
            Assert.Same(expectedResponse, actualResponse);
            // test that it is not disposed.
            await ComparisonUtils.CompareResponses(expectedResponse,
                actualResponse, expectedResBodyBytes);
        }

        [Fact]
        public async Task TestClientBypassTimeout()
        {
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
            var actualEx = await Assert.ThrowsAsync<QuasiHttpRequestProcessingException>(() =>
                client.Send(remoteEndpoint, new DefaultQuasiHttpRequest(), null));
            Log.Info(actualEx, "actual error from TestClientBypassTimeout");
            Assert.Equal(QuasiHttpRequestProcessingException.ReasonCodeTimeout,
                actualEx.ReasonCode);
        }

        [Fact]
        public async Task TestClientBypassCancellation()
        {
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
            var result = client.Send2(remoteEndpoint, _ =>
                Task.FromResult<IQuasiHttpRequest>(null), null);
            await Task.Delay(1_000);
            Assert.False(transportBypass.IsCancellationRequested);
            client.CancelSend(result.Item2);
            var actualEx = await Assert.ThrowsAsync<QuasiHttpRequestProcessingException>(() =>
                result.Item1);
            Log.Info(actualEx, "actual error from TestClientBypassCancellation");
            Assert.Equal(QuasiHttpRequestProcessingException.ReasonCodeCancelled,
                actualEx.ReasonCode);
            Assert.True(transportBypass.IsCancellationRequested);
        }

        [Fact]
        public async Task TestClientBypassNoTimeoutDueToCancellation()
        {
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
                    TimeoutMillis = 4_000
                }
            };
            var result = client.Send2(remoteEndpoint, _ =>
                Task.FromResult<IQuasiHttpRequest>(null), null);
            await Task.Delay(1_000);
            Assert.False(transportBypass.IsCancellationRequested);
            client.CancelSend(result.Item2);
            var actualEx = await Assert.ThrowsAsync<QuasiHttpRequestProcessingException>(() =>
                result.Item1);
            Log.Info(actualEx, "actual error from TestClientBypassNoTimeoutDueToCancellation");
            Assert.Equal(QuasiHttpRequestProcessingException.ReasonCodeCancelled,
                actualEx.ReasonCode);
            Assert.True(transportBypass.IsCancellationRequested);
        }

        [Fact]
        public async Task TestServerBypass()
        {
            var remoteEndpoint = new object();
            var request = new DefaultQuasiHttpRequest();
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
            var actualEx = await Assert.ThrowsAsync<QuasiHttpRequestProcessingException>(() =>
                server.AcceptRequest(new DefaultQuasiHttpRequest(),
                receiveOptions));
            Log.Info(actualEx, "actual error from TestServerBypassTimeout1");
            Assert.Equal(QuasiHttpRequestProcessingException.ReasonCodeTimeout,
                actualEx.ReasonCode);
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
