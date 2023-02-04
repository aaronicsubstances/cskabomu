using Kabomu.Concurrency;
using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.Client;
using Kabomu.QuasiHttp.EntityBody;
using Kabomu.QuasiHttp.Transport;
using Kabomu.Tests.Concurrency;
using Kabomu.Tests.Internals;
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

        private static VirtualTimeBasedEventLoopApi CreateTestEventLoopApi()
        {
            return new VirtualTimeBasedEventLoopApi
            {
                DefaultCallbackAftermathDelayance = () => Task.Delay(10)
            };
        }

        [Fact]
        public async Task Test1()
        {
            var testEventLoopApi = CreateTestEventLoopApi();

            var expectedResponse = new DefaultQuasiHttpResponse();
            var cancelCallCounter = new int[1];
            var sendCallCounter = new int[1];
            Func<SendTransferInternal, ISendProtocolInternal> altProtocolFactory = transfer =>
            {
                return new TestSendProtocolInternal
                {
                    TimerApi = testEventLoopApi,
                    ResponseDelay = 50,
                    ResponseToReturn = expectedResponse,
                    ResponseBufferingApplied = null,
                    CancelCallCounter = cancelCallCounter,
                    SendCallCounter = sendCallCounter,
                };
            };
            DefaultQuasiHttpSendOptions defaultSendOptions = null;
            IQuasiHttpAltTransport transportBypass = new TestTransportBypass();
            double transportBypassWrappingProbability = 0;

            var instance = new StandardQuasiHttpClient
            {
                //TimerApi = testEventLoopApi,
                AltProtocolFactory = altProtocolFactory,
                TransportBypass = transportBypass,
                TransportBypassWrappingProbability = transportBypassWrappingProbability,
                DefaultSendOptions = defaultSendOptions
            };
            object remoteEndpoint = null;
            IQuasiHttpRequest request = new DefaultQuasiHttpRequest();
            IQuasiHttpSendOptions sendOptions = null;
            Task<IQuasiHttpResponse> sendTask = instance.Send(remoteEndpoint, request, sendOptions);

            await testEventLoopApi.AdvanceTimeTo(100);
            IQuasiHttpResponse res = await MiscUtils.EnsureCompletedTask(sendTask);
            Assert.Same(expectedResponse, res);
            Assert.Equal(1, sendCallCounter[0]);
            Assert.Equal(1, cancelCallCounter[0]);
        }

        [Fact]
        public async Task Test2()
        {
            var testEventLoopApi = CreateTestEventLoopApi();

            var cancelCallCounter = new int[1];
            var sendCallCounter = new int[1];
            var expectedResponse = new DefaultQuasiHttpResponse();
            Func<SendTransferInternal, ISendProtocolInternal> altProtocolFactory = transfer =>
            {
                return new TestSendProtocolInternal
                {
                    CancelCallCounter = cancelCallCounter,
                    SendCallCounter = sendCallCounter,
                    TimerApi = testEventLoopApi,
                    ResponseDelay = 79,
                    ResponseToReturn = expectedResponse,
                    ResponseBufferingApplied = null
                };
            };
            var defaultSendOptions = new DefaultQuasiHttpSendOptions
            {
                TimeoutMillis = 80
            };
            IQuasiHttpAltTransport transportBypass = new TestTransportBypass();
            double transportBypassWrappingProbability = 0;

            var instance = new StandardQuasiHttpClient
            {
                TimerApi = testEventLoopApi,
                AltProtocolFactory = altProtocolFactory,
                TransportBypass = transportBypass,
                TransportBypassWrappingProbability = transportBypassWrappingProbability,
                DefaultSendOptions = defaultSendOptions
            };
            object remoteEndpoint = null;
            IQuasiHttpRequest request = new DefaultQuasiHttpRequest();
            IQuasiHttpSendOptions sendOptions = null;
            Task<IQuasiHttpResponse> sendTask = instance.Send(remoteEndpoint, request, sendOptions);

            await testEventLoopApi.AdvanceTimeTo(100);
            IQuasiHttpResponse res = await MiscUtils.EnsureCompletedTask(sendTask);
            Assert.Same(expectedResponse, res);
            Assert.Equal(1, sendCallCounter[0]);
            Assert.Equal(1, cancelCallCounter[0]);
        }

        [Fact]
        public async Task Test3()
        {
            var testEventLoopApi = CreateTestEventLoopApi();

            var cancelCallCounter = new int[1];
            var sendCallCounter = new int[1];
            var expectedResponse = new DefaultQuasiHttpResponse();
            Func<SendTransferInternal, ISendProtocolInternal> altProtocolFactory = transfer =>
            {
                return new TestSendProtocolInternal
                {
                    CancelCallCounter = cancelCallCounter,
                    SendCallCounter = sendCallCounter,
                    TimerApi = testEventLoopApi,
                    ResponseDelay = 80,
                    ResponseToReturn = expectedResponse,
                    ResponseBufferingApplied = null
                };
            };
            DefaultQuasiHttpSendOptions defaultSendOptions = new DefaultQuasiHttpSendOptions
            {
                TimeoutMillis = 90
            };
            IQuasiHttpAltTransport transportBypass = new TestTransportBypass();
            double transportBypassWrappingProbability = 0;

            var instance = new StandardQuasiHttpClient
            {
                TimerApi = testEventLoopApi,
                AltProtocolFactory = altProtocolFactory,
                TransportBypass = transportBypass,
                TransportBypassWrappingProbability = transportBypassWrappingProbability,
                DefaultSendOptions = defaultSendOptions
            };
            object remoteEndpoint = null;
            var bodyEndCallCount = new int[1];
            IQuasiHttpRequest request = new DefaultQuasiHttpRequest
            {
                Body = new TestQuasiHttpBody
                {
                    BodyEndCallCount = bodyEndCallCount
                }
            };
            IQuasiHttpSendOptions sendOptions = new DefaultQuasiHttpSendOptions
            {
                TimeoutMillis = 75
            };
            Task<IQuasiHttpResponse> sendTask = instance.Send(remoteEndpoint, request, sendOptions);

            await testEventLoopApi.AdvanceTimeTo(100);
            var sendError = await Assert.ThrowsAsync<QuasiHttpRequestProcessingException>(() =>
            {
                return MiscUtils.EnsureCompletedTask(sendTask);
            });
            Assert.Equal(QuasiHttpRequestProcessingException.ReasonCodeTimeout, sendError.ReasonCode);
            Assert.Equal(1, sendCallCounter[0]);
            Assert.Equal(1, cancelCallCounter[0]);
            Assert.Equal(1, bodyEndCallCount[0]);
        }

        [Fact]
        public async Task Test4()
        {
            var testEventLoopApi = CreateTestEventLoopApi();

            var cancelCallCounter = new int[1];
            var sendCallCounter = new int[1];
            var expectedResponse = new DefaultQuasiHttpResponse();
            Func<SendTransferInternal, ISendProtocolInternal> altProtocolFactory = transfer =>
            {
                return new TestSendProtocolInternal
                {
                    CancelCallCounter = cancelCallCounter,
                    SendCallCounter = sendCallCounter,
                    TimerApi = testEventLoopApi,
                    ResponseDelay = 50,
                    ResponseToReturn = expectedResponse,
                    ResponseBufferingApplied = null
                };
            };
            DefaultQuasiHttpSendOptions defaultSendOptions = new DefaultQuasiHttpSendOptions
            {
                TimeoutMillis = 90
            };
            IQuasiHttpAltTransport transportBypass = new TestTransportBypass();
            double transportBypassWrappingProbability = 0;

            var instance = new StandardQuasiHttpClient
            {
                TimerApi = testEventLoopApi,
                AltProtocolFactory = altProtocolFactory,
                TransportBypass = transportBypass,
                TransportBypassWrappingProbability = transportBypassWrappingProbability,
                DefaultSendOptions = defaultSendOptions
            };
            object remoteEndpoint = null;
            var bodyEndCallCount = new int[1];
            IQuasiHttpRequest request = new DefaultQuasiHttpRequest
            {
                Body = new TestQuasiHttpBody
                {
                    BodyEndCallCount = bodyEndCallCount
                }
            };
            IQuasiHttpSendOptions sendOptions = new DefaultQuasiHttpSendOptions
            {
                MaxChunkSize = 75
            };
            Task<IQuasiHttpResponse> sendTask = MiscUtils.Delay(testEventLoopApi, 10,
                () => {
                    var res = instance.Send2(remoteEndpoint, request, sendOptions);
                    _ = MiscUtils.Delay(testEventLoopApi, 30, () =>
                    {
                        instance.CancelSend(res.Item2);
                        return Task.CompletedTask;
                    });
                    return res.Item1;
                });
            await testEventLoopApi.AdvanceTimeTo(100);
            var sendError = await Assert.ThrowsAsync<QuasiHttpRequestProcessingException>(() =>
            {
                return MiscUtils.EnsureCompletedTask(sendTask);
            });
            Assert.Equal(QuasiHttpRequestProcessingException.ReasonCodeCancelled, sendError.ReasonCode);
            Assert.Equal(1, sendCallCounter[0]);
            Assert.Equal(1, cancelCallCounter[0]);
            Assert.Equal(1, bodyEndCallCount[0]);
        }

        private class TestSendProtocolInternal : ISendProtocolInternal
        {
            public ITimerApi TimerApi { get; set; }

            public int ResponseDelay { get; set; }

            public IQuasiHttpResponse ResponseToReturn { get; set; }

            public bool? ResponseBufferingApplied { get; set; }
            public int[] CancelCallCounter { get; set; }
            public int[] SendCallCounter { get; set; }

            public Task Cancel()
            {
                if (CancelCallCounter != null)
                {
                    CancelCallCounter[0]++;
                }
                return Task.CompletedTask;
            }

            public async Task<ProtocolSendResult> Send()
            {
                if (SendCallCounter != null)
                {
                    SendCallCounter[0]++;
                }
                await TimerApi.Delay(ResponseDelay);
                return new ProtocolSendResult
                {
                    Response = ResponseToReturn,
                    ResponseBufferingApplied = ResponseBufferingApplied
                };
            }
        }

        private class TestTransportBypass : IQuasiHttpAltTransport
        {
            public (Task<IQuasiHttpResponse>, object) ProcessSendRequest(IQuasiHttpRequest request,
                IConnectivityParams connectivityParams)
            {
                throw new NotImplementedException();
            }

            public void CancelSendRequest(object sendCancellationHandle)
            {
                throw new NotImplementedException();
            }
        }

        private class TestQuasiHttpBody : IQuasiHttpBody
        {
            public int[] BodyEndCallCount { get; set; }

            public long ContentLength => -1;

            public string ContentType => null;

            public Task<int> ReadBytes(byte[] data, int offset, int bytesToRead)
            {
                throw new NotImplementedException();
            }

            public Task EndRead()
            {
                if (BodyEndCallCount != null)
                {
                    BodyEndCallCount[0]++;
                }
                return Task.CompletedTask;
            }
        }
    }
}
