using Kabomu.Common;
using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.Client;
using Kabomu.QuasiHttp.EntityBody;
using Kabomu.QuasiHttp.Server;
using Kabomu.QuasiHttp.Transport;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.QuasiHttp
{
    public class QuasiHttpIntegrationTestOne
    {
        [Theory]
        [MemberData(nameof(CreateTestDirectSendData))]
        public async Task TestDirectSend(object remoteEndpoint, IQuasiHttpRequest request, IQuasiHttpSendOptions options,
            int responseTimeMillis, string expectedResponseError, IQuasiHttpResponse expectedResponse)
        {
            // arrange.
            IQuasiHttpTransportBypass directProcessingTransport = new ConfigurableQuasiHttpTransport
            {
                ProcessSendRequestCallback = (req, connectivityParams) =>
                {
                    Func<Task<IQuasiHttpResponse>> helperFunc = async () =>
                    {
                        Assert.Equal(remoteEndpoint, connectivityParams?.RemoteEndpoint);
                        Assert.Equal(request, req);
                        await Task.Delay(responseTimeMillis);
                        return expectedResponse;
                    };
                    var resTask = helperFunc.Invoke();
                    return Tuple.Create(resTask, (object)null);
                },
                WillCancelSendMakeResponseBodyUnusableCallback = (ch, res) =>
                {
                    return Task.FromResult(false);
                }
            };
            var instance = new DefaultQuasiHttpClient
            {
                DefaultSendOptions = new DefaultQuasiHttpSendOptions
                {
                    TimeoutMillis = 100
                },
                TransportBypass = directProcessingTransport
            };
            IQuasiHttpResponse actualResponse = null;
            string actualResponseError = null;

            // act.
            try
            {
                actualResponse = await instance.Send(remoteEndpoint, request, options);
            }
            catch (Exception e)
            {
                actualResponseError = e.Message;
            }

            // assert.
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
                Body = new StringBody("a")
            };
            DefaultQuasiHttpSendOptions options = null;
            int responseTimeMillis = 0;
            string expectedResponseError = null;
            var expectedResponse = new DefaultQuasiHttpResponse
            {
                StatusIndicatesSuccess = true,
                StatusMessage = "ok",
                Body = new StringBody("A,a")
                {
                    ContentType = "text/csv"
                }
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
            responseTimeMillis = 160;
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
            responseTimeMillis = 127;
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
        public async Task TestResetOfTransfersWithoutConnections()
        {
            // arrange.
            var cancellationHandle = new CancellationTokenSource();
            var transport = new ConfigurableQuasiHttpTransport
            {
                AllocateConnectionCallback = async (connectivityParams) =>
                {
                    await Task.Delay(200_000, cancellationHandle.Token);
                    return null;
                },
                ProcessSendRequestCallback = (req, connectivityParams) =>
                {
                    Func<Task<IQuasiHttpResponse>> helperFunc = async () =>
                    {
                        await Task.Delay(200_000, cancellationHandle.Token);
                        return null;
                    };
                    var resTask = helperFunc.Invoke();
                    return Tuple.Create(resTask, (object)null);
                },
                WillCancelSendMakeResponseBodyUnusableCallback = (ch, res) =>
                {
                    return Task.FromResult(false);
                }
            };
            var client = new DefaultQuasiHttpClient
            {
                DefaultSendOptions = new DefaultQuasiHttpSendOptions
                {
                    TimeoutMillis = 20
                },
                Transport = transport,
                TransportBypass = transport
            };
            var expectedErrors = new string[] { "send timeout", "send timeout",
                "send timeout", "client reset", "client reset" };
            var actualResponseErrors = new Exception[expectedErrors.Length];

            var tasks = new List<Task>();

            // act
            for (int i = 0; i < expectedErrors.Length; i++)
            {
                var capturedIndex = i;
                // 30 ms should be enough to distinguish callback firing times
                // on the common operating systems (15ms max on Windows, 10ms max on Linux).
                var sendTime = Math.Min(i * 30, 95);
                tasks.Add(Task.Run(async () =>
                {
                    await Task.Delay(sendTime);
                    var options = new DefaultQuasiHttpSendOptions
                    {
                        TimeoutMillis = capturedIndex > 2 ? 40 : 15
                    };
                    client.TransportBypassProbabilty = capturedIndex == 0 ? 1 : 0;
                    try
                    {
                        await client.Send(null, new DefaultQuasiHttpRequest(), options);
                        actualResponseErrors[capturedIndex] = null;
                    }
                    catch (Exception e)
                    {
                        actualResponseErrors[capturedIndex] = e;
                    }
                }));
            }

            tasks.Add(Task.Run(async () =>
            {
                await Task.Delay(110);
                await client.Reset(null);
            }));

            // wait for actions to complete.
            await Task.WhenAll(tasks);
            cancellationHandle.Cancel();

            // assert.
            for (int i = 0; i < expectedErrors.Length; i++)
            {
                Assert.NotNull(actualResponseErrors[i]);
                Assert.Equal(i + ". " + expectedErrors[i], i + ". " + actualResponseErrors[i].Message);
            }
        }

        [Fact]
        public async Task TestResetOfTransfersWithConnections()
        {
            // arrange.
            var cancellationHandle = new CancellationTokenSource();
            var transport = new ConfigurableQuasiHttpTransport
            {
                AllocateConnectionCallback = async (connectivityParams) =>
                {
                    return new DefaultConnectionAllocationResponse
                    {
                        Connection = new object()
                    };
                },
                WriteBytesCallback = async (c, d, o, l) =>
                {
                    await Task.Delay(200_000, cancellationHandle.Token);
                }, 
                ReleaseConnectionCallback = c => Task.CompletedTask
            };
            var client = new DefaultQuasiHttpClient
            {
                DefaultSendOptions = new DefaultQuasiHttpSendOptions
                {
                    TimeoutMillis = 20
                },
                Transport = transport
            };
            var expectedErrors = new string[] { "send timeout", "send timeout",
                "send timeout", "client reset", "client reset" };
            var actualResponseErrors = new Exception[expectedErrors.Length];

            var tasks = new List<Task>();

            // act
            for (int i = 0; i < expectedErrors.Length; i++)
            {
                var capturedIndex = i;
                // 30 ms should be enough to distinguish callback firing times
                // on the common operating systems (15ms max on Windows, 10ms max on Linux).
                var sendTime = Math.Min(i * 30, 95);
                tasks.Add(Task.Run(async () =>
                {
                    await Task.Delay(sendTime);
                    var options = new DefaultQuasiHttpSendOptions
                    {
                        TimeoutMillis = capturedIndex > 2 ? 40 : 15
                    };
                    try
                    {
                        await client.Send(null, new DefaultQuasiHttpRequest(), options);
                        actualResponseErrors[capturedIndex] = null;
                    }
                    catch (Exception e)
                    {
                        actualResponseErrors[capturedIndex] = e;
                    }
                }));
            }

            tasks.Add(Task.Run(async () =>
            {
                await Task.Delay(110);
                await client.Reset(null);
            }));

            // wait for actions to complete.
            await Task.WhenAll(tasks);
            cancellationHandle.Cancel();

            // assert.
            for (int i = 0; i < expectedErrors.Length; i++)
            {
                Assert.NotNull(actualResponseErrors[i]);
                Assert.Equal(i + ". " + expectedErrors[i], i + ". " + actualResponseErrors[i].Message);
            }
        }

        [Theory]
        [MemberData(nameof(CreateTestNormalSendAndReceiveData))]
        public async Task TestNormalSendAndReceiveSeparately(int scheduledTime, string localEndpoint,
            IQuasiHttpRequest request, IQuasiHttpSendOptions options, string responseError,
            IQuasiHttpResponse response, byte[] responseBodyBytes)
        {
            var testData = new List<object[]>();
            testData.Add(new object[] { scheduledTime, localEndpoint, request, options,
                responseError, response, responseBodyBytes });
            await RunTestNormalSendAndReceive(testData);
        }

        [Fact]
        public async Task TestNormalSendAndReceiveTogether()
        {
            var testData = CreateTestNormalSendAndReceiveData();
            await RunTestNormalSendAndReceive(testData);
        }

        private async Task RunTestNormalSendAndReceive(List<object[]> testDataList)
        {
            // arrange.
            var hub = new DefaultMemoryBasedTransportHub();

            var accraEndpoint = "accra";
            var accraServerMaxChunkSize = 150;
            var accraQuasiHttpServer = new DefaultQuasiHttpServer
            {
                DefaultProcessingOptions = new DefaultQuasiHttpProcessingOptions
                {
                    TimeoutMillis = 2_100,
                    MaxChunkSize = accraServerMaxChunkSize,
                }
            };
            var accraServerTransport = new MemoryBasedServerTransport();
            accraQuasiHttpServer.Transport = accraServerTransport;
            accraQuasiHttpServer.Application = CreateEndpointApplication(accraEndpoint,
                accraServerMaxChunkSize, 27);
            await hub.AddServer(accraEndpoint, accraQuasiHttpServer);
            await accraQuasiHttpServer.Start();

            var accraClientTransport = new MemoryBasedClientTransport
            {
                LocalEndpoint = accraEndpoint,
                Hub = hub
            };
            var accraQuasiHttpClient = new DefaultQuasiHttpClient
            {
                DefaultSendOptions = new DefaultQuasiHttpSendOptions
                {
                    TimeoutMillis = 2_100,
                    MaxChunkSize = 150
                },
                Transport = accraClientTransport
            };

            var kumasiEndpoint = "kumasi";
            var kumasiServerMaxChunkSize = 150;
            var kumasiQuasiHttpServer = new DefaultQuasiHttpServer
            {
                DefaultProcessingOptions = new DefaultQuasiHttpProcessingOptions
                {
                    TimeoutMillis = 1_050,
                    MaxChunkSize = kumasiServerMaxChunkSize
                }
            };
            var kumasiServerTransport = new MemoryBasedServerTransport();
            kumasiQuasiHttpServer.Transport = kumasiServerTransport;
            kumasiQuasiHttpServer.Application = CreateEndpointApplication(kumasiEndpoint,
                kumasiServerMaxChunkSize, 25);
            await hub.AddServer(kumasiEndpoint, kumasiQuasiHttpServer);
            await kumasiQuasiHttpServer.Start();

            var kumasiClientTransport = new MemoryBasedClientTransport
            {
                LocalEndpoint = kumasiEndpoint,
                Hub = hub
            };
            var kumasiQuasiHttpClient = new DefaultQuasiHttpClient
            {
                DefaultSendOptions = new DefaultQuasiHttpSendOptions
                {
                    TimeoutMillis = 1_050,
                    MaxChunkSize = 150
                },
                Transport = kumasiClientTransport
            };

            var tasks = new List<Task>();

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
                    client = accraQuasiHttpClient;
                    maxChunkSizes[i] = accraQuasiHttpClient.DefaultSendOptions.MaxChunkSize;
                    remoteEndpoint = kumasiEndpoint;
                }
                else
                {
                    client = kumasiQuasiHttpClient;
                    maxChunkSizes[i] = kumasiQuasiHttpClient.DefaultSendOptions.MaxChunkSize;
                    remoteEndpoint = accraEndpoint;
                }
                var testDataIndex = i; // capture it.
                tasks.Add(Task.Run(async () =>
                {
                    await Task.Delay(scheduledTime);
                    IQuasiHttpResponse res;
                    try
                    {
                        res = await client.Send(remoteEndpoint, request, options);
                        actualResponses[testDataIndex] = res;
                        if (res.Body == null)
                        {
                            return;
                        }
                    }
                    catch (Exception e)
                    {
                        actualResponseErrors[testDataIndex] = new Exception($"{testDataIndex}. {e.Message}", e);
                        return;
                    }
                    // read response body before assertion to prevent short timeouts from closing bodies.
                    var d = await TransportUtils.ReadBodyToEnd(res.Body, maxChunkSizes[testDataIndex]);
                    var equivalentRes = new DefaultQuasiHttpResponse
                    {
                        StatusIndicatesSuccess = res.StatusIndicatesSuccess,
                        StatusIndicatesClientError = res.StatusIndicatesClientError,
                        StatusMessage = res.StatusMessage,
                        Headers = res.Headers,
                        HttpStatusCode = res.HttpStatusCode,
                        HttpVersion = res.HttpVersion
                    };
                    equivalentRes.Body = new StreamBackedBody(new MemoryStream(d),
                        res.Body.ContentLength)
                    {
                        ContentType = res.Body.ContentType
                    };
                    actualResponses[testDataIndex] = equivalentRes;
                }));
            }

            // wait for actions to complete.
            await Task.WhenAll(tasks);

            // assert.
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
                    Assert.Equal(i + ". " + expectedResponseError, actualResponseError.Message);
                }
                else
                {
                    Assert.Null(actualResponseError);
                    await ComparisonUtils.CompareResponses(maxChunkSize,
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
                TimeoutMillis = 1_100
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
            request.Body = new ByteBufferBody(new byte[] { 0x13, 0x14, 0x15, 0x16 }, 0, 4);
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
            response.Body = new ByteBufferBody(new byte[] { 0x09, 0x0a, 0x0a, 0x0b }, 0, 4);
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
            request.Body = new StreamBackedBody(new MemoryStream(new byte[] { 0, 0x26, 0 }, 1, 1), -1);
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
            response.Body = new StreamBackedBody(new MemoryStream(new byte[] { 0x25, 0 }, 0, 1), -1);
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
            request.Body = new ByteBufferBody(new byte[] { 0, 0x26, 0 }, 1, 0);
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
            request.Body = new StreamBackedBody(new MemoryStream(new byte[] { 0, 0x26, 0 }, 1, 0), -1);
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
            response.Body = new StreamBackedBody(new MemoryStream(new byte[] { 0x25, 0 }, 1, 0), -1);
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

        private IQuasiHttpApplication CreateEndpointApplication(string endpoint,
            int maxChunkSize, int processingDelay)
        {
            var app = new ConfigurableQuasiHttpApplication
            {
                ProcessRequestCallback = async (req, options) =>
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
                            var requestBodyBytes = await TransportUtils.ReadBodyToEnd(req.Body, maxChunkSize);
                            var resBodyBytes = new byte[requestBodyBytes.Length];
                            for (int i = 0; i < requestBodyBytes.Length; i++)
                            {
                                resBodyBytes[i] = selectedOp.Invoke(requestBodyBytes[i]);
                            }
                            res.Body = new StreamBackedBody(new MemoryStream(resBodyBytes),
                                req.Body.ContentLength < 0 ? -1 : resBodyBytes.Length);
                        }
                    }
                    await Task.Delay(processingDelay);
                    return res;
                }
            };
            return app;
        }
    }
}
