using Kabomu.Abstractions;
using Kabomu.Exceptions;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests
{
    public class StandardQuasiHttpClientServerTest
    {
        [Theory]
        [MemberData(nameof(CreateTestRequestSerializationData))]
        public async Task TestRequestSerialization(
            byte[] expectedReqBodyBytes,
            IQuasiHttpRequest request,
            IQuasiHttpRequest expectedRequest,
            byte[] expectedSerializedReq)
        {
            var remoteEndpoint = new object();
            if (expectedReqBodyBytes != null)
            {
                request.Body = new MemoryStream(expectedReqBodyBytes);
            }
            var dummyRes = new DefaultQuasiHttpResponse();
            var memStream = new MemoryStream();
            IQuasiHttpProcessingOptions sendOptions = new DefaultQuasiHttpProcessingOptions();
            var clientConnection = new QuasiHttpConnectionImpl
            {
                ProcessingOptions = sendOptions,
                WritableStream = memStream,
            };
            var client = new StandardQuasiHttpClient
            {
                Transport = new ClientTransportImpl(false)
                {
                    AllocateConnectionFunc = async (endPt, opts) =>
                    {
                        Assert.Same(remoteEndpoint, endPt);
                        Assert.Same(sendOptions, opts);
                        return new DefaultConnectionAllocationResponse
                        {
                            Connection = clientConnection
                        };
                    },
                    ResponseDeserializer = async conn =>
                    {
                        Assert.Same(clientConnection, conn);
                        return dummyRes;
                    }
                }
            };
            var actualRes = await client.Send(remoteEndpoint, request,
                sendOptions);
            Assert.Same(dummyRes, actualRes);

            if (expectedSerializedReq != null)
            {
                Assert.Equal(expectedSerializedReq, memStream.ToArray());
            }

            memStream.Position = 0; // reset for reading.

            // deserialize
            IQuasiHttpRequest actualRequest = null;
            var serverConnection = new QuasiHttpConnectionImpl
            {
                ReadableStream = new RandomizedReadInputStream(memStream),
                Environment = new Dictionary<string, object>()
            };
            var server = new StandardQuasiHttpServer
            {
                Transport = new ServerTransportImpl(false)
                {
                    ResponseSerializer = async (conn, res) =>
                    {
                        Assert.Same(serverConnection, conn);
                        Assert.Same(dummyRes, res);
                        return true;
                    }
                },
                Application = async req =>
                {
                    actualRequest = req;
                    return dummyRes;
                }
            };
            await server.AcceptConnection(serverConnection);

            // assert
            await ComparisonUtils.CompareRequests(expectedRequest,
                actualRequest, expectedReqBodyBytes);
            Assert.Same(serverConnection.Environment, actualRequest.Environment);
        }

        public static List<object[]> CreateTestRequestSerializationData()
        {
            var testData = new List<object[]>();

            var expectedReqBodyBytes = MiscUtilsInternal.StringToBytes(
                "tanner");
            var request = new DefaultQuasiHttpRequest
            {
                HttpMethod = "GET",
                Target = "/",
                HttpVersion = "HTTP/1.0",
                ContentLength = expectedReqBodyBytes.Length,
                Headers = new Dictionary<string, IList<string>>
                {
                    { "Accept", new string[]{ "text/plain", "text/csv" } },
                    { "Content-Type", new string[]{ "application/json,charset=UTF-8" } }
                }
            };
            IQuasiHttpRequest expectedRequest = new DefaultQuasiHttpRequest
            {
                HttpMethod = "GET",
                Target = "/",
                HttpVersion = "HTTP/1.0",
                ContentLength = expectedReqBodyBytes.Length,
                Headers = new Dictionary<string, IList<string>>
                {
                    { "accept", new string[]{ "text/plain", "text/csv" } },
                    { "content-type", new string[]{ "application/json,charset=UTF-8" } }
                }
            };
            var expectedSerializedReq = new byte[]
            {
                0x68, 0x64, 0x72, 0x73,
                0, 0, 0, 90
            }.Concat(MiscUtilsInternal.StringToBytes("GET,/,HTTP/1.0,6\n"))
            .Concat(MiscUtilsInternal.StringToBytes("Accept,text/plain,text/csv\n"))
            .Concat(MiscUtilsInternal.StringToBytes("Content-Type,\"application/json,charset=UTF-8\"\n"))
            .Concat(expectedReqBodyBytes)
            .ToArray();
            testData.Add(new object[] { expectedReqBodyBytes,
                request, expectedRequest, expectedSerializedReq });

            expectedReqBodyBytes = null;
            request = new DefaultQuasiHttpRequest();
            expectedRequest = new DefaultQuasiHttpRequest
            {
                HttpMethod = "",
                Target = "",
                HttpVersion = "",
                ContentLength = 0,
                Headers = new Dictionary<string, IList<string>>(),
            };
            expectedSerializedReq = new byte[]
            {
                0x68, 0x64, 0x72, 0x73,
                0, 0, 0, 11,
                (byte)'"', (byte)'"', (byte)',', (byte)'"', (byte)'"',
                (byte)',', (byte)'"', (byte)'"', (byte)',', (byte)'0',
                (byte)'\n'
            };
            testData.Add(new object[] { expectedReqBodyBytes,
                request, expectedRequest, expectedSerializedReq });

            expectedReqBodyBytes = new byte[] { 8, 7, 8, 9 };
            request = new DefaultQuasiHttpRequest
            {
                ContentLength = -1
            };
            expectedRequest = new DefaultQuasiHttpRequest
            {
                HttpMethod = "",
                Target = "",
                HttpVersion = "",
                ContentLength = -1,
                Headers = new Dictionary<string, IList<string>>(),
            };
            expectedSerializedReq = null;
            testData.Add(new object[] { expectedReqBodyBytes,
                request, expectedRequest, expectedSerializedReq });

            return testData;
        }

        [Theory]
        [MemberData(nameof(CreateTestRequestSerializationForErrorsData))]
        public async Task TestRequestSerializationForErrors(
            IQuasiHttpRequest request, IQuasiHttpProcessingOptions sendOptions,
            string expectedErrorMsg,
            byte[] expectedSerializedReq)
        {
            var remoteEndpoint = new object();
            var dummyRes = new DefaultQuasiHttpResponse();
            var memStream = new MemoryStream();
            var clientConnection = new QuasiHttpConnectionImpl
            {
                ProcessingOptions = sendOptions,
                WritableStream = memStream,
            };
            var client = new StandardQuasiHttpClient
            {
                Transport = new ClientTransportImpl(true)
                {
                    AllocateConnectionFunc = async (endPt, opts) =>
                    {
                        Assert.Same(remoteEndpoint, endPt);
                        Assert.Same(sendOptions, opts);
                        return new DefaultConnectionAllocationResponse
                        {
                            Connection = clientConnection
                        };
                    },
                    ResponseDeserializer = async conn =>
                    {
                        Assert.Same(clientConnection, conn);
                        return dummyRes;
                    }
                }
            };

            if (expectedErrorMsg == null)
            {
                var actualRes = await client.Send(remoteEndpoint, request,
                    sendOptions);
                Assert.Same(dummyRes, actualRes);

                if (expectedSerializedReq != null)
                {
                    Assert.Equal(expectedSerializedReq, memStream.ToArray());
                }
            }
            else
            {
                var actualEx = await Assert.ThrowsAnyAsync<Exception>(async () =>
                {
                    await client.Send(remoteEndpoint, request, sendOptions);
                });
                Assert.Contains(expectedErrorMsg, actualEx.Message);
            }
        }

        public static List<object[]> CreateTestRequestSerializationForErrorsData()
        {
            var testData = new List<object[]>();

            var request = new DefaultQuasiHttpRequest
            {
                HttpMethod = "POST",
                Target = "/Update",
                ContentLength = 8
            };
            var sendOptions = new DefaultQuasiHttpProcessingOptions
            {
                MaxHeadersSize = 18
            };
            string expectedErrorMsg = null;
            byte[] expectedSerializedReq = new byte[]
            {
                0x68, 0x64, 0x72, 0x73,
                0, 0, 0, 18
            }.Concat(MiscUtilsInternal.StringToBytes("POST,/Update,\"\",8\n"))
            .ToArray();
            testData.Add(new object[] { request, sendOptions, expectedErrorMsg,
                expectedSerializedReq });

            var requestBodyBytes = new byte[] { 4 };
            request = new DefaultQuasiHttpRequest
            {
                HttpMethod = "PUT",
                Target = "/Updates",
                ContentLength = 0,
                Body = new MemoryStream(requestBodyBytes)
            };
            sendOptions = new DefaultQuasiHttpProcessingOptions
            {
                MaxHeadersSize = 19
            };
            expectedErrorMsg = null;
            expectedSerializedReq = new byte[]
            {
                0x68, 0x64, 0x72, 0x73,
                0, 0, 0, 18
            }.Concat(MiscUtilsInternal.StringToBytes("PUT,/Updates,\"\",0\n"))
            .Concat(new byte[] { 0x62, 0x64, 0x74, 0x61 })
            .Concat(new byte[] { 0, 0, 0, 1, 4 })
            .Concat(new byte[] { 0x62, 0x64, 0x74, 0x61 })
            .Concat(new byte[] { 0, 0, 0, 0 })
            .ToArray();
            testData.Add(new object[] { request, sendOptions, expectedErrorMsg,
                expectedSerializedReq });

            requestBodyBytes = new byte[] { 4, 5, 6 };
            request = new DefaultQuasiHttpRequest
            {
                ContentLength = 10,
                Body = new MemoryStream(requestBodyBytes)
            };
            sendOptions = null;
            expectedErrorMsg = null;
            expectedSerializedReq = new byte[]
            {
                0x68, 0x64, 0x72, 0x73,
                0, 0, 0, 12
            }.Concat(MiscUtilsInternal.StringToBytes("\"\",\"\",\"\",10\n"))
            .Concat(requestBodyBytes)
            .ToArray();
            testData.Add(new object[] { request, sendOptions, expectedErrorMsg,
                expectedSerializedReq });

            request = new DefaultQuasiHttpRequest();
            sendOptions = new DefaultQuasiHttpProcessingOptions
            {
                MaxHeadersSize = 5
            };
            expectedErrorMsg = "quasi http headers exceed max size";
            expectedSerializedReq = null;
            testData.Add(new object[] { request, sendOptions, expectedErrorMsg,
                expectedSerializedReq });

            request = new DefaultQuasiHttpRequest
            {
                HttpVersion = "no-spaces-allowed",
                Headers = new Dictionary<string, IList<string>>
                {
                    { "empty-prohibited", new string[]{ "a: \nb" } }
                }
            };
            sendOptions = null;
            expectedErrorMsg = "quasi http header value contains newlines";
            expectedSerializedReq = null;
            testData.Add(new object[] { request, sendOptions, expectedErrorMsg,
                expectedSerializedReq });

            return testData;
        }

        [Theory]
        [MemberData(nameof(CreateTestResponseSerializationData))]
        public async Task TestResponseSerialization(
            byte[] expectedResBodyBytes,
            IQuasiHttpResponse response,
            IQuasiHttpResponse expectedResponse,
            byte[] expectedSerializedRes)
        {
            if (expectedResBodyBytes != null)
            {
                response.Body = new MemoryStream(expectedResBodyBytes);
            }
            var memStream = new MemoryStream();
            var serverConnection = new QuasiHttpConnectionImpl
            {
                WritableStream = memStream,
            };
            var dummyReq = new DefaultQuasiHttpRequest();
            var server = new StandardQuasiHttpServer
            {
                Transport = new ServerTransportImpl(true)
                {
                    RequestDeserializer = async (conn) =>
                    {
                        Assert.Same(serverConnection, conn);
                        return dummyReq;
                    }
                },
                Application = async req =>
                {
                    Assert.Same(dummyReq, req);
                    return response;
                }
            };
            await server.AcceptConnection(serverConnection);

            if (expectedSerializedRes != null)
            {
                Assert.Equal(expectedSerializedRes, memStream.ToArray());
            }

            memStream.Position = 0; // reset for reading.

            // deserialize
            var remoteEndpoint = new object();
            IQuasiHttpProcessingOptions sendOptions = new DefaultQuasiHttpProcessingOptions();
            var clientConnection = new QuasiHttpConnectionImpl
            {
                ProcessingOptions = sendOptions,
                ReadableStream = new RandomizedReadInputStream(memStream),
            };
            var client = new StandardQuasiHttpClient
            {
                Transport = new ClientTransportImpl(true)
                {
                    AllocateConnectionFunc = async (endPt, opts) =>
                    {
                        Assert.Same(remoteEndpoint, endPt);
                        Assert.Same(sendOptions, opts);
                        return new DefaultConnectionAllocationResponse
                        {
                            Connection = clientConnection
                        };
                    },
                    RequestSerializer = async (conn, req) =>
                    {
                        Assert.Same(clientConnection, conn);
                        Assert.Same(dummyReq, req);
                        return true;
                    }
                }
            };
            var actualRes = await client.Send(remoteEndpoint, dummyReq,
                sendOptions);

            // assert
            await ComparisonUtils.CompareResponses(expectedResponse,
                actualRes, expectedResBodyBytes);
        }

        public static List<object[]> CreateTestResponseSerializationData()
        {
            var testData = new List<object[]>();

            var expectedResBodyBytes = MiscUtilsInternal.StringToBytes(
                "sent");
            var response = new DefaultQuasiHttpResponse
            {
                HttpVersion = "HTTP/1.1",
                StatusCode = 400,
                HttpStatusMessage = "Bad, Request",
                ContentLength = expectedResBodyBytes.Length,
                Headers = new Dictionary<string, IList<string>>
                {
                    { "Status", new string[]{ "seen" } }
                }
            };
            IQuasiHttpResponse expectedResponse = new DefaultQuasiHttpResponse
            {
                HttpVersion = "HTTP/1.1",
                StatusCode = 400,
                HttpStatusMessage = "Bad, Request",
                ContentLength = expectedResBodyBytes.Length,
                Headers = new Dictionary<string, IList<string>>
                {
                    { "status", new string[]{ "seen" } }
                }
            };
            var expectedSerializedRes = new byte[]
            {
                0x68, 0x64, 0x72, 0x73,
                0, 0, 0, 42
            }.Concat(MiscUtilsInternal.StringToBytes("HTTP/1.1,400,\"Bad, Request\",4\n"))
            .Concat(MiscUtilsInternal.StringToBytes("Status,seen\n"))
            .Concat(expectedResBodyBytes)
            .ToArray();
            testData.Add(new object[] { expectedResBodyBytes,
                response, expectedResponse, expectedSerializedRes });

            expectedResBodyBytes = null;
            response = new DefaultQuasiHttpResponse();
            expectedResponse = new DefaultQuasiHttpResponse
            {
                HttpVersion = "",
                StatusCode = 0,
                HttpStatusMessage = "",
                ContentLength = 0,
                Headers = new Dictionary<string, IList<string>>(),
            };
            expectedSerializedRes = new byte[]
            {
                0x68, 0x64, 0x72, 0x73,
                0, 0, 0, 10,
                (byte)'"', (byte)'"', (byte)',', (byte)'0',
                (byte)',', (byte)'"', (byte)'"', (byte)',',
                (byte)'0', (byte)'\n'
            };
            testData.Add(new object[] { expectedResBodyBytes,
                response, expectedResponse, expectedSerializedRes });

            expectedResBodyBytes = new byte[] { 8, 7, 8, 9, 2 };
            response = new DefaultQuasiHttpResponse
            {
                ContentLength = -5
            };
            expectedResponse = new DefaultQuasiHttpResponse
            {
                HttpVersion = "",
                StatusCode = 0,
                HttpStatusMessage = "",
                ContentLength = -5,
                Headers = new Dictionary<string, IList<string>>(),
            };
            expectedSerializedRes = null;
            testData.Add(new object[] { expectedResBodyBytes,
                response, expectedResponse, expectedSerializedRes });

            return testData;
        }

        [Theory]
        [MemberData(nameof(CreateTestResponseDeserializationForErrorsData))]
        public async Task TestResponseDeserializationForErrors(
            byte[] serializedRes, IQuasiHttpProcessingOptions sendOptions,
            string expectedErrorMsg)
        {
            var memStream = new MemoryStream(serializedRes);
            var dummyReq = new DefaultQuasiHttpRequest();

            // deserialize
            var remoteEndpoint = new object();
            var clientConnection = new QuasiHttpConnectionImpl
            {
                ProcessingOptions = sendOptions,
                ReadableStream = new RandomizedReadInputStream(memStream),
                Environment = new Dictionary<string, object>()
            };
            var client = new StandardQuasiHttpClient
            {
                Transport = new ClientTransportImpl(false)
                {
                    AllocateConnectionFunc = async (endPt, opts) =>
                    {
                        Assert.Same(remoteEndpoint, endPt);
                        Assert.Same(sendOptions, opts);
                        return new DefaultConnectionAllocationResponse
                        {
                            Connection = clientConnection
                        };
                    },
                    RequestSerializer = async (conn, req) =>
                    {
                        Assert.Same(clientConnection, conn);
                        Assert.Same(dummyReq, req);
                        return true;
                    }
                }
            };

            if (expectedErrorMsg == null)
            {
                var res = await client.Send2(remoteEndpoint, async (env) =>
                {
                    Assert.Same(clientConnection.Environment, env);
                    return dummyReq;
                }, sendOptions);
                if (res.Body != null)
                {
                    await ComparisonUtils.ReadToBytes(res.Body);
                }
            }
            else
            {
                var actualEx = await Assert.ThrowsAnyAsync<Exception>(async () =>
                {
                    var res = await client.Send2(remoteEndpoint, async (env) =>
                    {
                        return dummyReq;
                    }, sendOptions);
                    if (res.Body != null)
                    {
                        await ComparisonUtils.ReadToBytes(res.Body);
                    }
                });
                Assert.Contains(expectedErrorMsg, actualEx.Message);
            }
        }

        public static List<object[]> CreateTestResponseDeserializationForErrorsData()
        {
            var testData = new List<object[]>();

            var serializedRes = new byte[]
            {
                0x68, 0x64, 0x72, 0x73,
                0, 0, 0, 30,
                (byte)'H', (byte)'T', (byte)'T', (byte)'P',
                (byte)'/', (byte)'1', (byte)'.', (byte)'1',
                (byte)',', (byte)'4', (byte)'0', (byte)'0',
                (byte)',', (byte)'"', (byte)'B', (byte)'a',
                (byte)'d', (byte)',', (byte)' ', (byte)'R',
                (byte)'e', (byte)'q', (byte)'u', (byte)'e',
                (byte)'s', (byte)'t', (byte)'"', (byte)',',
                (byte)'x', (byte)'\n',
                (byte)'s', (byte)'e', (byte)'n', (byte)'t'
            };
            IQuasiHttpProcessingOptions sendOpts = null;
            var expectedErrorMsg = "invalid quasi http response content length";
            testData.Add(new object[] { serializedRes, sendOpts,
                expectedErrorMsg });

            serializedRes = new byte[]
            {
                0x68, 0x64, 0x72, 0x73,
                0, 0, 0, 10,
                (byte)'"', (byte)'"', (byte)',', (byte)'y',
                (byte)',', (byte)'"', (byte)'"', (byte)',',
                (byte)'0', (byte)'\n'
            };
            sendOpts = null;
            expectedErrorMsg = "invalid quasi http response status code";
            testData.Add(new object[] { serializedRes, sendOpts,
                expectedErrorMsg });

            serializedRes = new byte[]
            {
                0x68, 0x64, 0x72, 0x10,
                0, 0, 0, 10,
                (byte)'"', (byte)'"', (byte)',', (byte)'1',
                (byte)',', (byte)'"', (byte)'"', (byte)',',
                (byte)'0', (byte)'\n'
            };
            sendOpts = null;
            expectedErrorMsg = "unexpected quasi http headers tag";
            testData.Add(new object[] { serializedRes, sendOpts,
                expectedErrorMsg });

            serializedRes = new byte[]
            {
                0x68, 0x64, 0x72, 0x73,
                0, 0, 0, 10,
                (byte)'"', (byte)'"', (byte)',', (byte)'1',
                (byte)',', (byte)'"', (byte)'"', (byte)',',
                (byte)'2', (byte)'\n',
                (byte)'0', (byte)'d'
            };
            sendOpts = new DefaultQuasiHttpProcessingOptions
            {
                MaxResponseBodySize = 2
            };
            expectedErrorMsg = null;
            testData.Add(new object[] { serializedRes, sendOpts,
                expectedErrorMsg });

            serializedRes = new byte[]
            {
                0x68, 0x64, 0x72, 0x73,
                0, 0, 0, 10,
                (byte)'"', (byte)'"', (byte)',', (byte)'1',
                (byte)',', (byte)'"', (byte)'"', (byte)',',
                (byte)'2', (byte)'\n',
                (byte)'0', (byte)'d'
            };
            sendOpts = new DefaultQuasiHttpProcessingOptions
            {
                MaxResponseBodySize = 1
            };
            expectedErrorMsg = "stream size exceeds limit";
            testData.Add(new object[] { serializedRes, sendOpts,
                expectedErrorMsg });

            serializedRes = new byte[]
            {
                0x68, 0x64, 0x72, 0x73,
                0, 0, 0, 10,
                (byte)'"', (byte)'"', (byte)',', (byte)'1',
                (byte)',', (byte)'"', (byte)'"', (byte)',',
                (byte)'2', (byte)'\n',
                (byte)'0', (byte)'d'
            };
            sendOpts = new DefaultQuasiHttpProcessingOptions
            {
                MaxHeadersSize = 1
            };
            expectedErrorMsg = "quasi http headers exceed max size";
            testData.Add(new object[] { serializedRes, sendOpts,
                expectedErrorMsg });

            serializedRes = new byte[]
            {
                0x68, 0x64, 0x72, 0x73,
                0, 0, 0, 10,
                (byte)'"', (byte)'"', (byte)',', (byte)'1',
                (byte)',', (byte)'"', (byte)'"', (byte)',',
                (byte)'2', (byte)'\n',
                (byte)'0', (byte)'d'
            };
            sendOpts = new DefaultQuasiHttpProcessingOptions
            {
                MaxHeadersSize = 11,
                MaxResponseBodySize = -1
            };
            expectedErrorMsg = null;
            testData.Add(new object[] { serializedRes, sendOpts,
                expectedErrorMsg });

            serializedRes = new byte[]
            {
                0x68, 0x64, 0x72, 0x73,
                0, 0, 0, 11,
                (byte)'"', (byte)'"', (byte)',', (byte)'1',
                (byte)',', (byte)'"', (byte)'"', (byte)',',
                (byte)'-', (byte)'1', (byte)'\n',
                0x62, 0x65, 0x78, 0x74,
                0, 0, 0, 3,
                (byte)'a', (byte)'b', (byte)'c',
                0x62,0x64, 0x74, 0x61,
                0, 0, 0, 0
            };
            sendOpts = new DefaultQuasiHttpProcessingOptions
            {
                MaxHeadersSize = 11,
                MaxResponseBodySize = 1
            };
            expectedErrorMsg = null;
            testData.Add(new object[] { serializedRes, sendOpts,
                expectedErrorMsg });

            serializedRes = new byte[]
            {
                0x68, 0x64, 0x72, 0x73,
                0, 0, 0, 11,
                (byte)'"', (byte)'"', (byte)',', (byte)'1',
                (byte)',', (byte)'"', (byte)'"', (byte)',',
                (byte)'-', (byte)'1', (byte)'\n',
                0x62,0x64, 0x74, 0x61,
                0, 0, 0, 3,
                (byte)'a', (byte)'b', (byte)'c',
                0x62,0x64, 0x74, 0x61,
                0, 0, 0, 0
            };
            sendOpts = new DefaultQuasiHttpProcessingOptions
            {
                MaxResponseBodySize = 1
            };
            expectedErrorMsg = "stream size exceeds limit";
            testData.Add(new object[] { serializedRes, sendOpts,
                expectedErrorMsg });

            serializedRes = new byte[]
            {
                0x68, 0x64, 0x72, 0x73,
                0, 0, 0, 11,
                (byte)'"', (byte)'"', (byte)',', (byte)'1',
                (byte)',', (byte)'"', (byte)'"', (byte)',',
                (byte)'8', (byte)'2', (byte)'\n',
                (byte)'a', (byte)'b', (byte)'c',
            };
            sendOpts = new DefaultQuasiHttpProcessingOptions
            {
                MaxResponseBodySize = -1
            };
            expectedErrorMsg = "end of read";
            testData.Add(new object[] { serializedRes, sendOpts,
                expectedErrorMsg });

            return testData;
        }

        class ClientTransportImpl : IQuasiHttpClientTransport, IQuasiHttpAltTransport
        {
            public ClientTransportImpl(bool initializeSerializerFunctions)
            {
                if (!initializeSerializerFunctions)
                {
                    return;
                }
                RequestSerializer = (_, _) => Task.FromResult(false);
                ResponseSerializer = (_, _) => Task.FromResult(false);
                RequestDeserializer = _ => Task.FromResult<IQuasiHttpRequest>(null);
                ResponseDeserializer = _ => Task.FromResult<IQuasiHttpResponse>(null);
            }

            public Func<IQuasiHttpConnection, IQuasiHttpRequest, Task<bool>> RequestSerializer { get; set; }

            public Func<IQuasiHttpConnection, IQuasiHttpResponse, Task<bool>> ResponseSerializer { get; set; }

            public Func<IQuasiHttpConnection, Task<IQuasiHttpRequest>> RequestDeserializer { get; set; }

            public Func<IQuasiHttpConnection, Task<IQuasiHttpResponse>> ResponseDeserializer { get; set; }

            public Func<object, IQuasiHttpProcessingOptions, Task<IConnectionAllocationResponse>>
                AllocateConnectionFunc { get; set; }

            public async Task<IConnectionAllocationResponse> AllocateConnection(
                object remoteEndpoint, IQuasiHttpProcessingOptions sendOptions)
            {
                return await AllocateConnectionFunc(remoteEndpoint, sendOptions);
            }

            public Stream GetReadableStream(IQuasiHttpConnection connection)
            {
                return ((QuasiHttpConnectionImpl)connection).ReadableStream;
            }

            public Stream GetWritableStream(IQuasiHttpConnection connection)
            {
                return ((QuasiHttpConnectionImpl)connection).WritableStream;
            }

            public Task ReleaseConnection(IQuasiHttpConnection connection,
                IQuasiHttpResponse response)
            {
                return Task.CompletedTask;
            }
        }

        class ServerTransportImpl : IQuasiHttpServerTransport, IQuasiHttpAltTransport
        {
            public ServerTransportImpl(bool initializeSerializerFunctions)
            {
                if (!initializeSerializerFunctions)
                {
                    return;
                }
                RequestSerializer = (_, _) => Task.FromResult(false);
                ResponseSerializer = (_, _) => Task.FromResult(false);
                RequestDeserializer = _ => Task.FromResult<IQuasiHttpRequest>(null);
                ResponseDeserializer = _ => Task.FromResult<IQuasiHttpResponse>(null);
            }

            public Func<IQuasiHttpConnection, IQuasiHttpRequest, Task<bool>> RequestSerializer { get; set; }

            public Func<IQuasiHttpConnection, IQuasiHttpResponse, Task<bool>> ResponseSerializer { get; set; }

            public Func<IQuasiHttpConnection, Task<IQuasiHttpRequest>> RequestDeserializer { get; set; }

            public Func<IQuasiHttpConnection, Task<IQuasiHttpResponse>> ResponseDeserializer { get; set; }

            public Stream GetReadableStream(IQuasiHttpConnection connection)
            {
                return ((QuasiHttpConnectionImpl)connection).ReadableStream;
            }

            public Stream GetWritableStream(IQuasiHttpConnection connection)
            {
                return ((QuasiHttpConnectionImpl)connection).WritableStream;
            }

            public Task ReleaseConnection(IQuasiHttpConnection connection)
            {
                return Task.CompletedTask;
            }
        }

        class QuasiHttpConnectionImpl : IQuasiHttpConnection
        {
            public Stream ReadableStream { get; set; }
            public Stream WritableStream { get; set; }
            public IQuasiHttpProcessingOptions ProcessingOptions { get; set; }
            public IDictionary<string, object> Environment { get; set; }
            public CancellationToken CancellationToken => CancellationToken.None;
            public Task<bool> TimeoutTask => null;
        }
    }
}