using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.Client;
using Kabomu.QuasiHttp.Server;
using Kabomu.Tests.Shared.Common;
using Kabomu.Tests.Shared.QuasiHttp;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.IntegrationTests.QuasiHttp
{
    public class StandardClientServerTest2
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        [Fact]
        public async Task TestNoConnection1()
        {
            // should cause connection allocation problem from
            // null reference exception on servers property
            var clientTransport = new MemoryBasedClientTransport();

            var client = new StandardQuasiHttpClient
            {
                Transport = clientTransport
            };
            var remoteEndpoint = "seed";
            var request = new DefaultQuasiHttpRequest();
            DefaultQuasiHttpSendOptions options = null;
            var actualEx = await Assert.ThrowsAsync<QuasiHttpRequestProcessingException>(() =>
                client.Send(remoteEndpoint, request, options));
            Log.Info(actualEx, "actual error from TestNoConnection1");
            Assert.Contains("send request processing", actualEx.Message);
        }

        [Fact]
        public async Task TestNoConnection2()
        {
            var remoteEndpoint = "seed2";
            var clientTransport = new MemoryBasedClientTransport
            {
                Servers = new Dictionary<object, MemoryBasedServerTransport>
                {
                    { remoteEndpoint, null }
                }
            };
            var client = new StandardQuasiHttpClient
            {
                Transport = clientTransport
            };
            var request = new DefaultQuasiHttpRequest();
            DefaultQuasiHttpSendOptions options = null;
            var actualEx = await Assert.ThrowsAsync<QuasiHttpRequestProcessingException>(() =>
                client.Send(remoteEndpoint, request, options));
            Log.Info(actualEx, "actual error from TestNoConnection2");
            Assert.Contains("no connection", actualEx.Message);
        }

        [Fact]
        public async Task TestNoConnection3()
        {
            var remoteEndpoint = "seed3";
            var clientTransport = new MemoryBasedClientTransport
            {
                Servers = new Dictionary<object, MemoryBasedServerTransport>()
            };
            var client = new StandardQuasiHttpClient
            {
                Transport = clientTransport
            };
            var request = new DefaultQuasiHttpRequest();
            DefaultQuasiHttpSendOptions options = null;
            var actualEx = await Assert.ThrowsAsync<QuasiHttpRequestProcessingException>(() =>
                client.Send(remoteEndpoint, request, options));
            Log.Info(actualEx, "actual error from TestNoConnection3");
            Assert.Contains("no connection", actualEx.Message);
        }

        [Fact]
        public async Task TestRequestFuncYieldNoRequest1()
        {
            var remoteEndpoint = "seed3";
            var server = new MemoryBasedServerTransport
            {
                AcceptConnectionFunc = _ =>
                {
                    // do nothing.
                }
            };
            var clientTransport = new MemoryBasedClientTransport
            {
                Servers = new Dictionary<object, MemoryBasedServerTransport>
                {
                    { remoteEndpoint, server }
                }
            };
            var client = new StandardQuasiHttpClient
            {
                Transport = clientTransport
            };
            IDictionary<string, object> actualReqEnv = null;
            Func<IDictionary<string, object>, Task<IQuasiHttpRequest>> requestFunc = reqEnv =>
            {
                actualReqEnv = reqEnv;
                return Task.FromResult<IQuasiHttpRequest>(null);
            };
            var options = new DefaultQuasiHttpSendOptions
            {
                ExtraConnectivityParams = new Dictionary<string, object>
                {
                    { "scheme", "https" }
                }
            };
            var actualEx = await Assert.ThrowsAsync<QuasiHttpRequestProcessingException>(() =>
                client.Send2(remoteEndpoint, requestFunc, options).Item1);
            Log.Info(actualEx, "actual error from TestRequestFuncYieldNoRequest1");
            Assert.Contains("no request", actualEx.Message);

            // also check the environment passed to the request func.
            Assert.Equal(options.ExtraConnectivityParams, actualReqEnv);
        }

        [Fact]
        public async Task TestRequestFuncYieldNoRequest2()
        {
            var remoteEndpoint = 345;
            var server = new MemoryBasedServerTransport
            {
                AcceptConnectionFunc = _ =>
                {
                    // do nothing.
                }
            };
            var clientTransport = new MemoryBasedClientTransport
            {
                Servers = new Dictionary<object, MemoryBasedServerTransport>
                {
                    { remoteEndpoint, server }
                }
            };
            var client = new StandardQuasiHttpClient
            {
                Transport = clientTransport,
                DefaultSendOptions = new DefaultQuasiHttpSendOptions
                {
                    ExtraConnectivityParams = new Dictionary<string, object>
                    {
                        { "one", 1 },
                        { "scheme", "plus" }
                    }
                }
            };
            IDictionary<string, object> actualReqEnv = null;
            Func<IDictionary<string, object>, Task<IQuasiHttpRequest>> requestFunc = reqEnv =>
            {
                actualReqEnv = reqEnv;
                throw new InvalidOperationException("error from req func");
            };
            var options = new DefaultQuasiHttpSendOptions
            {
                ExtraConnectivityParams = new Dictionary<string, object>
                {
                    { "scheme", "https" }
                }
            };
            var actualEx = await Assert.ThrowsAsync<QuasiHttpRequestProcessingException>(() =>
                client.Send2(remoteEndpoint, requestFunc, options).Item1);
            Log.Info(actualEx, "actual error from TestRequestFuncYieldNoRequest2");
            Assert.Contains("send request processing", actualEx.Message);
            Assert.Equal("error from req func", actualEx.InnerException?.Message);

            // check that the environment is passed in has been merged correctly
            // with default.
            var expectedReqEnv = new Dictionary<string, object>
            {
                { "scheme", "https" },
                { "one", 1 }
            };
            Assert.Equal(expectedReqEnv, actualReqEnv);
        }

        [Fact]
        public async Task TestRequestFuncYield()
        {
            var remoteEndpoint = new List<string>();
            var expectedResponse = new DefaultQuasiHttpResponse();
            IQuasiHttpRequest actualRequest = null;
            var server = new StandardQuasiHttpServer
            {
                Application = new ConfigurableQuasiHttpApplication
                {
                    ProcessRequestCallback = req =>
                    {
                        actualRequest = req;
                        return Task.FromResult<IQuasiHttpResponse>(expectedResponse);
                    }
                }
            };
            Task serverTask = null;
            server.Transport = new MemoryBasedServerTransport
            {
                AcceptConnectionFunc = c =>
                {
                    serverTask = server.AcceptConnection(c);
                }
            };
            var clientTransport = new MemoryBasedClientTransport
            {
                Servers = new Dictionary<object, MemoryBasedServerTransport>
                {
                    { remoteEndpoint, (MemoryBasedServerTransport)server.Transport }
                }
            };
            var client = new StandardQuasiHttpClient
            {
                Transport = clientTransport
            };
            var expectedRequest = new DefaultQuasiHttpRequest
            {
                Environment = new Dictionary<string, object>()
            };
            IDictionary<string, object> actualReqEnv = null;
            Func<IDictionary<string, object>, Task<IQuasiHttpRequest>> requestFunc = reqEnv =>
            {
                actualReqEnv = reqEnv;
                return Task.FromResult<IQuasiHttpRequest>(expectedRequest);
            };
            var result = client.Send2(remoteEndpoint, requestFunc, null);
            var actualResponse = await result.Item1;
            Assert.NotNull(serverTask);
            await serverTask;
            await ComparisonUtils.CompareRequests(expectedRequest, actualRequest, null);
            await ComparisonUtils.CompareResponses(expectedResponse, actualResponse, null);

            // check that empty default environment is passed in to req func,
            // since MemoryBasedClientTransport uses merged connectivity params
            Assert.Equal(new Dictionary<string, object>(), actualReqEnv);
        }

        [Fact]
        public async Task TestNoTimeout1()
        {
            var remoteEndpoint = new List<string>();
            var expectedResponse = new DefaultQuasiHttpResponse();
            IQuasiHttpRequest actualRequest = null;
            var server = new StandardQuasiHttpServer
            {
                Application = new ConfigurableQuasiHttpApplication
                {
                    ProcessRequestCallback = async req =>
                    {
                        actualRequest = req;
                        await Task.Delay(2000);
                        return expectedResponse;
                    }
                },
                DefaultProcessingOptions = new DefaultQuasiHttpProcessingOptions
                {
                    TimeoutMillis = 4_000
                }
            };
            Task serverTask = null;
            server.Transport = new MemoryBasedServerTransport
            {
                AcceptConnectionFunc = c =>
                {
                    serverTask = server.AcceptConnection(c);
                }
            };
            var clientTransport = new MemoryBasedClientTransport
            {
                Servers = new Dictionary<object, MemoryBasedServerTransport>
                {
                    { remoteEndpoint, (MemoryBasedServerTransport)server.Transport }
                }
            };
            var client = new StandardQuasiHttpClient
            {
                Transport = clientTransport,
                DefaultSendOptions = new DefaultQuasiHttpSendOptions
                {
                    TimeoutMillis = 5_000
                }
            };
            var expectedRequest = new DefaultQuasiHttpRequest
            {
                Environment = new Dictionary<string, object>()
            };
            IDictionary<string, object> actualReqEnv = null;
            Func<IDictionary<string, object>, Task<IQuasiHttpRequest>> requestFunc = async reqEnv =>
            {
                actualReqEnv = reqEnv;
                await Task.Delay(1000);
                return expectedRequest;
            };
            var sendOptions = new DefaultQuasiHttpSendOptions();
            var result = client.Send2(remoteEndpoint, requestFunc, sendOptions);
            var actualResponse = await result.Item1;
            Assert.NotNull(serverTask);
            await serverTask;
            await ComparisonUtils.CompareRequests(expectedRequest, actualRequest, null);
            await ComparisonUtils.CompareResponses(expectedResponse, actualResponse, null);
        }

        [Fact]
        public async Task TestNoTimeout2()
        {
            var remoteEndpoint = new List<string>();
            var expectedResponse = new DefaultQuasiHttpResponse();
            IQuasiHttpRequest actualRequest = null;
            var server = new StandardQuasiHttpServer
            {
                Application = new ConfigurableQuasiHttpApplication
                {
                    ProcessRequestCallback = async req =>
                    {
                        actualRequest = req;
                        await Task.Delay(2000);
                        return expectedResponse;
                    }
                }
            };
            Task serverTask = null;
            server.Transport = new MemoryBasedServerTransport
            {
                AcceptConnectionFunc = c =>
                {
                    serverTask = server.AcceptConnection(c);
                }
            };
            var clientTransport = new MemoryBasedClientTransport
            {
                Servers = new Dictionary<object, MemoryBasedServerTransport>
                {
                    { remoteEndpoint, (MemoryBasedServerTransport)server.Transport }
                }
            };
            var client = new StandardQuasiHttpClient
            {
                Transport = clientTransport
            };
            var expectedRequest = new DefaultQuasiHttpRequest
            {
                Environment = new Dictionary<string, object>()
            };
            IDictionary<string, object> actualReqEnv = null;
            Func<IDictionary<string, object>, Task<IQuasiHttpRequest>> requestFunc = async reqEnv =>
            {
                actualReqEnv = reqEnv;
                await Task.Delay(1000);
                return expectedRequest;
            };
            var result = client.Send2(remoteEndpoint, requestFunc, null);
            var actualResponse = await result.Item1;
            Assert.NotNull(serverTask);
            await serverTask;
            await ComparisonUtils.CompareRequests(expectedRequest, actualRequest, null);
            await ComparisonUtils.CompareResponses(expectedResponse, actualResponse, null);
        }

        [Fact]
        public async Task TestCancellation()
        {
            var remoteEndpoint = new object();
            var server = new StandardQuasiHttpServer
            {
                Application = new ConfigurableQuasiHttpApplication
                {
                    ProcessRequestCallback = async req =>
                    {
                        await Task.Delay(2_000);
                        return new DefaultQuasiHttpResponse();
                    }
                },
                DefaultProcessingOptions = new DefaultQuasiHttpProcessingOptions
                {
                    TimeoutMillis = -1
                }
            };
            server.Transport = new MemoryBasedServerTransport
            {
                AcceptConnectionFunc = c =>
                {
                    // don't wait.
                    _ = server.AcceptConnection(c);
                }
            };
            var clientTransport = new MemoryBasedClientTransport
            {
                Servers = new Dictionary<object, MemoryBasedServerTransport>
                {
                    { remoteEndpoint, (MemoryBasedServerTransport)server.Transport }
                }
            };
            var client = new StandardQuasiHttpClient
            {
                Transport = clientTransport,
                DefaultSendOptions = new DefaultQuasiHttpSendOptions
                {
                    TimeoutMillis = -1
                }
            };
            Func<IDictionary<string, object>, Task<IQuasiHttpRequest>> requestFunc = async reqEnv =>
            {
                return new DefaultQuasiHttpRequest();
            };
            var sendOptions = new DefaultQuasiHttpSendOptions();
            var result = client.Send2(remoteEndpoint, requestFunc, sendOptions);
            await Task.Delay(1000);
            client.CancelSend(result.Item2);
            var actualEx = await Assert.ThrowsAsync<QuasiHttpRequestProcessingException>(() =>
                result.Item1);
            Log.Info(actualEx, "actual error from TestCancellation");
            Assert.Equal(QuasiHttpRequestProcessingException.ReasonCodeCancelled,
                actualEx.ReasonCode);
        }

        [Fact]
        public async Task TestNoTimeoutDueToCancellation()
        {
            var remoteEndpoint = new List<string>();
            var server = new StandardQuasiHttpServer
            {
                Application = new ConfigurableQuasiHttpApplication
                {
                    ProcessRequestCallback = async req =>
                    {
                        await Task.Delay(2_000);
                        return new DefaultQuasiHttpResponse();
                    }
                },
                DefaultProcessingOptions = new DefaultQuasiHttpProcessingOptions
                {
                    TimeoutMillis = 4_000
                }
            };
            server.Transport = new MemoryBasedServerTransport
            {
                AcceptConnectionFunc = c =>
                {
                    // don't wait.
                    _ = server.AcceptConnection(c);
                }
            };
            var clientTransport = new MemoryBasedClientTransport
            {
                Servers = new Dictionary<object, MemoryBasedServerTransport>
                {
                    { remoteEndpoint, (MemoryBasedServerTransport)server.Transport }
                }
            };
            var client = new StandardQuasiHttpClient
            {
                Transport = clientTransport,
                DefaultSendOptions = new DefaultQuasiHttpSendOptions
                {
                    TimeoutMillis = 5_000
                }
            };
            Func<IDictionary<string, object>, Task<IQuasiHttpRequest>> requestFunc = async reqEnv =>
            {
                await Task.Delay(1000);
                return new DefaultQuasiHttpRequest();
            };
            var sendOptions = new DefaultQuasiHttpSendOptions();
            var result = client.Send2(remoteEndpoint, requestFunc, sendOptions);
            await Task.Delay(1000);
            client.CancelSend(result.Item2);
            var actualEx = await Assert.ThrowsAsync<QuasiHttpRequestProcessingException>(() =>
                result.Item1);
            Log.Info(actualEx, "actual error from TestNoTimeoutDueToCancellation");
            Assert.Equal(QuasiHttpRequestProcessingException.ReasonCodeCancelled,
                actualEx.ReasonCode);
        }

        [Fact]
        public async Task TestTimeout1()
        {
            var remoteEndpoint = new List<string>();
            var server = new StandardQuasiHttpServer
            {
                Application = new ConfigurableQuasiHttpApplication
                {
                    ProcessRequestCallback = async req =>
                    {
                        await Task.Delay(4_000);
                        return new DefaultQuasiHttpResponse();
                    }
                },
                DefaultProcessingOptions = new DefaultQuasiHttpProcessingOptions
                {
                    TimeoutMillis = -1
                }
            };
            server.Transport = new MemoryBasedServerTransport
            {
                AcceptConnectionFunc = c =>
                {
                    // don't wait.
                    _ = server.AcceptConnection(c);
                }
            };
            var clientTransport = new MemoryBasedClientTransport
            {
                Servers = new Dictionary<object, MemoryBasedServerTransport>
                {
                    { remoteEndpoint, (MemoryBasedServerTransport)server.Transport }
                }
            };
            var client = new StandardQuasiHttpClient
            {
                Transport = clientTransport,
                DefaultSendOptions = new DefaultQuasiHttpSendOptions
                {
                    TimeoutMillis = 3_000
                }
            };
            Func<IDictionary<string, object>, Task<IQuasiHttpRequest>> requestFunc = async reqEnv =>
            {
                await Task.Delay(1000);
                return new DefaultQuasiHttpRequest();
            };
            var sendOptions = new DefaultQuasiHttpSendOptions();
            var result = client.Send2(remoteEndpoint, requestFunc, sendOptions);
            var actualEx = await Assert.ThrowsAsync<QuasiHttpRequestProcessingException>(() =>
                result.Item1);
            Log.Info(actualEx, "actual error from TestTimeout1");
            Assert.Equal(QuasiHttpRequestProcessingException.ReasonCodeTimeout,
                actualEx.ReasonCode);
        }

        [Fact]
        public async Task TestTimeout2()
        {
            var remoteEndpoint = new List<string>();
            var server = new StandardQuasiHttpServer
            {
                Application = new ConfigurableQuasiHttpApplication
                {
                    ProcessRequestCallback = async req =>
                    {
                        await Task.Delay(4_000);
                        return new DefaultQuasiHttpResponse();
                    }
                },
                DefaultProcessingOptions = new DefaultQuasiHttpProcessingOptions
                {
                    TimeoutMillis = -1
                }
            };
            Task serverTask = null;
            server.Transport = new MemoryBasedServerTransport
            {
                AcceptConnectionFunc = c =>
                {
                    serverTask = server.AcceptConnection(c);
                }
            };
            var clientTransport = new MemoryBasedClientTransport
            {
                Servers = new Dictionary<object, MemoryBasedServerTransport>
                {
                    { remoteEndpoint, (MemoryBasedServerTransport)server.Transport }
                }
            };
            var client = new StandardQuasiHttpClient
            {
                Transport = clientTransport,
                DefaultSendOptions = new DefaultQuasiHttpSendOptions
                {
                    TimeoutMillis = 30_000
                }
            };
            var sendOptions = new DefaultQuasiHttpSendOptions
            {
                TimeoutMillis = 3_000
            };
            var actualEx = await Assert.ThrowsAsync<QuasiHttpRequestProcessingException>(() =>
                client.Send(remoteEndpoint, new DefaultQuasiHttpRequest(),
                sendOptions));
            Log.Info(actualEx, "actual error from TestTimeout2");
            Assert.Equal(QuasiHttpRequestProcessingException.ReasonCodeTimeout,
                actualEx.ReasonCode);
            Assert.NotNull(serverTask);
            actualEx = await Assert.ThrowsAsync<QuasiHttpRequestProcessingException>(() =>
                serverTask);
            Log.Info(actualEx, "actual server error from TestTimeout2");
        }

        [Fact]
        public async Task TestTimeout3()
        {
            var remoteEndpoint = "127.0.0.1";
            var server = new StandardQuasiHttpServer
            {
                Application = new ConfigurableQuasiHttpApplication
                {
                    ProcessRequestCallback = async req =>
                    {
                        await Task.Delay(4_000);
                        return new DefaultQuasiHttpResponse();
                    }
                },
                DefaultProcessingOptions = new DefaultQuasiHttpProcessingOptions
                {
                    TimeoutMillis = 2_000
                }
            };
            Task serverTask = null;
            server.Transport = new MemoryBasedServerTransport
            {
                AcceptConnectionFunc = c =>
                {
                    serverTask = server.AcceptConnection(c);
                }
            };
            var clientTransport = new MemoryBasedClientTransport
            {
                Servers = new Dictionary<object, MemoryBasedServerTransport>
                {
                    { remoteEndpoint, (MemoryBasedServerTransport)server.Transport }
                }
            };
            var client = new StandardQuasiHttpClient
            {
                Transport = clientTransport,
                DefaultSendOptions = new DefaultQuasiHttpSendOptions
                {
                    TimeoutMillis = 3_000
                }
            };
            var sendOptions = new DefaultQuasiHttpSendOptions
            {
                TimeoutMillis = -1
            };
            var actualEx = await Assert.ThrowsAsync<QuasiHttpRequestProcessingException>(() =>
                client.Send(remoteEndpoint, new DefaultQuasiHttpRequest(),
                sendOptions));
            Log.Info(actualEx, "actual client error from TestTimeout3");
            Assert.NotNull(serverTask);
            actualEx = await Assert.ThrowsAsync<QuasiHttpRequestProcessingException>(() =>
                serverTask);
            Log.Info(actualEx, "actual server error from TestTimeout3");
            Assert.Equal(QuasiHttpRequestProcessingException.ReasonCodeTimeout,
                actualEx.ReasonCode);
        }

        [Fact]
        public async Task TestTimeout4()
        {
            var remoteEndpoint = "127.0.0.2";
            var server = new StandardQuasiHttpServer
            {
                Application = new ConfigurableQuasiHttpApplication
                {
                    ProcessRequestCallback = async req =>
                    {
                        await Task.Delay(4_000);
                        return new DefaultQuasiHttpResponse();
                    }
                },
                DefaultProcessingOptions = new DefaultQuasiHttpProcessingOptions
                {
                    TimeoutMillis = 2_000
                }
            };
            Task serverTask = null;
            server.Transport = new MemoryBasedServerTransport
            {
                AcceptConnectionFunc = c =>
                {
                    serverTask = server.AcceptConnection(c);
                }
            };
            var clientTransport = new MemoryBasedClientTransport
            {
                Servers = new Dictionary<object, MemoryBasedServerTransport>
                {
                    { remoteEndpoint, (MemoryBasedServerTransport)server.Transport }
                }
            };
            var client = new StandardQuasiHttpClient
            {
                Transport = clientTransport,
                DefaultSendOptions = new DefaultQuasiHttpSendOptions
                {
                    TimeoutMillis = 3_000
                }
            };
            var sendOptions = new DefaultQuasiHttpSendOptions
            {
                TimeoutMillis = 6_000
            };
            var actualEx = await Assert.ThrowsAsync<QuasiHttpRequestProcessingException>(() =>
                client.Send(remoteEndpoint, new DefaultQuasiHttpRequest(),
                sendOptions));
            Log.Info(actualEx, "actual client error from TestTimeout4");
            Assert.NotNull(serverTask);
            actualEx = await Assert.ThrowsAsync<QuasiHttpRequestProcessingException>(() =>
                serverTask);
            Log.Info(actualEx, "actual server error from TestTimeout4");
            Assert.Equal(QuasiHttpRequestProcessingException.ReasonCodeTimeout,
                actualEx.ReasonCode);
        }
    }
}
