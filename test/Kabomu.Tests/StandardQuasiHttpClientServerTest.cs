using Kabomu.Abstractions;
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
            if (expectedRequest == null)
            {
                expectedRequest = request;
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
                Transport = new ClientTransportImpl
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
                        Assert.Equal(clientConnection, conn);
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
                ReadableStream = memStream,
            };
            var server = new StandardQuasiHttpServer
            {
                Transport = new ServerTransportImpl
                {
                    ResponseSerializer = async (conn, res) =>
                    {
                        Assert.Equal(serverConnection, conn);
                        Assert.Equal(dummyRes, res);
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
                Headers = new Dictionary<string, IList<string>>(),
                Body = new MemoryStream(expectedReqBodyBytes)
            };
            IQuasiHttpRequest expectedRequest = null;
            var expectedSerializedReq = new byte[]
            {
                0x68, 0x64, 0x72, 0x73,
                0, 0, 0, 17,
                (byte)'G', (byte)'E', (byte)'T', (byte)',',
                (byte)'/', (byte)',', (byte)'H', (byte)'T',
                (byte)'T', (byte)'P', (byte)'/', (byte)'1',
                (byte)'.', (byte)'0', (byte)',', (byte)'6',
                (byte)'\n',
                (byte)'t', (byte)'a', (byte)'n', (byte)'n', (byte)'e',
                (byte)'r'
            };
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

            return testData;
        }

        class ClientTransportImpl : IQuasiHttpClientTransport, IQuasiHttpAltTransport
        {
            public Func<IQuasiHttpConnection, IQuasiHttpRequest, Task<bool>> RequestSerializer { get; set; }

            public Func<IQuasiHttpConnection, IQuasiHttpResponse, Task<bool>> ResponseSerializer { get; set; }

            public Func<IQuasiHttpConnection, Task<IQuasiHttpRequest>> RequestDeserializer { get; set; }

            public Func<IQuasiHttpConnection, Task<IQuasiHttpResponse>> ResponseDeserializer { get; set; }

            public Func<object, IQuasiHttpProcessingOptions, Task<IConnectionAllocationResponse>>
                AllocateConnectionFunc
            { get; set; }

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