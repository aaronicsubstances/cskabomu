using Kabomu.Common;
using Kabomu.Common.Bodies;
using Kabomu.QuasiHttp;
using Kabomu.Tests.Internals;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.IO;
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
            var directProcessingTransport = new ConfigurableQuasiHttpTransport
            {
                DirectSendRequestProcessingEnabled = true,
                ProcessSendRequestCallback = (actualRemoteEndpoint, req, resCb) =>
                {
                    Assert.Equal(remoteEndpoint, actualRemoteEndpoint);
                    Assert.Equal(request, req);
                    eventLoop.ScheduleTimeout(responseTimeMillis, _ =>
                    {
                        resCb.Invoke(null, expectedResponse);
                        // test handling of multiple callback invocations.
                        resCb.Invoke(null, expectedResponse);
                    }, null);
                }
            };
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

        [Fact]
        public void TestResetOfTransfersWithoutConnections()
        {
            // arrange.
            var eventLoop = new TestEventLoopApi
            {
                RunMutexApiThroughPostCallback = true
            };
            var transport = new ConfigurableQuasiHttpTransport();
            var client = new KabomuQuasiHttpClient
            {
                DefaultTimeoutMillis = 20,
                Application = new ConfigurableQuasiHttpApplication(),
                EventLoop = eventLoop,
                Transport = transport
            };
            var expectedErrors = new string[] { "send timeout", "send timeout",
                "send timeout", "shutdown", "shutdown" };
            var actualResponseErrors = new Exception[expectedErrors.Length];
            for (int i = 0; i < expectedErrors.Length; i++)
            {
                eventLoop.ScheduleTimeout(i * 2, s =>
                {
                    var capturedIndex = (int)s;
                    var options = new DefaultQuasiHttpSendOptions
                    {
                        TimeoutMillis = 5
                    };
                    transport.DirectSendRequestProcessingEnabled = capturedIndex == 0;
                    client.Send(null, new DefaultQuasiHttpRequest(), options, (e, res) =>
                    {
                        actualResponseErrors[capturedIndex] = e;
                    });
                }, i);
            }
            var resetCbCalled = false;
            eventLoop.ScheduleTimeout(10, _ =>
            {
                client.Reset(new Exception("shutdown"), e =>
                {
                    Assert.False(resetCbCalled);
                    Assert.Null(e);
                    resetCbCalled = true;
                });
            }, null);

            // act.
            eventLoop.AdvanceTimeTo(1000);

            // assert.
            Assert.True(resetCbCalled);
            for (int i = 0; i < expectedErrors.Length; i++)
            {
                Assert.NotNull(actualResponseErrors[i]);
                Assert.Equal(expectedErrors[i], actualResponseErrors[i].Message);
            }
        }

        [Fact]
        public void TestResetOfTransfersWithConnections()
        {
            // arrange.
            var eventLoop = new TestEventLoopApi
            {
                RunMutexApiThroughPostCallback = true
            };
            var transport = new ConfigurableQuasiHttpTransport
            {
                MaxChunkSize = 100,
                AllocateConnectionCallback = (remoteEndpoint, cb) =>
                {
                    cb.Invoke(null, new object());
                }
            };
            var client = new KabomuQuasiHttpClient
            {
                DefaultTimeoutMillis = 20,
                Application = new ConfigurableQuasiHttpApplication(),
                EventLoop = eventLoop,
                Transport = transport
            };
            var expectedErrors = new string[] { "send timeout", "send timeout",
                "send timeout", "shutdown", "shutdown" };
            var actualResponseErrors = new Exception[expectedErrors.Length];
            for (int i = 0; i < expectedErrors.Length; i++)
            {
                eventLoop.ScheduleTimeout(i * 2, s =>
                {
                    var capturedIndex = (int)s;
                    var options = new DefaultQuasiHttpSendOptions
                    {
                        TimeoutMillis = 5
                    };
                    transport.DirectSendRequestProcessingEnabled = capturedIndex == 0;
                    client.Send(null, new DefaultQuasiHttpRequest(), options, (e, res) =>
                    {
                        actualResponseErrors[capturedIndex] = e;
                    });
                }, i);
            }
            var resetCbCalled = false;
            eventLoop.ScheduleTimeout(10, _ =>
            {
                client.Reset(new Exception("shutdown"), e =>
                {
                    Assert.False(resetCbCalled);
                    Assert.Null(e);
                    resetCbCalled = true;
                });
            }, null);

            // act.
            eventLoop.AdvanceTimeTo(1000);

            // assert.
            Assert.True(resetCbCalled);
            for (int i = 0; i < expectedErrors.Length; i++)
            {
                Assert.NotNull(actualResponseErrors[i]);
                Assert.Equal(expectedErrors[i], actualResponseErrors[i].Message);
            }
        }

        [Theory]
        [MemberData(nameof(CreateTestNormalSendAndReceiveData))]
        public void TestNormalSendAndReceiveSeparately(int scheduledTime, string localEndpoint,
            IQuasiHttpRequest request, IQuasiHttpSendOptions options, string responseError,
            IQuasiHttpResponse response, byte[] responseBodyBytes)
        {
            var testData = new List<object[]>();
            testData.Add(new object[] { scheduledTime, localEndpoint, request, options,
                responseError, response, responseBodyBytes });
            RunTestNormalSendAndReceive(testData);
        }

        [Fact]
        public void TestNormalSendAndReceiveTogether()
        {
            var testData = CreateTestNormalSendAndReceiveData();
            RunTestNormalSendAndReceive(testData);
        }

        private void RunTestNormalSendAndReceive(List<object[]> testDataList)
        {
            // arrange.
            var eventLoop = new TestEventLoopApi
            {
                RunMutexApiThroughPostCallback = true
            };
            var accraEndpoint = "accra";
            var accraClient = new KabomuQuasiHttpClient
            {
                EventLoop = eventLoop,
                DefaultTimeoutMillis = 100
            };
            var kumasiEndpoint = "kumasi";
            var kumasiClient = new KabomuQuasiHttpClient
            {
                EventLoop = eventLoop,
                DefaultTimeoutMillis = 50
            };

            var hub = new MemoryBasedTransportHub();
            hub.Clients.Add(accraEndpoint, accraClient);
            hub.Clients.Add(kumasiEndpoint, kumasiClient);

            var accraTransport = new MemoryBasedTransport
            {
                Hub = hub,
                Mutex = eventLoop,
                MaxChunkSize = 150
            };
            accraClient.Transport = accraTransport;
            accraClient.Application = CreateEndpointApplication(eventLoop, accraEndpoint,
                accraTransport.MaxChunkSize, 7);
            var kumasiTransport = new MemoryBasedTransport
            {
                Hub = hub,
                Mutex = eventLoop,
                MaxChunkSize = 150
            };
            kumasiClient.Transport = kumasiTransport;
            kumasiClient.Application = CreateEndpointApplication(eventLoop, kumasiEndpoint,
                kumasiTransport.MaxChunkSize, 10);

            var maxChunkSizes = new int[testDataList.Count];
            var actualResponseErrors = new Exception[testDataList.Count];
            var actualResponses = new IQuasiHttpResponse[testDataList.Count];

            for (int i = 0; i < testDataList.Count; i++)
            {
                var testData = testDataList[i];
                var scheduledTime = (int)testData[0];
                var localEndpoint = (string)testData[1];
                var request = (IQuasiHttpRequest)testData[2];
                var options = (IQuasiHttpSendOptions)testData[3];

                IQuasiHttpClient client;
                string remoteEndpoint;
                if (localEndpoint == accraEndpoint)
                {
                    client = accraClient;
                    maxChunkSizes[i] = accraTransport.MaxChunkSize;
                    remoteEndpoint = kumasiEndpoint;
                }
                else
                {
                    client = kumasiClient;
                    maxChunkSizes[i] = kumasiTransport.MaxChunkSize;
                    remoteEndpoint = accraEndpoint;
                }
                var testDataIndex = i; // capture it.
                eventLoop.ScheduleTimeout(scheduledTime, _ =>
                {
                    client.Send(remoteEndpoint, request, options, (e, res) =>
                    {
                        actualResponseErrors[testDataIndex] = e;
                        if (e != null)
                        {
                            return;
                        }
                        actualResponses[testDataIndex] = res;
                        if (res.Body != null)
                        {
                            // read response body before assertion to prevent timeout during
                            // event loop advance.
                            TransportUtils.ReadBodyToEnd(eventLoop, res.Body,
                                maxChunkSizes[testDataIndex], (e, d) =>
                            {
                                Assert.Null(e);
                                var equivalentRes = new DefaultQuasiHttpResponse
                                {
                                    StatusIndicatesSuccess = res.StatusIndicatesSuccess,
                                    StatusIndicatesClientError = res.StatusIndicatesClientError,
                                    StatusMessage = res.StatusMessage,
                                    Headers = res.Headers,
                                    HttpStatusCode = res.HttpStatusCode,
                                    HttpVersion = res.HttpVersion
                                };
                                if (res.Body.ContentLength < 0)
                                {
                                    equivalentRes.Body = new StreamBackedBody(new MemoryStream(d), res.Body.ContentType);
                                }
                                else
                                {
                                    equivalentRes.Body = new ByteBufferBody(d, 0, d.Length, res.Body.ContentType);
                                }
                                actualResponses[testDataIndex] = equivalentRes;
                            });
                        }
                    });
                }, null);
            }

            // act.
            eventLoop.AdvanceTimeTo(1000);

            // assert.
            eventLoop.RunMutexApiThroughPostCallback = false;
            for (int i = 0; i < testDataList.Count; i++)
            {
                var testData = testDataList[i];
                var expectedResponseError = (string)testData[4];
                var expectedResponse = (IQuasiHttpResponse)testData[5];
                var expectedResponseBodyBytes = (byte[])testData[6];
                var actualResponseError = actualResponseErrors[i];
                var actualResponse = actualResponses[i];
                var maxChunkSize = maxChunkSizes[i];
                
                if (expectedResponseError != null)
                {
                    Assert.NotNull(actualResponseError);
                    Assert.Equal(expectedResponseError, actualResponseError.Message);
                }
                else
                {
                    Assert.Null(actualResponseError);
                    ComparisonUtils.CompareResponses(eventLoop, maxChunkSize,
                        expectedResponse, actualResponse, expectedResponseBodyBytes);
                }
            }
        }

        public static List<object[]> CreateTestNormalSendAndReceiveData()
        {
            var accraEndpoint = "accra";
            var kumasiEndpoint = "kumasi";
            var testData = new List<object[]>();

            int scheduledTime = 2;
            var localEndpoint = accraEndpoint;
            var request = new DefaultQuasiHttpRequest
            {
                HttpMethod = "PUT",
                HttpVersion = "1.0"
            };
            DefaultQuasiHttpSendOptions options = null;
            string responseError = null;
            var response = new DefaultQuasiHttpResponse
            {
                StatusIndicatesSuccess = false,
                StatusIndicatesClientError = true,
                StatusMessage = "bad request",
                Headers = new Dictionary<string, List<string>>
                {
                    { "origin", new List<string>{ kumasiEndpoint } },
                    { "method", new List<string>{ "PUT" } },
                    { "version", new List<string>{ "1.0" } }
                },
                HttpStatusCode = 400
            };
            byte[] responseBodyBytes = null;
            testData.Add(new object[] { scheduledTime, localEndpoint, request, options, 
                responseError, response, responseBodyBytes });

            scheduledTime = 4;
            localEndpoint = accraEndpoint;
            request = new DefaultQuasiHttpRequest
            {
                Path = "/",
                Headers = new Dictionary<string, List<string>>
                {
                    { "op", new List<string>{ "sub" } },
                    { "first", new List<string>{ "02", "034f" } },
                    { "second", new List<string>{ "2" } }
                }
            };
            options = new DefaultQuasiHttpSendOptions
            {
                TimeoutMillis = 40
            };
            responseError = null;
            response = new DefaultQuasiHttpResponse
            {
                StatusIndicatesSuccess = true,
                StatusMessage = "ok",
                Headers = new Dictionary<string, List<string>>
                {
                    { "origin", new List<string>{ kumasiEndpoint } },
                    { "path", new List<string>{ "/" } },
                    { "ans", new List<string>{ "00", "014d" } }
                },
                HttpStatusCode = 200,
                HttpVersion = "1.1"
            };
            responseBodyBytes = null;
            testData.Add(new object[] { scheduledTime, localEndpoint, request, options,
                responseError, response, responseBodyBytes });

            scheduledTime = 5;
            localEndpoint = kumasiEndpoint;
            request = new DefaultQuasiHttpRequest
            {
                Path = "/compute",
                Headers = new Dictionary<string, List<string>>
                {
                    { "op", new List<string>{ "div" } },
                    { "first", new List<string>{ "02", "034f" } },
                    { "second", new List<string>{ "2" } }
                }
            };
            request.Body = new ByteBufferBody(new byte[] { 0x13, 0x14, 0x15, 0x16 }, 0, 4, null);
            options = null;
            responseError = null;
            response = new DefaultQuasiHttpResponse
            {
                StatusIndicatesSuccess = true,
                StatusMessage = "ok",
                Headers = new Dictionary<string, List<string>>
                {
                    { "origin", new List<string>{ accraEndpoint } },
                    { "path", new List<string>{ "/compute" } },
                    { "ans", new List<string>{ "01", "0127" } }
                },
                HttpStatusCode = 200,
                HttpVersion = "1.1"
            };
            response.Body = new ByteBufferBody(new byte[] { 0x09, 0x0a, 0x0a, 0x0b }, 0, 4, null);
            responseBodyBytes = new byte[] { 0x09, 0x0a, 0x0a, 0x0b };
            testData.Add(new object[] { scheduledTime, localEndpoint, request, options,
                responseError, response, responseBodyBytes });

            scheduledTime = 10;
            localEndpoint = kumasiEndpoint;
            request = new DefaultQuasiHttpRequest
            {
                Path = "/grind",
                Headers = new Dictionary<string, List<string>>
                {
                    { "op", new List<string>{ "sub" } },
                    { "first", new List<string>{ "0a" } },
                    { "second", new List<string>{ "1" } }
                }
            };
            request.Body = new StreamBackedBody(new MemoryStream(new byte[] { 0, 0x26, 0 }, 1, 1), null);
            options = null;
            responseError = null;
            response = new DefaultQuasiHttpResponse
            {
                StatusIndicatesSuccess = true,
                StatusMessage = "ok",
                Headers = new Dictionary<string, List<string>>
                {
                    { "origin", new List<string>{ accraEndpoint } },
                    { "path", new List<string>{ "/grind" } },
                    { "ans", new List<string>{ "09" } }
                },
                HttpStatusCode = 200,
                HttpVersion = "1.1"
            };
            response.Body = new StreamBackedBody(new MemoryStream(new byte[] { 0x25, 0 }, 0, 1), null);
            responseBodyBytes = new byte[] { 0x25 };
            testData.Add(new object[] { scheduledTime, localEndpoint, request, options,
                responseError, response, responseBodyBytes });

            scheduledTime = 11;
            localEndpoint = accraEndpoint;
            request = new DefaultQuasiHttpRequest
            {
                Path = "/ping",
                Headers = new Dictionary<string, List<string>>
                {
                    { "op", new List<string>{ "div" } },
                    { "first", new List<string>{ "" } },
                    { "second", new List<string>{ "14000" } }
                },
                HttpVersion = "1.1",
                HttpMethod = "GET"
            };
            request.Body = new ByteBufferBody(new byte[] { 0, 0x26, 0 }, 1, 0, null);
            options = null;
            responseError = null;
            response = new DefaultQuasiHttpResponse
            {
                StatusIndicatesSuccess = true,
                StatusMessage = "ok",
                Headers = new Dictionary<string, List<string>>
                {
                    { "origin", new List<string>{ kumasiEndpoint } },
                    { "path", new List<string>{ "/ping" } },
                    { "ans", new List<string>{ "" } },
                    { "version", new List<string>{ "1.1" } },
                    { "method", new List<string>{ "GET" } }
                },
                HttpStatusCode = 200,
                HttpVersion = "1.1"
            };
            response.Body = null;
            responseBodyBytes = null;
            testData.Add(new object[] { scheduledTime, localEndpoint, request, options,
                responseError, response, responseBodyBytes });

            scheduledTime = 15;
            localEndpoint = kumasiEndpoint;
            request = new DefaultQuasiHttpRequest
            {
                Path = "/pong",
                Headers = new Dictionary<string, List<string>>
                {
                    { "op", new List<string>{ "sub" } },
                    { "first", new List<string>{ "" } },
                    { "second", new List<string>{ "14000" } }
                }
            };
            request.Body = new StreamBackedBody(new MemoryStream(new byte[] { 0, 0x26, 0 }, 1, 0), null);
            options = null;
            responseError = null;
            response = new DefaultQuasiHttpResponse
            {
                StatusIndicatesSuccess = true,
                StatusMessage = "ok",
                Headers = new Dictionary<string, List<string>>
                {
                    { "origin", new List<string>{ accraEndpoint } },
                    { "path", new List<string>{ "/pong" } },
                    { "ans", new List<string>{ "" } }
                },
                HttpStatusCode = 200,
                HttpVersion = "1.1"
            };
            response.Body = new StreamBackedBody(new MemoryStream(new byte[] { 0x25, 0 }, 1, 0), null);
            responseBodyBytes = new byte[0];
            testData.Add(new object[] { scheduledTime, localEndpoint, request, options,
                responseError, response, responseBodyBytes });

            scheduledTime = 17;
            localEndpoint = accraEndpoint;
            request = new DefaultQuasiHttpRequest
            {
                Path = "/t",
                Headers = new Dictionary<string, List<string>>
                {
                    { "op", new List<string>{ "sub" } },
                    { "first", new List<string>{ "082b" } },
                    { "second", new List<string>{ "14000" } }
                }
            };
            options = new DefaultQuasiHttpSendOptions
            {
                TimeoutMillis = 3
            };
            responseError = "send timeout";
            response = null;
            responseBodyBytes = null;
            testData.Add(new object[] { scheduledTime, localEndpoint, request, options,
                responseError, response, responseBodyBytes });

            return testData;
        }

        private IQuasiHttpApplication CreateEndpointApplication(IEventLoopApi eventLoop, string endpoint,
            int maxChunkSize, int processingDelay)
        {
            var app = new ConfigurableQuasiHttpApplication
            {
                ProcessRequestCallback = (req, resCb) =>
                {
                    Func<byte, byte> selectedOp = null;
                    if (req.Headers != null && req.Headers.ContainsKey("second") &&
                        req.Headers.ContainsKey("op"))
                    {
                        int secondOperand = int.Parse(req.Headers["second"][0]);
                        Func<byte, byte> subOp = b => (byte)(b - secondOperand);
                        Func<byte, byte> divOp = b => (byte)(b / secondOperand);
                        var opCode = req.Headers["op"][0];
                        switch (opCode)
                        {
                            case "div":
                                selectedOp = divOp;
                                break;
                            case "sub":
                                selectedOp = subOp;
                                break;
                            default:
                                break;
                        }
                    }
                    var res = new DefaultQuasiHttpResponse
                    {
                        Headers = new Dictionary<string, List<string>>()
                    };
                    res.Headers.Add("origin", new List<string> { endpoint });
                    if (req.Path != null)
                    {
                        // test that path was received correctly.
                        res.Headers.Add("path", new List<string> { req.Path });
                    }
                    if (req.HttpVersion != null)
                    {
                        // test that version was received correctly.
                        res.Headers.Add("version", new List<string> { req.HttpVersion });
                    }
                    if (req.HttpMethod != null)
                    {
                        // test that method was received correctly.
                        res.Headers.Add("method", new List<string> { req.HttpMethod });
                    }
                    if (selectedOp == null)
                    {
                        res.StatusIndicatesClientError = true;
                        res.StatusMessage = "bad request";
                        res.HttpStatusCode = 400;
                    }
                    else
                    {
                        res.StatusIndicatesSuccess = true;
                        res.StatusMessage = "ok";
                        res.HttpStatusCode = 200;
                        res.HttpVersion = "1.1";
                        if (req.Headers.ContainsKey("first"))
                        {
                            var answers = new List<string>();
                            foreach (var opArgStr in req.Headers["first"])
                            {
                                var opArg = ByteUtils.ConvertHexToBytes(opArgStr);
                                var answer = new byte[opArg.Length];
                                for (int i = 0; i < opArg.Length; i++)
                                {
                                    answer[i] = selectedOp.Invoke(opArg[i]);
                                }
                                answers.Add(ByteUtils.ConvertBytesToHex(answer, 0, answer.Length));
                            }
                            res.Headers.Add("ans", answers);
                        }
                        if (req.Body != null)
                        {
                            TransportUtils.ReadBodyToEnd(eventLoop, req.Body, maxChunkSize, (e, reqBodyBytes) =>
                            {
                                Assert.Null(e);
                                var resBodyBytes = new byte[reqBodyBytes.Length];
                                for (int i = 0; i < reqBodyBytes.Length; i++)
                                {
                                    resBodyBytes[i] = selectedOp.Invoke(reqBodyBytes[i]);
                                }
                                if (req.Body.ContentLength < 0)
                                {
                                    res.Body = new StreamBackedBody(new MemoryStream(resBodyBytes), null);
                                }
                                else
                                {
                                    res.Body = new ByteBufferBody(resBodyBytes, 0, resBodyBytes.Length, null);
                                }
                                eventLoop.ScheduleTimeout(processingDelay, _ =>
                                {
                                    resCb.Invoke(null, res);
                                }, null);
                            });
                            return;
                        }
                    }
                    eventLoop.ScheduleTimeout(processingDelay, _ =>
                    {
                        resCb.Invoke(null, res);
                    }, null);
                }
            };
            return app;
        }
    }
}
