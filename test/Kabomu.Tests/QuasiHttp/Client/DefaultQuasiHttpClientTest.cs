using Kabomu.Concurrency;
using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.Client;
using Kabomu.QuasiHttp.Transport;
using Kabomu.Tests.Internals;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.QuasiHttp.Client
{
    public class DefaultQuasiHttpClientTest
    {
        [Fact]
        public async Task TestSend()
        {
            var testEventLoop = new VirtualTimeBasedEventLoopApi();
            IQuasiHttpClientTransport clientTransport = new TestClientTransport
            {
                EventLoopApi = testEventLoop
            };
            IQuasiHttpSendOptions defaultSendOptions = new DefaultQuasiHttpSendOptions
            {
                TimeoutMillis = 100
            };
            var instance = new DefaultQuasiHttpClient
            {
                Transport = clientTransport,
                MutexApi = testEventLoop,
                TimerApi = testEventLoop,
                DefaultSendOptions = defaultSendOptions
            };
            var remoteEndpoint = new TestConnection
            {
                OutputStream = new MemoryStream(),
                ReadDelayMillis = 10,
                WriteDelayMillis = 20,
                ReleaseDelayMillis = 50
            };
            var expectedResponse = new DefaultQuasiHttpResponse();
            byte[] responseBodyBytes = null;
            remoteEndpoint.InputStream = MiscUtils.CreateResponseInputStream(expectedResponse, responseBodyBytes);
            var request = new DefaultQuasiHttpRequest();
            IQuasiHttpSendOptions sendOptions = new DefaultQuasiHttpSendOptions
            {
                MaxChunkSize = 150
            };

            // act
            var responseTask = MiscUtils.SendWithDelay(instance, testEventLoop, 10, remoteEndpoint, request, sendOptions);
            await testEventLoop.AdvanceTimeTo(1_000);

            // assert
            var actualResponse = await responseTask;
            await ComparisonUtils.CompareResponses(sendOptions.MaxChunkSize, expectedResponse, actualResponse,
                responseBodyBytes);
        }
    }
}
