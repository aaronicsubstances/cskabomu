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
            var directProcessingTransport = new TestDirectProcessingTransport(remoteEndpoint, (req, resCb) =>
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

        /*[Fact]
        public void TestIndirectSend()
        {
            // arrange.
            var eventLoop = new TestEventLoopApi
            {
                RunMutexApiThroughPostCallback = true
            };
            OutputEventLogger logger = new OutputEventLogger
            {
                Logs = new List<string>()
            };
            UncaughtErrorCallback errorHandler = (e, m) =>
            {
                logger.Logs.Add($"error({m},{e?.Message})");
            };
            var hub = new FakeTcpTransportHub();

            var londonEndpoint = "london";
            var numbersFromLondon = new Dictionary<string, List<string>>
            {
                { "1", new List<string>{ "one" } },
                { "2", new List<string>{ "two" } },
                { "3", new List<string>{ "three" } },
                { "4", new List<string>{ "four" } },
                { "5", new List<string>{ "five" } }
            };
            var londonApp = CreateApplication(eventLoop, londonEndpoint, 20, numbersFromLondon);
            var londonTransport = new FakeTcpTransport
            {
                Hub = hub,
                MaxChunkSize = 5,
            };
            var londonInstance = new KabomuQuasiHttpClient
            {
                DefaultTimeoutMillis = 100,
                ErrorHandler = errorHandler,
                EventLoop = eventLoop,
                Transport = londonTransport,
                Application = londonApp
            };
            londonTransport.Upstream = londonInstance;
            hub.Connections.Add(londonEndpoint, londonTransport);

            var kumasiEndpoint = "kumasi";
            var numbersFromKumasi = new Dictionary<string, List<string>>
            {
                { "1", new List<string>{ "baako" } },
                { "2", new List<string>{ "mmienu" } },
                { "3", new List<string>{ "mmi\u0025Bsa", "mmi3nsa" } },
                { "4", new List<string>{ "nnan" } },
                { "5", new List<string>{ "nnum" } }
            };
            var kumasiApp = CreateApplication(eventLoop, kumasiEndpoint, 30, numbersFromKumasi);
            var kumasiTransport = new FakeTcpTransport
            {
                Hub = hub,
                MaxChunkSize = 5,
            };
            var kumasiInstance = new KabomuQuasiHttpClient
            {
                DefaultTimeoutMillis = 100,
                ErrorHandler = errorHandler,
                EventLoop = eventLoop,
                Transport = kumasiTransport,
                Application = kumasiApp
            };
            kumasiTransport.Upstream = kumasiInstance;
            hub.Connections.Add(kumasiEndpoint, kumasiTransport);

            var requestBodyStr = "caveat";
            var request = new DefaultQuasiHttpRequest
            {
                Path = "/",
                Headers = new Dictionary<string, List<string>>
                {
                    {  "y", new List<string>{ "yes" } }
                },
            };
            // to prevent end of read error, serialize before assigning body.
            string expectedResStr = null;
            SerializeRequest(new TestEventLoopApi(), request, (e, s) =>
            {
                Assert.Null(e);
                expectedResStr = s;
            });
            if (requestBodyStr != null)
            {
                request.Body = new StringBody(requestBodyStr, null);
                expectedResStr += requestBodyStr;
            }
            IQuasiHttpSendOptions options = null;
            string expectedResponseError = null;
            var expectedResponse = new DefaultQuasiHttpResponse
            {
                StatusIndicatesSuccess = true,
            };
            expectedResponse.Body = new StringBody(expectedResStr, null);

            KabomuQuasiHttpClient instance;
            string remoteEndpoint;
            bool sendFromKumasi = true;
            if (sendFromKumasi)
            {
                remoteEndpoint = londonEndpoint;
                instance = kumasiInstance;
                expectedResponse.StatusMessage = londonEndpoint;
                expectedResponse.Headers = numbersFromLondon;
            }
            else
            {
                remoteEndpoint = kumasiEndpoint;
                instance = londonInstance;
                expectedResponse.StatusMessage = kumasiEndpoint;
                expectedResponse.Headers = numbersFromKumasi;
            }

            // act.
            string actualResponseError = null;
            IQuasiHttpResponse actualResponse = null;
            var cbCalled = false;
            instance.Send(remoteEndpoint, request, options, (e, res) =>
            {
                Assert.False(cbCalled);
                actualResponseError = e?.Message;
                actualResponse = res;
                cbCalled = true;
            });
            eventLoop.AdvanceTimeBy(1000);

            // assert
            Assert.True(cbCalled);
            if (expectedResponseError != null)
            {
                Assert.NotNull(actualResponseError);
                Assert.Equal(expectedResponseError, actualResponseError);
                Assert.Single(logger.Logs);
            }
            else
            {
                Assert.Null(actualResponseError);
                ComparisonUtils.CompareResponses(eventLoop, 1000, expectedResponse, actualResponse,
                    expectedResStr);
                Assert.Empty(logger.Logs);
            }
        }

        private static IQuasiHttpApplication CreateApplication(IEventLoopApi eventLoop,
            string localEndpoint, int responseTime,
            Dictionary<string, List<string>> responseHeaders)
        {
            var app = new TestQuasiHttpApplication((req, resCb) =>
            {
                SerializeRequest(eventLoop, req, (e, serializedReq) =>
                {
                    Assert.Null(e);
                    var res = new DefaultQuasiHttpResponse
                    {
                        StatusIndicatesSuccess = true,
                        StatusMessage = localEndpoint,
                        Headers = responseHeaders,
                        Body = new StringBody(serializedReq, null)
                    };
                    eventLoop.ScheduleTimeout(responseTime, _ =>
                    {
                        resCb.Invoke(null, res);
                    }, null);
                    // test handling of multiple callback invocations
                    eventLoop.ScheduleTimeout(responseTime + 2, _ =>
                    {
                        resCb.Invoke(null, res);
                    }, null);
                });
            });
            return app;
        }

        private static void SerializeRequest(IMutexApi mutex, IQuasiHttpRequest req, Action<Exception, string> cb)
        {
            var s = new StringBuilder();
            s.Append(req.Path);
            if (req.Headers != null)
            {
                var keys = new List<string>(req.Headers.Keys);
                keys.Sort();
                foreach (var key in keys)
                {
                    s.Append(key);
                    foreach (var value in req.Headers[key])
                    {
                        s.Append(value);
                    }
                }
            }
            if (req.Body != null)
            {
                TransportUtils.ReadBodyToEnd(req.Body, mutex, 10, (e, data) =>
                {
                    if (e != null)
                    {
                        cb.Invoke(e, null);
                        return;
                    }
                    var dataStr = Encoding.UTF8.GetString(data, 0, data.Length);
                    s.Append(dataStr);
                    cb.Invoke(null, s.ToString());
                });
            }
            else
            {
                cb.Invoke(null, s.ToString());
            }
        }

        private static string SerializeResponse(IQuasiHttpResponse res)
        {
            var s = new StringBuilder();
            s.Append(res.StatusIndicatesSuccess);
            s.Append(res.StatusIndicatesClientError);
            s.Append(res.StatusMessage);
            if (res.Headers != null)
            {
                var keys = new List<string>(res.Headers.Keys);
                keys.Sort();
                foreach (var key in keys)
                {
                    s.Append(key);
                    foreach (var value in res.Headers[key])
                    {
                        s.Append(value);
                    }
                }
            }
            return s.ToString();
        }*/
    }
}
