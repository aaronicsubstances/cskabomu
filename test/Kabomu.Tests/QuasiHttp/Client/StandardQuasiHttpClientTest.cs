using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.Client;
using Kabomu.QuasiHttp.EntityBody;
using Kabomu.QuasiHttp.Exceptions;
using Kabomu.QuasiHttp.Transport;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.QuasiHttp.Client
{
    public class StandardQuasiHttpClientTest
    {
        public StandardQuasiHttpClientTest()
        {

        }

        [Fact]
        public async Task TestDirectSend1()
        {
            // arrange
            var expectedResponse = new TestQuasiHttpResponse();
            var cancelCallCounter = new MutableInt();
            var sendCallCounter = new MutableInt();
            var transfers = new List<SendTransferInternal>();
            Func<SendTransferInternal, ISendProtocolInternal> altProtocolFactory = transfer =>
            {
                transfers.Add(transfer);
                return new TestSendProtocolInternal
                {
                    ResponseDelay = 50,
                    ResponseToReturn = expectedResponse,
                    ResponseBufferingApplied = null,
                    CancelCallCounter = cancelCallCounter,
                    SendCallCounter = sendCallCounter,
                };
            };
            DefaultQuasiHttpSendOptions defaultSendOptions = null;
            IQuasiHttpAltTransport transportBypass = new ConfigurableQuasiHttpTransport();

            var instance = new StandardQuasiHttpClient
            {
                //TimerApi = testEventLoopApi,
                AltProtocolFactory = altProtocolFactory,
                TransportBypass = transportBypass,
                DefaultSendOptions = defaultSendOptions,
            };
            object remoteEndpoint = "localhost";
            IQuasiHttpRequest request = new DefaultQuasiHttpRequest();
            IQuasiHttpSendOptions sendOptions = null;

            // act
            Task<IQuasiHttpResponse> sendTask = instance.Send(remoteEndpoint, request, sendOptions);
            IQuasiHttpResponse res = await MiscUtils.EnsureCompletedTask(sendTask);

            // assert
            Assert.Same(expectedResponse, res);
            Assert.Equal(new MutableInt(1), sendCallCounter);
            Assert.Equal(new MutableInt(1), cancelCallCounter);
            Assert.Equal(0, expectedResponse.CloseCallCount);

            Assert.Single(transfers);
            var expectedTransfer = new SendTransferInternal
            {
                IsAborted = true,
                Request = request,
                TimeoutMillis = 0,
                ConnectivityParams = new DefaultConnectivityParams
                {
                    RemoteEndpoint = remoteEndpoint,
                    ExtraParams = new Dictionary<string, object>()
                },
                Connection = null,
                MaxChunkSize = 8_192,
                ResponseBufferingEnabled = true,
                ResponseBodyBufferingSizeLimit = 134_217_728
            };
            ComparisonUtils.CompareSendTransfers(expectedTransfer, transfers[0]);
            Assert.Null(transfers[0].CancellationTcs);
        }

        [Fact]
        public async Task TestDirectSend2()
        {
            var cancelCallCounter = new MutableInt();
            var sendCallCounter = new MutableInt();
            var expectedResponse = new TestQuasiHttpResponse();
            var transfers = new List<SendTransferInternal>();
            Func<SendTransferInternal, ISendProtocolInternal> altProtocolFactory = transfer =>
            {
                transfers.Add(transfer);
                return new TestSendProtocolInternal
                {
                    CancelCallCounter = cancelCallCounter,
                    SendCallCounter = sendCallCounter,
                    ResponseDelay = 79,
                    ResponseToReturn = expectedResponse,
                    ResponseBufferingApplied = null
                };
            };
            var defaultSendOptions = new DefaultQuasiHttpSendOptions
            {
                TimeoutMillis = 80,
                ExtraConnectivityParams = new Dictionary<string, object>
                {
                    { "k", 1 }, { "u7", false }
                }
            };
            IQuasiHttpAltTransport transportBypass = new ConfigurableQuasiHttpTransport();

            var instance = new StandardQuasiHttpClient
            {
                AltProtocolFactory = altProtocolFactory,
                TransportBypass = transportBypass,
                DefaultSendOptions = defaultSendOptions
            };
            object remoteEndpoint = null;
            IQuasiHttpRequest request = new DefaultQuasiHttpRequest();
            IQuasiHttpSendOptions sendOptions = new DefaultQuasiHttpSendOptions
            {
                MaxChunkSize = 90,
                ResponseBufferingEnabled = true,
                ResponseBodyBufferingSizeLimit = 120
            };
            Task<IQuasiHttpResponse> sendTask = instance.Send(remoteEndpoint, request, sendOptions);

            IQuasiHttpResponse res = await MiscUtils.EnsureCompletedTask(sendTask);
            Assert.Same(expectedResponse, res);
            Assert.Equal(new MutableInt(1), sendCallCounter);
            Assert.Equal(new MutableInt(1), cancelCallCounter);
            Assert.Equal(0, expectedResponse.CloseCallCount);

            Assert.Single(transfers);
            var expectedTransfer = new SendTransferInternal
            {
                IsAborted = true,
                Request = request,
                TimeoutMillis = 80,
                ConnectivityParams = new DefaultConnectivityParams
                {
                    RemoteEndpoint = remoteEndpoint,
                    ExtraParams = new Dictionary<string, object>
                    {
                        { "k", 1 }, { "u7", false }
                    }
                },
                Connection = null,
                MaxChunkSize = 90,
                ResponseBufferingEnabled = true,
                ResponseBodyBufferingSizeLimit = 120
            };
            ComparisonUtils.CompareSendTransfers(expectedTransfer, transfers[0]);
            Assert.NotNull(transfers[0].CancellationTcs);
        }

        [Fact]
        public async Task TestDirectSend3()
        {
            var cancelCallCounter = new MutableInt();
            var sendCallCounter = new MutableInt();
            var expectedResponse = new TestQuasiHttpResponse();
            var transfers = new List<SendTransferInternal>();
            Func<SendTransferInternal, ISendProtocolInternal> altProtocolFactory = transfer =>
            {
                transfers.Add(transfer);
                return new TestSendProtocolInternal
                {
                    CancelCallCounter = cancelCallCounter,
                    SendCallCounter = sendCallCounter,
                    ResponseDelay = 80,
                    ResponseToReturn = expectedResponse,
                    ResponseBufferingApplied = null
                };
            };
            DefaultQuasiHttpSendOptions defaultSendOptions = new DefaultQuasiHttpSendOptions
            {
                TimeoutMillis = 90,
                MaxChunkSize = 50,
                ResponseBodyBufferingSizeLimit = 60,
                ResponseBufferingEnabled = true
            };
            IQuasiHttpAltTransport transportBypass = new ConfigurableQuasiHttpTransport();

            var instance = new StandardQuasiHttpClient
            {
                AltProtocolFactory = altProtocolFactory,
                TransportBypass = transportBypass,
                DefaultSendOptions = defaultSendOptions
            };
            object remoteEndpoint = null;
            var bodyEndCallCount = new MutableInt();
            IQuasiHttpRequest request = new DefaultQuasiHttpRequest
            {
                Body = new ConfigurableQuasiHttpBody
                {
                    EndReadCallback = async () => bodyEndCallCount.Increment()
                }
            };
            IQuasiHttpSendOptions sendOptions = new DefaultQuasiHttpSendOptions
            {
                TimeoutMillis = 75,
                MaxChunkSize = 50,
                ResponseBodyBufferingSizeLimit = 60,
                ResponseBufferingEnabled = false,
                ExtraConnectivityParams =  new Dictionary<string, object>
                {
                    { "k", 1 }, { "u7", false }
                }
            };
            Task<IQuasiHttpResponse> sendTask = instance.Send(remoteEndpoint, request, sendOptions);

            var sendError = await Assert.ThrowsAsync<QuasiHttpRequestProcessingException>(() =>
            {
                return MiscUtils.EnsureCompletedTask(sendTask);
            });
            Assert.Equal(QuasiHttpRequestProcessingException.ReasonCodeTimeout, sendError.ReasonCode);
            Assert.Equal(new MutableInt(1), sendCallCounter);
            Assert.Equal(new MutableInt(1), cancelCallCounter);
            Assert.Equal(new MutableInt(1), bodyEndCallCount);
            Assert.Equal(1, expectedResponse.CloseCallCount);

            Assert.Single(transfers);
            var expectedTransfer = new SendTransferInternal
            {
                IsAborted = true,
                Request = request,
                TimeoutMillis = 75,
                ConnectivityParams = new DefaultConnectivityParams
                {
                    RemoteEndpoint = remoteEndpoint,
                    ExtraParams = new Dictionary<string, object>
                    {
                        { "k", 1 }, { "u7", false }
                    }
                },
                Connection = null,
                MaxChunkSize = 50,
                ResponseBufferingEnabled = false,
                ResponseBodyBufferingSizeLimit = 60
            };
            ComparisonUtils.CompareSendTransfers(expectedTransfer, transfers[0]);
            Assert.NotNull(transfers[0].CancellationTcs);
        }

        [Fact]
        public async Task TestDirectSend4()
        {
            var cancelCallCounter = new MutableInt();
            var sendCallCounter = new MutableInt();
            var lateResponse = new TestQuasiHttpResponse();
            var transfers = new List<SendTransferInternal>();
            Func<SendTransferInternal, ISendProtocolInternal> altProtocolFactory = transfer =>
            {
                transfers.Add(transfer);
                return new TestSendProtocolInternal
                {
                    CancelCallCounter = cancelCallCounter,
                    SendCallCounter = sendCallCounter,
                    ResponseDelay = 50,
                    ResponseToReturn = lateResponse,
                    ResponseBufferingApplied = null
                };
            };
            DefaultQuasiHttpSendOptions defaultSendOptions = new DefaultQuasiHttpSendOptions
            {
                TimeoutMillis = 90,
                MaxChunkSize = 60,
                ResponseBodyBufferingSizeLimit = 125,
                ExtraConnectivityParams = new Dictionary<string, object>
                {
                    { "kb", "ytr" }, { "u7", false }
                }
            };
            IQuasiHttpAltTransport transportBypass = new ConfigurableQuasiHttpTransport();

            var instance = new StandardQuasiHttpClient
            {
                AltProtocolFactory = altProtocolFactory,
                TransportBypass = transportBypass,
                DefaultSendOptions = defaultSendOptions
            };
            object remoteEndpoint = "s34";
            IQuasiHttpRequest request = new DefaultQuasiHttpRequest();
            IQuasiHttpSendOptions sendOptions = new DefaultQuasiHttpSendOptions
            {
                MaxChunkSize = 75,
                TimeoutMillis = 0,
                ResponseBufferingEnabled = null,
                ExtraConnectivityParams = new Dictionary<string, object>
                {
                    { "k", 1 }, { "u7", true }
                }
            };
            Task<IQuasiHttpResponse> sendTask = Task.Delay(10).ContinueWith(
                _ =>
                {
                    var res = instance.Send2(remoteEndpoint, request, sendOptions);
                    Task.Delay(30).ContinueWith(_ =>
                    {
                        instance.CancelSend(res.Item2);
                        return Task.CompletedTask;
                    });
                    return res.Item1;
                }).Unwrap();
            var sendError = await Assert.ThrowsAsync<QuasiHttpRequestProcessingException>(() =>
            {
                return MiscUtils.EnsureCompletedTask(sendTask);
            });
            Assert.Equal(QuasiHttpRequestProcessingException.ReasonCodeCancelled, sendError.ReasonCode);
            Assert.Equal(new MutableInt(1), sendCallCounter);
            Assert.Equal(new MutableInt(1), cancelCallCounter);
            Assert.Equal(1, lateResponse.CloseCallCount);

            Assert.Single(transfers);
            var expectedTransfer = new SendTransferInternal
            {
                IsAborted = true,
                Request = request,
                TimeoutMillis = 90,
                ConnectivityParams = new DefaultConnectivityParams
                {
                    RemoteEndpoint = remoteEndpoint,
                    ExtraParams = new Dictionary<string, object>
                    {
                        { "k", 1 }, { "kb", "ytr" }, { "u7", true }
                    }
                },
                Connection = null,
                MaxChunkSize = 75,
                ResponseBufferingEnabled = true,
                ResponseBodyBufferingSizeLimit = 125
            };
            ComparisonUtils.CompareSendTransfers(expectedTransfer, transfers[0]);
            Assert.NotNull(transfers[0].CancellationTcs);
        }

        [Fact]
        public async Task TestDefaultSend1()
        {
            var cancelCallCounter = new MutableInt();
            var sendCallCounter = new MutableInt();
            var expectedResponse = new DefaultQuasiHttpResponse();
            var transfers = new List<SendTransferInternal>();
            Func<SendTransferInternal, ISendProtocolInternal> defaultProtocolFactory = transfer =>
            {
                transfers.Add(transfer);
                return new TestSendProtocolInternal
                {
                    CancelCallCounter = cancelCallCounter,
                    SendCallCounter = sendCallCounter,
                    ResponseDelay = 15,
                    ResponseToReturn = expectedResponse,
                    ResponseBufferingApplied = false
                };
            };
            DefaultQuasiHttpSendOptions defaultSendOptions = new DefaultQuasiHttpSendOptions
            {
                TimeoutMillis = 90,
                MaxChunkSize = 60,
                ResponseBodyBufferingSizeLimit = 125,
                ExtraConnectivityParams = new Dictionary<string, object>
                {
                    { "kb", "ytr" }, { "u7", false }, { "0", true }, { "79", 97 }
                }
            };
            object connection = "67.89";
            IDictionary<string, object> requestEnv = new Dictionary<string, object>
            {
                { "is_secure", true }
            };
            IQuasiHttpClientTransport transport = new ConfigurableQuasiHttpTransport
            {
                AllocateConnectionCallback = async (paramsC) =>
                {
                    await Task.Delay(15);
                    return new DefaultConnectionAllocationResponse
                    {
                        Connection = connection,
                        Environment = requestEnv
                    };
                }
            };

            var instance = new StandardQuasiHttpClient
            {
                DefaultProtocolFactory = defaultProtocolFactory,
                Transport = transport,
                DefaultSendOptions = defaultSendOptions
            };
            object remoteEndpoint = "senty";
            var bodyEndCallCount = new MutableInt();
            IQuasiHttpRequest request = new DefaultQuasiHttpRequest
            {
                Body = new ConfigurableQuasiHttpBody
                {
                    EndReadCallback = () =>
                    {
                        bodyEndCallCount.Increment();
                        return Task.CompletedTask;
                    }
                },
                Environment = requestEnv
            };
            IQuasiHttpSendOptions sendOptions = new DefaultQuasiHttpSendOptions
            {
                TimeoutMillis = 50,
                ExtraConnectivityParams = new Dictionary<string, object>
                {
                    { "0", true }, { "79", 97 }
                },
                ResponseBufferingEnabled = false
            };
            Task<IQuasiHttpResponse> sendTask = Task.Delay(5).ContinueWith(
                _ =>
                {
                    var res = instance.Send2(remoteEndpoint, request, sendOptions);
                    Task.Delay(80).ContinueWith(_ => instance.CancelSend(res.Item2));
                    return res.Item1;
                }).Unwrap();
            var response = await MiscUtils.EnsureCompletedTask(sendTask);
            Assert.Same(expectedResponse, response);
            Assert.Equal(new MutableInt(1), sendCallCounter);
            Assert.Equal(new MutableInt(1), cancelCallCounter);
            Assert.Equal(new MutableInt(1), bodyEndCallCount);

            Assert.Single(transfers);
            var expectedTransfer = new SendTransferInternal
            {
                IsAborted = true,
                Request = request,
                TimeoutMillis = 50,
                ConnectivityParams = new DefaultConnectivityParams
                {
                    RemoteEndpoint = remoteEndpoint,
                    ExtraParams = new Dictionary<string, object>
                    {
                        { "kb", "ytr" }, { "u7", false }, { "0", true }, { "79", 97 }
                    }
                },
                Connection = connection,
                MaxChunkSize = 60,
                ResponseBufferingEnabled = false,
                ResponseBodyBufferingSizeLimit = 125
            };
            ComparisonUtils.CompareSendTransfers(expectedTransfer, transfers[0]);
            Assert.NotNull(transfers[0].CancellationTcs);
        }

        [Fact]
        public async Task TestDefaultSend2()
        {
            var cancelCallCounter = new MutableInt();
            var sendCallCounter = new MutableInt();
            var expectedResponse = new TestQuasiHttpResponse
            {
                Body = new StringBody("data")
            };
            var transfers = new List<SendTransferInternal>();
            Func<SendTransferInternal, ISendProtocolInternal> defaultProtocolFactory = transfer =>
            {
                transfers.Add(transfer);
                return new TestSendProtocolInternal
                {
                    CancelCallCounter = cancelCallCounter,
                    SendCallCounter = sendCallCounter,
                    ResponseDelay = 30,
                    ResponseToReturn = expectedResponse,
                    ResponseBufferingApplied = false
                };
            };
            DefaultQuasiHttpSendOptions defaultSendOptions = new DefaultQuasiHttpSendOptions
            {
                TimeoutMillis = 90
            };
            object connection = "102.67.89.3";
            IDictionary<string, object> requestEnv = null;
            IQuasiHttpClientTransport transport = new ConfigurableQuasiHttpTransport
            {
                AllocateConnectionCallback = async (paramsC) =>
                {
                    await Task.Delay(15);
                    return new DefaultConnectionAllocationResponse
                    {
                        Connection = connection,
                        Environment = requestEnv
                    };
                }
            };

            var instance = new StandardQuasiHttpClient
            {
                DefaultProtocolFactory = defaultProtocolFactory,
                Transport = transport,
                DefaultSendOptions = defaultSendOptions
            };
            object remoteEndpoint = "senty2";
            IQuasiHttpRequest request = new DefaultQuasiHttpRequest
            {
                Environment = requestEnv
            };
            IQuasiHttpSendOptions sendOptions = new DefaultQuasiHttpSendOptions
            {
                ExtraConnectivityParams = new Dictionary<string, object>(),
                ResponseBufferingEnabled = false,
                ResponseBodyBufferingSizeLimit = 125,
                MaxChunkSize = 50
            };
            Task<IQuasiHttpResponse> sendTask = Task.Delay(10).ContinueWith(
                _ =>
                {
                    var res = instance.Send2(remoteEndpoint, request, sendOptions);
                    Task.Delay(70).ContinueWith(_ => instance.CancelSend(res.Item2));
                    return res.Item1;
                }).Unwrap();
            var response = await  MiscUtils.EnsureCompletedTask(sendTask);
            Assert.Same(expectedResponse, response);
            Assert.Equal(new MutableInt(1), sendCallCounter);
            Assert.Equal(new MutableInt(0), cancelCallCounter);
            Assert.Equal(0, expectedResponse.CloseCallCount);

            Assert.Single(transfers);
            var expectedTransfer = new SendTransferInternal
            {
                IsAborted = true,
                Request = request,
                TimeoutMillis = 90,
                ConnectivityParams = new DefaultConnectivityParams
                {
                    RemoteEndpoint = remoteEndpoint,
                    ExtraParams = new Dictionary<string, object>()
                },
                Connection = connection,
                MaxChunkSize = 50,
                ResponseBufferingEnabled = false,
                ResponseBodyBufferingSizeLimit = 125
            };
            ComparisonUtils.CompareSendTransfers(expectedTransfer, transfers[0]);
            Assert.NotNull(transfers[0].CancellationTcs);
        }

        [Fact]
        public async Task TestDefaultSend3()
        {
            var cancelCallCounter = new MutableInt();
            var sendCallCounter = new MutableInt();
            var transfers = new List<SendTransferInternal>();
            Func<SendTransferInternal, ISendProtocolInternal> defaultProtocolFactory = transfer =>
            {
                transfers.Add(transfer);
                return new TestSendProtocolInternal
                {
                    CancelCallCounter = cancelCallCounter,
                    SendCallCounter = sendCallCounter
                };
            };
            DefaultQuasiHttpSendOptions defaultSendOptions = new DefaultQuasiHttpSendOptions
            {
                TimeoutMillis = -1
            };
            object connection = new List<string> { "and" };
            IDictionary<string, object> requestEnv = new Dictionary<string, object>();
            IQuasiHttpClientTransport transport = new ConfigurableQuasiHttpTransport
            {
                AllocateConnectionCallback = async (paramsC) =>
                {
                    await Task.Delay(80);
                    return new DefaultConnectionAllocationResponse
                    {
                        Connection = connection,
                        Environment = requestEnv
                    };
                }
            };

            var instance = new StandardQuasiHttpClient
            {
                DefaultProtocolFactory = defaultProtocolFactory,
                Transport = transport,
                DefaultSendOptions = defaultSendOptions
            };
            object remoteEndpoint = 6;
            IQuasiHttpRequest request = new DefaultQuasiHttpRequest
            {
                Environment = requestEnv
            };
            IQuasiHttpSendOptions sendOptions = new DefaultQuasiHttpSendOptions
            {
                TimeoutMillis = 20,
                ResponseBufferingEnabled = false,
                ResponseBodyBufferingSizeLimit = 150,
            };
            Task<IQuasiHttpResponse> sendTask = instance.Send(remoteEndpoint, request, sendOptions);
            var sendError = await Assert.ThrowsAsync<QuasiHttpRequestProcessingException>(() =>
            {
                return MiscUtils.EnsureCompletedTask(sendTask);
            });
            Assert.Equal(QuasiHttpRequestProcessingException.ReasonCodeTimeout, sendError.ReasonCode);
            Assert.Equal(new MutableInt(0), sendCallCounter);
            Assert.Equal(new MutableInt(1), cancelCallCounter);

            Assert.Single(transfers);
            var expectedTransfer = new SendTransferInternal
            {
                IsAborted = true,
                Request = request,
                TimeoutMillis = 20,
                ConnectivityParams = new DefaultConnectivityParams
                {
                    RemoteEndpoint = remoteEndpoint,
                    ExtraParams = new Dictionary<string, object>()
                },
                Connection = connection,
                MaxChunkSize = 8_192,
                ResponseBufferingEnabled = false,
                ResponseBodyBufferingSizeLimit = 150
            };
            ComparisonUtils.CompareSendTransfers(expectedTransfer, transfers[0]);
            Assert.NotNull(transfers[0].CancellationTcs);
        }

        [Fact]
        public async Task TestDefaultSend4()
        {
            var cancelCallCounter = new MutableInt();
            var sendCallCounter = new MutableInt();
            var transfers = new List<SendTransferInternal>();
            var lateResponse = new TestQuasiHttpResponse();
            Func<SendTransferInternal, ISendProtocolInternal> defaultProtocolFactory = transfer =>
            {
                transfers.Add(transfer);
                return new TestSendProtocolInternal
                {
                    CancelCallCounter = cancelCallCounter,
                    SendCallCounter = sendCallCounter,
                    ResponseDelay = 50,
                    ResponseToReturn = lateResponse
                };
            };
            DefaultQuasiHttpSendOptions defaultSendOptions = new DefaultQuasiHttpSendOptions
            {
                TimeoutMillis = 10,
            };
            object connection = new List<object> { true, "and" };
            IDictionary<string, object> requestEnv = new Dictionary<string, object>();
            IQuasiHttpClientTransport transport = new ConfigurableQuasiHttpTransport
            {
                AllocateConnectionCallback = async (paramsC) =>
                {
                    await Task.Delay(10);
                    return new DefaultConnectionAllocationResponse
                    {
                        Connection = connection,
                        Environment = requestEnv
                    };
                }
            };

            var instance = new StandardQuasiHttpClient
            {
                DefaultProtocolFactory = defaultProtocolFactory,
                Transport = transport,
                DefaultSendOptions = defaultSendOptions
            };
            object remoteEndpoint = true;
            var reqBodyCallCounter = new MutableInt();
            IQuasiHttpRequest request = new DefaultQuasiHttpRequest
            {
                Environment = requestEnv,
                Body = new ConfigurableQuasiHttpBody
                {
                    EndReadCallback = async () => reqBodyCallCounter.Increment()
                }
            };
            IQuasiHttpSendOptions sendOptions = new DefaultQuasiHttpSendOptions
            {
                TimeoutMillis = -1
            };
            var res = instance.Send2(remoteEndpoint, request, sendOptions);
            _ = Task.Delay(40).ContinueWith(_ => instance.CancelSend(res.Item2));
            Task<IQuasiHttpResponse> sendTask = res.Item1;
            var sendError = await Assert.ThrowsAsync<QuasiHttpRequestProcessingException>(() =>
            {
                return MiscUtils.EnsureCompletedTask(sendTask);
            });
            Assert.Equal(QuasiHttpRequestProcessingException.ReasonCodeCancelled, sendError.ReasonCode);
            Assert.Equal(new MutableInt(1), sendCallCounter);
            Assert.Equal(new MutableInt(1), cancelCallCounter);
            Assert.Equal(new MutableInt(1), reqBodyCallCounter);
            Assert.Equal(1, lateResponse.CloseCallCount);

            Assert.Single(transfers);
            var expectedTransfer = new SendTransferInternal
            {
                IsAborted = true,
                Request = request,
                TimeoutMillis = -1,
                ConnectivityParams = new DefaultConnectivityParams
                {
                    RemoteEndpoint = remoteEndpoint,
                    ExtraParams = new Dictionary<string, object>()
                },
                Connection = connection,
                MaxChunkSize = 8_192,
                ResponseBufferingEnabled = true,
                ResponseBodyBufferingSizeLimit = 134_217_728
            };
            ComparisonUtils.CompareSendTransfers(expectedTransfer, transfers[0]);
            Assert.NotNull(transfers[0].CancellationTcs);
        }

        private class TestSendProtocolInternal : ISendProtocolInternal
        {
            public int ResponseDelay { get; set; }

            public IQuasiHttpResponse ResponseToReturn { get; set; }

            public bool? ResponseBufferingApplied { get; set; }
            public MutableInt CancelCallCounter { get; set; }
            public MutableInt SendCallCounter { get; set; }

            public Task Cancel()
            {
                CancelCallCounter?.Increment();
                return Task.CompletedTask;
            }

            public async Task<ProtocolSendResult> Send()
            {
                SendCallCounter?.Increment();
                await Task.Delay(ResponseDelay);
                return new ProtocolSendResult
                {
                    Response = ResponseToReturn,
                    ResponseBufferingApplied = ResponseBufferingApplied
                };
            }
        }

        private class TestQuasiHttpResponse : IQuasiHttpResponse
        {
            public int CloseCallCount { get; set; }

            public int StatusCode { get; set; }

            public IDictionary<string, IList<string>> Headers { get; set; }

            public IQuasiHttpBody Body { get; set; }

            public string HttpStatusMessage { get; set; }

            public string HttpVersion { get; set; }

            public IDictionary<string, object> Environment { get; set; }

            public Task Close()
            {
                CloseCallCount++;
                return Task.CompletedTask;
            }
        }
    }
}
