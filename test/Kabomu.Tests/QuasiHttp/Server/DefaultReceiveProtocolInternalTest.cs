using Kabomu.Common;
using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.ChunkedTransfer;
using Kabomu.QuasiHttp.EntityBody;
using Kabomu.QuasiHttp.Server;
using Kabomu.QuasiHttp.Transport;
using Kabomu.Tests.Internals;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.QuasiHttp.Server
{
    public class DefaultReceiveProtocolInternalTest
    {
        [Fact]
        public async Task TestReceiveForDependencyErrors()
        {
            await Assert.ThrowsAsync<MissingDependencyException>(() =>
            {
                var instance = new DefaultReceiveProtocolInternal
                {
                    Transport = new ConfigurableQuasiHttpTransport()
                };
                return instance.Receive();
            });

            await Assert.ThrowsAsync<MissingDependencyException>(() =>
            {
                var instance = new DefaultReceiveProtocolInternal
                {
                    Application = new ConfigurableQuasiHttpApplication()
                };
                return instance.Receive();
            });
        }

        [Fact]
        public async Task TestReceiveForRejectionOfNullResponses()
        {
            var reqChunk = new LeadChunk
            {
                Version = LeadChunk.Version01
            };
            var inputStream = new MemoryStream();
            var serializedReq = reqChunk.Serialize();
            MiscUtils.WriteChunk(serializedReq, (data, offset, length) =>
                inputStream.Write(data, offset, length));
            inputStream.Position = 0; // rewind for reads.

            var transport = new ConfigurableQuasiHttpTransport
            {
                ReadBytesCallback = async (actualConnection, data, offset, length) =>
                {
                    Assert.Null(actualConnection);
                    var bytesRead = inputStream.Read(data, offset, length);
                    return bytesRead;
                }
            };
            IQuasiHttpResponse response = null;
            var app = new ConfigurableQuasiHttpApplication
            {
                ProcessRequestCallback = (req, actualReqEnv) =>
                {
                    Assert.Null(actualReqEnv);
                    return Task.FromResult(response);
                }
            };
            var instance = new DefaultReceiveProtocolInternal
            {
                Transport = transport,
                Application = app,
            };
            var cbCallCount = 0;
            instance.AbortCallback = async (parent) =>
            {
                cbCallCount++;
            };
            var ex = await Assert.ThrowsAnyAsync<Exception>(() =>
            {
                return instance.Receive();
            });
            Assert.Contains("no response", ex.Message);
            Assert.Equal(0, cbCallCount);
        }

        [Fact]
        public async Task TestReceiveEnsuresCloseOnSuccessfulResponse()
        {
            var request = new DefaultQuasiHttpRequest();
            var connectivityParams = new DefaultConnectivityParams();
            var response = new ErrorQuasiHttpResponse();

            var reqChunk = new LeadChunk
            {
                Version = LeadChunk.Version01
            };
            var inputStream = new MemoryStream();
            var serializedReq = reqChunk.Serialize();
            MiscUtils.WriteChunk(serializedReq, (data, offset, length) =>
                inputStream.Write(data, offset, length));
            inputStream.Position = 0; // rewind for reads.

            var transport = new ConfigurableQuasiHttpTransport
            {
                ReadBytesCallback = async (actualConnection, data, offset, length) =>
                {
                    Assert.Null(actualConnection);
                    var bytesRead = inputStream.Read(data, offset, length);
                    return bytesRead;
                }
            };

            var app = new ConfigurableQuasiHttpApplication
            {
                ProcessRequestCallback = (req, actualReqEnv) =>
                {
                    Assert.Null(actualReqEnv);
                    return Task.FromResult((IQuasiHttpResponse)response);
                }
            };
            var instance = new DefaultReceiveProtocolInternal
            {
                Application = app,
                Transport = transport
            };
            var cbCallCount = 0;
            instance.AbortCallback = async (parent) =>
            {
                cbCallCount++;
            };
            await Assert.ThrowsAsync<NotImplementedException>(() =>
            {
                return instance.Receive();
            });

            Assert.Equal(0, cbCallCount);
            Assert.True(response.CloseCalled);
        }

        [Theory]
        [MemberData(nameof(CreateTestReceiveData))]
        public async Task TestReceive(object connection, int maxChunkSize,
            IQuasiHttpRequest request, byte[] requestBodyBytes, IDictionary<string, object> reqEnv,
            IQuasiHttpResponse expectedResponse, byte[] expectedResponseBodyBytes)
        {
            // arrange.
            var reqChunk = new LeadChunk
            {
                Version = LeadChunk.Version01,
                Path = request.Path,
                Headers = request.Headers,
                ContentLength = request.Body?.ContentLength ?? 0,
                ContentType = request.Body?.ContentType,
                HttpVersion = request.HttpVersion,
                HttpMethod = request.HttpMethod
            };

            var expectedResChunk = new LeadChunk
            {
                Version = LeadChunk.Version01,
                StatusIndicatesSuccess = expectedResponse.StatusIndicatesSuccess,
                StatusIndicatesClientError = expectedResponse.StatusIndicatesClientError,
                StatusMessage = expectedResponse.StatusMessage,
                Headers = expectedResponse.Headers,
                ContentLength = expectedResponse.Body?.ContentLength ?? 0,
                ContentType = expectedResponse.Body?.ContentType,
                HttpVersion = expectedResponse.HttpVersion,
                HttpStatusCode = expectedResponse.HttpStatusCode
            };

            var inputStream = new MemoryStream();
            var serializedReq = reqChunk.Serialize();
            MiscUtils.WriteChunk(serializedReq, (data, offset, length) =>
                inputStream.Write(data, offset, length));
            if (requestBodyBytes != null)
            {
                if (request.Body.ContentLength < 0)
                {
                    var reqBodyChunk = new SubsequentChunk
                    {
                        Version = LeadChunk.Version01,
                        Data = requestBodyBytes,
                        DataLength = requestBodyBytes.Length
                    }.Serialize();
                    MiscUtils.WriteChunk(reqBodyChunk, (data, offset, length) =>
                        inputStream.Write(data, offset, length));
                    // write trailing empty chunk.
                    var emptyBodyChunk = new SubsequentChunk
                    {
                        Version = LeadChunk.Version01
                    }.Serialize();
                    MiscUtils.WriteChunk(emptyBodyChunk, (data, offset, length) =>
                        inputStream.Write(data, offset, length));
                }
                else
                {
                    inputStream.Write(requestBodyBytes);
                }
            }
            inputStream.Position = 0; // rewind read pointer.
            var outputStream = new MemoryStream();
            IQuasiHttpTransport transport = new ConfigurableQuasiHttpTransport
            {
                ReadBytesCallback = async (actualConnection, data, offset, length) =>
                {
                    Assert.Equal(connection, actualConnection);
                    var bytesRead = inputStream.Read(data, offset, length);
                    return bytesRead;
                },
                WriteBytesCallback = async (actualConnection, data, offset, length) =>
                {
                    Assert.Equal(connection, actualConnection);
                    outputStream.Write(data, offset, length);
                },
                ReleaseConnectionCallback = (actualConnection) =>
                {
                    Assert.Equal(connection, actualConnection);
                    return Task.CompletedTask;
                },
            };
            var instance = new DefaultReceiveProtocolInternal
            {
                Parent = new object(),
                Transport = transport,
                Connection = connection,
                RequestEnvironment = reqEnv,
                MaxChunkSize = maxChunkSize
            };

            IQuasiHttpRequest actualRequest = null;
            IDictionary<string, object> actualRequestEnvironment = null;
            instance.Application = new ConfigurableQuasiHttpApplication
            {
                ProcessRequestCallback = async (req, env) =>
                {
                    actualRequest = req;
                    actualRequestEnvironment = env;
                    return expectedResponse;
                }
            };
            int abortCallCount = 0;
            object actualProtocolParentSeen = null;
            instance.AbortCallback = (parent) =>
            {
                actualProtocolParentSeen = parent;
                abortCallCount++;
                return Task.CompletedTask;
            };

            // act
            await instance.Receive();

            // assert
            Assert.Equal(1, abortCallCount);
            Assert.Equal(instance.Parent, actualProtocolParentSeen);
            await ComparisonUtils.CompareRequests(maxChunkSize, request, actualRequest,
                requestBodyBytes);
            Assert.Equal(reqEnv, actualRequestEnvironment);

            // ensure that response closure occured by trying to read response body
            if (expectedResponse.Body != null)
            {
                await Assert.ThrowsAsync<EndOfReadException>(() =>
                {
                    return expectedResponse.Body.ReadBytes(new byte[1], 0, 1);
                });
            }

            // finally verify contents of output stream.
            var actualRes = outputStream.ToArray();
            Assert.NotEmpty(actualRes);
            var actualResChunkLength = (int)ByteUtils.DeserializeUpToInt64BigEndian(actualRes, 0,
                MiscUtils.LengthOfEncodedChunkLength);
            var actualResChunk = LeadChunk.Deserialize(actualRes,
                MiscUtils.LengthOfEncodedChunkLength, actualResChunkLength);
            ComparisonUtils.CompareLeadChunks(expectedResChunk, actualResChunk);
            var actualResponseBodyLen = actualRes.Length - 
                MiscUtils.LengthOfEncodedChunkLength - actualResChunkLength;
            if (expectedResponseBodyBytes == null)
            {
                Assert.Equal(0, actualResponseBodyLen);
            }
            else
            {
                byte[] actualResBodyBytes;
                if (actualResChunk.ContentLength < 0)
                {
                    actualResBodyBytes = await MiscUtils.ReadChunkedBody(actualRes,
                        actualResChunkLength + MiscUtils.LengthOfEncodedChunkLength,
                        actualResponseBodyLen);
                }
                else
                {
                    actualResBodyBytes = new byte[actualResponseBodyLen];
                    Array.Copy(actualRes, actualResChunkLength + MiscUtils.LengthOfEncodedChunkLength,
                        actualResBodyBytes, 0, actualResponseBodyLen);
                }
                Assert.Equal(expectedResponseBodyBytes, actualResBodyBytes);
            }
        }

        public static List<object[]> CreateTestReceiveData()
        {
            var testData = new List<object[]>();

            object connection = "vgh";
            int maxChunkSize = 100;
            var request = new DefaultQuasiHttpRequest
            {
                HttpMethod = "POST",
                Path = "/koobi",
                Headers = new Dictionary<string, List<string>>
                {
                    { "variant", new List<string>{ "sea", "drive" } }
                }
            };
            var reqBodyBytes = Encoding.UTF8.GetBytes("this is our king");
            request.Body = new ByteBufferBody(reqBodyBytes, 0, reqBodyBytes.Length)
            {
                ContentType = "text/plain"
            };
            IDictionary<string, object> reqEnv = null;

            var expectedResponse = new DefaultQuasiHttpResponse
            {
                StatusIndicatesSuccess = true,
                StatusMessage = "ok",
                HttpStatusCode = 200,
                Headers = new Dictionary<string, List<string>>
                {
                    { "dkt", new List<string>{ "bb" } }
                },
            };
            byte[] expectedResBodyBytes = Encoding.UTF8.GetBytes("and this is our queen");
            expectedResponse.Body = new ByteBufferBody(expectedResBodyBytes, 0, expectedResBodyBytes.Length)
            {
                ContentType = "image/png"
            };
            testData.Add(new object[] { connection, maxChunkSize, request, reqBodyBytes, reqEnv,
                expectedResponse, expectedResBodyBytes });

            connection = 123;
            maxChunkSize = 95;
            request = new DefaultQuasiHttpRequest
            {
                Path = "/p"
            };
            reqBodyBytes = null;
            reqEnv = new Dictionary<string, object>
            {
                { "is_ssl", "true" }
            };

            expectedResponse = new DefaultQuasiHttpResponse
            {
                StatusIndicatesSuccess = false,
                StatusIndicatesClientError = true,
                StatusMessage = "not found"
            };
            expectedResBodyBytes = null;
            testData.Add(new object[] { connection, maxChunkSize, request, reqBodyBytes, reqEnv,
                expectedResponse, expectedResBodyBytes });

            connection = null;
            maxChunkSize = 90;
            request = new DefaultQuasiHttpRequest
            {
                HttpVersion = "1.1",
                Path = "/bread"
            };
            reqBodyBytes = Encoding.UTF8.GetBytes("<a>this is news</a>");
            request.Body = new StringBody("<a>this is news</a>")
            {
                ContentType = "application/xml"
            };
            reqEnv = new Dictionary<string, object>();

            expectedResponse = new DefaultQuasiHttpResponse
            {
                HttpVersion = "1.1",
                StatusIndicatesSuccess = false,
                StatusIndicatesClientError = false,
                StatusMessage = "server error"
            };
            expectedResBodyBytes = null;
            testData.Add(new object[] { connection, maxChunkSize, request, reqBodyBytes, reqEnv,
                expectedResponse, expectedResBodyBytes });

            connection = new object();
            maxChunkSize = 150;
            request = new DefaultQuasiHttpRequest
            {
                Path = "/fxn",
                Headers = new Dictionary<string, List<string>>
                {
                    { "x", new List<string>() },
                    { "a", new List<string>{ "A" } },
                    { "bb", new List<string>{ "B1", "B2" } },
                    { "ccc", new List<string>{ "C1", "C2", "C3" } }
                }
            };
            reqBodyBytes = null;
            reqEnv = new Dictionary<string, object>
            {
                { "r", 2 }, { "tea", new byte[3] }
            };

            expectedResponse = new DefaultQuasiHttpResponse
            {
                StatusIndicatesSuccess = false,
                StatusIndicatesClientError = false,
                StatusMessage = "server error",
                Headers = new Dictionary<string, List<string>>
                {
                    { "x", new List<string>{ "A" } },
                    { "y", new List<string>{ "B1", "B2", "C1", "C2", "C3" } }
                }
            };
            expectedResBodyBytes =  Encoding.UTF8.GetBytes("<a>this is news</a>");
            expectedResponse.Body = new StringBody("<a>this is news</a>")
            {
                ContentType = "application/xml"
            };
            testData.Add(new object[] { connection, maxChunkSize, request, reqBodyBytes, reqEnv,
                expectedResponse, expectedResBodyBytes });

            return testData;
        }

        class ErrorQuasiHttpResponse : IQuasiHttpResponse
        {
            public bool CloseCalled { get; set; }

            public bool StatusIndicatesSuccess => throw new NotImplementedException();

            public bool StatusIndicatesClientError => throw new NotImplementedException();

            public string StatusMessage => throw new NotImplementedException();

            public IDictionary<string, List<string>> Headers => throw new NotImplementedException();

            public IQuasiHttpBody Body => throw new NotImplementedException();

            public int HttpStatusCode => throw new NotImplementedException();

            public string HttpVersion => throw new NotImplementedException();

            public Task Close()
            {
                CloseCalled = true;
                return Task.CompletedTask;
            }
        }
    }
}
