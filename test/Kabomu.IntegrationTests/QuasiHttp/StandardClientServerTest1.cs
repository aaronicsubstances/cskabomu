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
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.IntegrationTests.QuasiHttp
{
    public class StandardClientServerTest1
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private static readonly string KeyStatusMessageOK = "Ok";
        private static readonly string KeyStatusMessageBadRequest = "Bad Request";
        private static readonly string KeyRequestTarget = "x-target";
        private static readonly string KeyRequestMethod = "x-method";
        private static readonly string KeyStatusCode = "x-status-code";
        private static readonly string KeyStatusPhrase = "x-status-phrase";
        private static readonly string KeyHttpVersion1_0 = "HTTP/1.0";
        private static readonly string KeyHttpVersion1_1 = "HTTP/1.1";

        private static readonly string EndpointLang = "lang";
        private static readonly string EndpointPascal = "pascal";
        private static readonly string EndpointParrot = "parrot";

        private static readonly string KeyTransportDelayMs = "tr-delay-ms";
        private static readonly string KeyMathOp = "math-operator";
        private static readonly string KeyMathArg1 = "math-arg-1";
        private static readonly string KeyMathArg2 = "math-arg-2";
        private static readonly string KeyMathResult = "math-answer";
        private static readonly string KeyMathOpAdd = "+";
        private static readonly string KeyMathOpMul = "*";

        [Fact]
        public async Task TestSuccess()
        {
            var testData = CreateTest1Data();
            var servers = new Dictionary<object, MemoryBasedServerTransport>();
            var serverTasks = new List<Task>();
            CreateServer1(servers, serverTasks);
            CreateServer2(servers, serverTasks);
            CreateServer3(servers, serverTasks);

            var clientTransport = new MemoryBasedClientTransport
            {
                Servers = servers
            };
            IQuasiHttpAltTransport clientTransportBypass = null;
            var client = new StandardQuasiHttpClient
            {
                Transport = clientTransport,
                TransportBypass = clientTransportBypass,
                DefaultSendOptions = new DefaultQuasiHttpSendOptions
                {
                    TimeoutMillis = 5_000
                }
            };
            Log.Info($"{nameof(TestSuccess)} starting...\n\n");
            var clientTasks = new List<Task>();
            for (int i = 0; i < testData.Count; i++)
            {
                var testDataItem = testData[i];
                clientTasks.Add(RunTestDataItem(client, i, testDataItem));
            }
            for (int i = 0; i < clientTasks.Count; i++)
            {
                var task = clientTasks[i];
                try
                {
                    await task;
                }
                catch (Exception e)
                {
                    Log.Error(e, "Error occured in Test1 with client task#{0}", i);
                }
            }
            // record any server errors.
            foreach (var task in serverTasks)
            {
                try
                {
                    await task;
                }
                catch (Exception e)
                {
                    Log.Error(e, $"Error occured in {nameof(TestSuccess)} with a server task");
                }
            }
            await Task.WhenAll(clientTasks.Concat(serverTasks));
            Log.Info($"{nameof(TestSuccess)} completed sucessfully.\n\n");
        }

        private async Task RunTestDataItem(StandardQuasiHttpClient client, int index,
            Test1Data testDataItem)
        {
            Log.Info($"Starting {nameof(TestSuccess)} with data#{{0}}...", index);
            var actualResponse = await client.Send(testDataItem.RemoteEndpoint,
                testDataItem.Request, testDataItem.SendOptions);
            try
            {
                await ComparisonUtils.CompareResponses(testDataItem.ExpectedResponse,
                    actualResponse, testDataItem.ExpectedResponseBodyBytes);
                Log.Info($"Sucessfully tested {nameof(TestSuccess)} with data#{{0}}", index);
            }
            finally
            {
                // needed to prevent server tasks from hanging when
                // response buffering is disabled.
                await actualResponse.CustomDispose();
            }
        }

        private static List<Test1Data> CreateTest1Data()
        {
            var testData = new List<Test1Data>();

            // next
            var remoteEndpoint = EndpointParrot;
            DefaultQuasiHttpSendOptions sendOptions = null;
            var expectedResponseBodyBytes = new byte[] {(byte)'d', (byte)'i',
                (byte)'d', (byte)' ', (byte)'i', (byte)'t' };
            var request = new DefaultQuasiHttpRequest
            {
                Method = "GRANT",
                Target = "/reporter",
                HttpVersion = "20",
                Headers = new Dictionary<string, IList<string>>
                {
                    { "soap", new List<string>{ "key soap" } },
                    { "washing power", new List<string>{ "omo", "madar" } },
                    { "math basics", new List<string>{ "+", "-", "*", "/" } },
                    { KeyStatusCode, new List<string>{ "201" } },
                    { KeyStatusPhrase, new List<string>{ "Accepted" } }
                },
                Body = new StringBody(ByteUtils.BytesToString(expectedResponseBodyBytes))
                {
                    ContentType = "text/plain"
                }
            };
            var expectedResponse = new DefaultQuasiHttpResponse
            {
                StatusCode = 201,
                HttpStatusMessage = "Accepted",
                HttpVersion = request.HttpVersion,
                Headers = new Dictionary<string, IList<string>>(request.Headers),
                Body = new ByteBufferBody(expectedResponseBodyBytes)
                {
                    ContentType = "text/plain"
                }
            };
            expectedResponse.Headers.Add(KeyRequestMethod,
                new List<string> { request.Method });
            expectedResponse.Headers.Add(KeyRequestTarget,
                new List<string> { request.Target });
            testData.Add(new Test1Data
            {
                Request = request,
                ExpectedResponse = expectedResponse,
                ExpectedResponseBodyBytes = expectedResponseBodyBytes,
                RemoteEndpoint = remoteEndpoint,
                SendOptions = sendOptions
            });

            // next
            remoteEndpoint = EndpointLang;
            sendOptions = new DefaultQuasiHttpSendOptions
            {
                MaxChunkSize = 200,
                ResponseBufferingEnabled = false
            };
            request = new DefaultQuasiHttpRequest
            {
                Method = "GET",
                Target = "/returjn/dude",
                HttpVersion = KeyHttpVersion1_0,
                Body = new StringBody("hello")
                {
                    ContentType = "text/csv"
                }
            };
            expectedResponseBodyBytes = ByteUtils.StringToBytes("HELLO");
            expectedResponse = new DefaultQuasiHttpResponse
            {
                StatusCode = 0,
                HttpStatusMessage = null,
                HttpVersion = null,
                Body = new ByteBufferBody(expectedResponseBodyBytes)
                {
                    ContentLength = -1
                }
            };
            testData.Add(new Test1Data
            {
                Request = request,
                ExpectedResponse = expectedResponse,
                ExpectedResponseBodyBytes = expectedResponseBodyBytes,
                RemoteEndpoint = remoteEndpoint,
                SendOptions = sendOptions
            });

            // next...
            remoteEndpoint = EndpointPascal;
            sendOptions = null;
            request = new DefaultQuasiHttpRequest
            {
                Method = "POST",
                Target = "/compute",
                Headers = new Dictionary<string, IList<string>>
                {
                    { KeyMathOp, new List<string>{ KeyMathOpMul } },
                    { KeyMathArg1, new List<string>{ "70" } },
                    { KeyMathArg2, new List<string>{ "2" } }
                }
            };
            expectedResponseBodyBytes = null;
            expectedResponse = new DefaultQuasiHttpResponse
            {
                StatusCode = 200,
                HttpStatusMessage = KeyStatusMessageOK,
                HttpVersion = KeyHttpVersion1_0,
                Headers = new Dictionary<string, IList<string>>
                {
                    { KeyMathResult, new string[]{ "140" } }
                }
            };
            testData.Add(new Test1Data
            {
                Request = request,
                ExpectedResponse = expectedResponse,
                ExpectedResponseBodyBytes = expectedResponseBodyBytes,
                RemoteEndpoint = remoteEndpoint,
                SendOptions = sendOptions
            });

            // next...
            remoteEndpoint = EndpointPascal;
            sendOptions = null;
            request = new DefaultQuasiHttpRequest
            {
                Method = "POST",
                Target = "/compute",
                Headers = new Dictionary<string, IList<string>>
                {
                    { KeyMathOp, new List<string>{ "invalid" } },
                    { KeyMathArg1, new List<string>{ "70" } },
                    { KeyMathArg2, new List<string>{ "2" } },
                    { KeyTransportDelayMs, new List<string>{ "1500" } }
                }
            };
            expectedResponseBodyBytes = null;
            expectedResponse = new DefaultQuasiHttpResponse
            {
                StatusCode = 400,
                HttpStatusMessage = KeyStatusMessageBadRequest,
                HttpVersion = KeyHttpVersion1_0
            };
            testData.Add(new Test1Data
            {
                Request = request,
                ExpectedResponse = expectedResponse,
                ExpectedResponseBodyBytes = expectedResponseBodyBytes,
                RemoteEndpoint = remoteEndpoint,
                SendOptions = sendOptions
            });

            // next...
            remoteEndpoint = EndpointPascal;
            sendOptions = null;
            request = new DefaultQuasiHttpRequest
            {
                Method = "PUT",
                Target = "/compute",
                Headers = new Dictionary<string, IList<string>>
                {
                    { KeyMathOp, new List<string>{ KeyMathOpAdd } },
                    { KeyMathArg1, new List<string>{ "70" } },
                    { KeyMathArg2, new List<string>{ "2" } }
                }
            };
            expectedResponseBodyBytes = null;
            expectedResponse = new DefaultQuasiHttpResponse
            {
                StatusCode = 200,
                HttpStatusMessage = KeyStatusMessageOK,
                HttpVersion = KeyHttpVersion1_0,
                Headers = new Dictionary<string, IList<string>>
                {
                    { KeyMathResult, new string[]{ "72" } }
                }
            };
            testData.Add(new Test1Data
            {
                Request = request,
                ExpectedResponse = expectedResponse,
                ExpectedResponseBodyBytes = expectedResponseBodyBytes,
                RemoteEndpoint = remoteEndpoint,
                SendOptions = sendOptions
            });

            return testData;
        }

        private static void CreateServer1( 
            Dictionary<object, MemoryBasedServerTransport> servers,
            List<Task> serverTasks)
        {
            var server = new StandardQuasiHttpServer
            {
                Application = new ConfigurableQuasiHttpApplication
                {
                    ProcessRequestCallback = EchoApplicationServer
                },
                DefaultProcessingOptions = new DefaultQuasiHttpProcessingOptions
                {
                    TimeoutMillis = 4_000
                }
            };
            var serverTransport = new MemoryBasedServerTransport
            {
                AcceptConnectionFunc = c =>
                { 
                    serverTasks.Add(ProcessAcceptConnection(c, server));
                }
            };
            server.Transport = serverTransport;

            servers.Add(EndpointParrot, serverTransport);
        }

        private static void CreateServer2(
            Dictionary<object, MemoryBasedServerTransport> servers,
            List<Task> serverTasks)
        {
            var server = new StandardQuasiHttpServer
            {
                Application = new ConfigurableQuasiHttpApplication
                {
                    ProcessRequestCallback = CapitalizationApplicationServer
                }
            };
            var serverTransport = new MemoryBasedServerTransport
            {
                AcceptConnectionFunc = c =>
                {
                    serverTasks.Add(ProcessAcceptConnection(c, server));
                }
            };
            server.Transport = serverTransport;

            servers.Add(EndpointLang, serverTransport);
        }

        private static void CreateServer3(
            Dictionary<object, MemoryBasedServerTransport> servers,
            List<Task> serverTasks)
        {
            var server = new StandardQuasiHttpServer
            {
                Application = new ConfigurableQuasiHttpApplication
                {
                    ProcessRequestCallback = ArithmeticApplicationServer
                },
                DefaultProcessingOptions = new DefaultQuasiHttpProcessingOptions
                {
                    MaxChunkSize = 100,
                    TimeoutMillis = 2_000
                }
            };
            var serverTransport = new MemoryBasedServerTransport
            {
                AcceptConnectionFunc = c =>
                {
                    serverTasks.Add(ProcessAcceptConnection(c, server));
                }
            };
            server.Transport = serverTransport;

            servers.Add(EndpointPascal, serverTransport);
        }

        private static Task<IQuasiHttpResponse> EchoApplicationServer(
            IQuasiHttpRequest request)
        {
            int status;
            string statusReason;
            try
            {
                status = int.Parse(request.Headers[KeyStatusCode][0]);
                statusReason = request.Headers[KeyStatusPhrase][0];
            }
            catch (Exception)
            {
                status = 200;
                statusReason = KeyStatusMessageOK;
            }
            var resHeaders = new Dictionary<string, IList<string>>
            {
                { KeyRequestTarget, new string[]{ request.Target } },
                { KeyRequestMethod, new string[]{ request.Method } },
            };
            var response = new DefaultQuasiHttpResponse
            {
                StatusCode = status,
                HttpStatusMessage = statusReason,
                HttpVersion = request.HttpVersion ?? KeyHttpVersion1_1,
                Headers = resHeaders,
                Body = request.Body
            };
            if (request.Headers != null)
            {
                foreach (var item in request.Headers)
                {
                    resHeaders.Add(item.Key, item.Value);
                }
            }
            return Task.FromResult<IQuasiHttpResponse>(response);
        }

        private static async Task<IQuasiHttpResponse> CapitalizationApplicationServer(
            IQuasiHttpRequest request)
        {
            var bodyAsString = ByteUtils.BytesToString(
                await IOUtils.ReadAllBytes(request.Body.AsReader()));
            bodyAsString = bodyAsString.ToUpperInvariant();
            return new DefaultQuasiHttpResponse
            {
                Body = new StringBody(bodyAsString)
                {
                    ContentLength = -1
                }
            };
        }

        private static Task<IQuasiHttpResponse> ArithmeticApplicationServer(
            IQuasiHttpRequest request)
        {
            int status = 200;
            string statusReason = KeyStatusMessageOK;
            var resHeaders = new Dictionary<string, IList<string>>();
            var op = request.Headers[KeyMathOp][0];
            var arg1 = double.Parse(request.Headers[KeyMathArg1][0]);
            var arg2 = double.Parse(request.Headers[KeyMathArg2][0]);
            double result = 0;
            if (op == KeyMathOpAdd)
            {
                result = arg1 + arg2;
            }
            else if (op == KeyMathOpMul)
            {
                result = arg1 * arg2;
            }
            else
            {
                status = 400;
                statusReason = KeyStatusMessageBadRequest;
            }
            if (status == 200)
            {
                resHeaders.Add(KeyMathResult, new List<string> { result.ToString() });
            }
            IQuasiHttpResponse response = new DefaultQuasiHttpResponse
            {
                StatusCode = status,
                HttpStatusMessage = statusReason,
                HttpVersion = KeyHttpVersion1_0,
                Headers = resHeaders
            };
            return Task.FromResult(response);
        }

        private static async Task ProcessAcceptConnection(
            IConnectionAllocationResponse c,
            StandardQuasiHttpServer server)
        {
            await Task.Yield();
            if (c.Environment != null && c.Environment.ContainsKey(KeyTransportDelayMs))
            {
                await Task.Delay((int)c.Environment[KeyTransportDelayMs]);
            }
            await server.AcceptConnection(c);
        }

        class Test1Data
        {
            public string RemoteEndpoint { get; set; }
            public IQuasiHttpSendOptions SendOptions { get; set; }
            public IQuasiHttpRequest Request { get; set; }
            public IQuasiHttpResponse ExpectedResponse { get; set; }
            public byte[] ExpectedResponseBodyBytes { get; set; }
        }
    }
}
