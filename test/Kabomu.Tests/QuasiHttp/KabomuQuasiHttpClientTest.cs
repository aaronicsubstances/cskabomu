using Kabomu.Common;
using Kabomu.QuasiHttp;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Kabomu.Tests.QuasiHttp
{
    public class KabomuQuasiHttpClientTest
    {
        [Theory]
        [MemberData(nameof(CreateTestDirectSendData))]
        public void TestDirectSend(object remoteEndpoint, IQuasiHttpRequest request, IQuasiHttpSendOptions options,
        int responseTimeMillis, string expectedResponseError,
            IQuasiHttpResponse expectedResponse)
        {
            // arrange.
            var eventLoop = new TestEventLoopApi
            {
                RunMutexApiThroughPostCallback = true
            };
            UncaughtErrorCallback errorHandler = (e, m) =>
            {
                Assert.Equal(expectedResponseError, e?.Message);
            };
            var directProcessingTransport = new DirectProcessingTransport(remoteEndpoint, (req, resCb) =>
            {
                Assert.Equal(request, req);
                eventLoop.ScheduleTimeout(responseTimeMillis, _ =>
                {
                    resCb.Invoke(null, expectedResponse);
                    // test handling of multiple callback invocations.
                    resCb.Invoke(null, expectedResponse);
                }, null);
            });
            var instance = new KabomuQuasiHttpClient
            {
                DefaultTimeoutMillis = 100,
                ErrorHandler = errorHandler,
                EventLoop = eventLoop,
                Transport = directProcessingTransport
            };
            IQuasiHttpResponse actualResponse = null;
            string actualResponseError = null;

            // act.
            var cbCalled = false;
            eventLoop.PostCallback(_ =>
            {
                instance.Send(remoteEndpoint, request, options, (e, res) =>
                {
                    Assert.False(cbCalled);
                    actualResponseError = e?.Message;
                    actualResponse = res;
                    cbCalled = true;
                });
            }, null);
            eventLoop.AdvanceTimeTo(1000);

            // assert.
            Assert.True(cbCalled);
            if (expectedResponseError != null)
            {
                Assert.NotNull(actualResponseError);
                Assert.Equal(expectedResponseError, actualResponseError);
            }
            else
            {
                Assert.Null(actualResponseError);
                Assert.Equal(expectedResponse, actualResponse);
            }
        }

        public static List<object[]> CreateTestDirectSendData()
        {
            var testData = new List<object[]>();

            object remoteEndpoint = "s";
            var request = new DefaultQuasiHttpRequest
            {
                Path = "/",
                Headers = new Dictionary<string, List<string>>
                {
                    { "tr", new List<string>{ "e", "d" } }
                },
                Body = new StringBody("a", null)
            };
            DefaultQuasiHttpSendOptions options = null;
            int responseTimeMillis = 0;
            string expectedResponseError = null;
            var expectedResponse = new DefaultQuasiHttpResponse
            {
                StatusIndicatesSuccess = true,
                StatusMessage = "ok",
                Body = new StringBody("A,a", "text/csv")
            };
            testData.Add(new object[] { remoteEndpoint, request, options,
                responseTimeMillis, expectedResponseError, expectedResponse });

            remoteEndpoint = 3;
            request = new DefaultQuasiHttpRequest
            {
                Path = "/co",
            };
            options = new DefaultQuasiHttpSendOptions
            {
                TimeoutMillis = 200
            };
            responseTimeMillis = 190;
            expectedResponseError = null;
            expectedResponse = new DefaultQuasiHttpResponse
            {
                StatusIndicatesSuccess = true,
                StatusMessage = "ok",
                Headers = new Dictionary<string, List<string>>
                {
                    { "year", new List<string>{ "2022" } }
                }
            };
            testData.Add(new object[] { remoteEndpoint, request, options,
                responseTimeMillis, expectedResponseError, expectedResponse });

            remoteEndpoint = 3;
            request = new DefaultQuasiHttpRequest
            {
                Path = "/long",
            };
            options = null;
            responseTimeMillis = 105;
            expectedResponseError = "send timeout";
            expectedResponse = null;
            testData.Add(new object[] { remoteEndpoint, request, options,
                responseTimeMillis, expectedResponseError, expectedResponse });

            remoteEndpoint = null;
            request = new DefaultQuasiHttpRequest
            {
                Path = "/ping",
            };
            options = null;
            responseTimeMillis = 15;
            expectedResponseError = "no response";
            expectedResponse = null;
            testData.Add(new object[] { remoteEndpoint, request, options,
                responseTimeMillis, expectedResponseError, expectedResponse });

            return testData;
        }
    }
}
