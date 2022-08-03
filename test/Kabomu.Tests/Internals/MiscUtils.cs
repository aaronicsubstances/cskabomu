using Kabomu.Common;
using Kabomu.Concurrency;
using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.ChunkedTransfer;
using Kabomu.QuasiHttp.Client;
using Kabomu.QuasiHttp.EntityBody;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.Internals
{
    public static class MiscUtils
    {
        public static readonly int LengthOfEncodedChunkLength = 3;

        private static void WriteChunk(ByteBufferSlice[] slices, Stream outputStream)
        {
            var byteCount = ByteUtils.CalculateSizeOfSlices(slices);
            var encodedLength = new byte[LengthOfEncodedChunkLength];
            ByteUtils.SerializeUpToInt64BigEndian(byteCount, encodedLength, 0, encodedLength.Length);
            outputStream.Write(encodedLength, 0, encodedLength.Length);
            foreach (var slice in slices)
            {
                outputStream.Write(slice.Data, slice.Offset, slice.Length);
            }
        }

        public static Task<byte[]> ReadChunkedBody(byte[] data, int offset, int length)
        {
            var body = new ChunkDecodingBody(new ByteBufferBody(data, offset, length), 100);
            return TransportUtils.ReadBodyToEnd(body, 100);
        }

        public static MemoryStream CreateRequestInputStream(IQuasiHttpRequest request,
            byte[] requestBodyBytes)
        {
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

            var stream = new MemoryStream();
            var serializedReq = reqChunk.Serialize();
            WriteChunk(serializedReq, stream);
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
                    WriteChunk(reqBodyChunk, stream);
                    // write trailing empty chunk.
                    var emptyBodyChunk = new SubsequentChunk
                    {
                        Version = LeadChunk.Version01
                    }.Serialize();
                    WriteChunk(emptyBodyChunk, stream);
                }
                else
                {
                    stream.Write(requestBodyBytes);
                }
            }
            stream.Position = 0; // rewind read pointer.
            return stream;
        }

        public static void AssertMessageInErrorTree(string expectedSubstring, Exception actualError)
        {
            Exception e = actualError;
            while (e != null)
            {
                if (e.Message.Contains(expectedSubstring))
                {
                    break;
                }
                e = e.InnerException;
            }
            Assert.True(e != null, $"could not find substring in error tree: {expectedSubstring}");
        }

        public static MemoryStream CreateResponseInputStream(IQuasiHttpResponse response,
            byte[] responseBodyBytes)
        {
            var resChunk = new LeadChunk
            {
                Version = LeadChunk.Version01,
                StatusIndicatesSuccess = response.StatusIndicatesSuccess,
                StatusIndicatesClientError = response.StatusIndicatesClientError,
                StatusMessage = response.StatusMessage,
                Headers = response.Headers,
                ContentLength = response.Body?.ContentLength ?? 0,
                ContentType = response.Body?.ContentType,
                HttpVersion = response.HttpVersion,
                HttpStatusCode = response.HttpStatusCode
            };
            var stream = new MemoryStream();
            var serializedRes = resChunk.Serialize();
            WriteChunk(serializedRes, stream);
            if (responseBodyBytes != null)
            {
                if (response.Body.ContentLength < 0)
                {
                    var resBodyChunk = new SubsequentChunk
                    {
                        Version = LeadChunk.Version01,
                        Data = responseBodyBytes,
                        DataLength = responseBodyBytes.Length
                    }.Serialize();
                    WriteChunk(resBodyChunk, stream);
                    // write trailing empty chunk.
                    var emptyBodyChunk = new SubsequentChunk
                    {
                        Version = LeadChunk.Version01
                    }.Serialize();
                    WriteChunk(emptyBodyChunk, stream);
                }
                else
                {
                    stream.Write(responseBodyBytes);
                }
            }
            stream.Position = 0; // rewind read pointer.
            return stream;
        }

        public static Task<IQuasiHttpResponse> SendWithDelay(IQuasiHttpClient instance, IEventLoopApi testEventLoop, int delay,
            object remoteEndpoint, IQuasiHttpRequest request, IQuasiHttpSendOptions sendOptions)
        {
            var responseTcs = new TaskCompletionSource<IQuasiHttpResponse>();
            testEventLoop.SetTimeout(async () =>
            {
                try
                {
                    var res = await instance.Send(remoteEndpoint, request, sendOptions);
                    responseTcs.SetResult(res);
                }
                catch (Exception e)
                {
                    responseTcs.SetException(e);
                }
            }, delay);
            return responseTcs.Task;
        }
    }
}
