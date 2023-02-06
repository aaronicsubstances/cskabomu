﻿using Kabomu.Common;
using Kabomu.Concurrency;
using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.ChunkedTransfer;
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
                RequestTarget = request.Target,
                Headers = request.Headers,
                ContentLength = request.Body?.ContentLength ?? 0,
                ContentType = request.Body?.ContentType,
                HttpVersion = request.HttpVersion,
                Method = request.Method
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
                else if (request.Body.ContentLength > 0)
                {
                    stream.Write(ChunkEncodingBody.EncodedChunkLengthOfDefaultInvalidValue);
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
                StatusCode = response.StatusCode,
                HttpStatusMessage = response.HttpStatusMessage,
                Headers = response.Headers,
                ContentLength = response.Body?.ContentLength ?? 0,
                ContentType = response.Body?.ContentType,
                HttpVersion = response.HttpVersion,
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
                else if (response.Body.ContentLength > 0)
                {
                    stream.Write(ChunkEncodingBody.EncodedChunkLengthOfDefaultInvalidValue);
                    stream.Write(responseBodyBytes);
                }
            }
            stream.Position = 0; // rewind read pointer.
            return stream;
        }

        public static Task<IQuasiHttpResponse> EnsureCompletedTask(Task<IQuasiHttpResponse> sendTask)
        {
            if (sendTask.IsCompleted)
            {
                return sendTask;
            }
            throw new Exception("task is not completed");
        }

        public static async Task<T> Delay<T>(ITimerApi timerApi, int delay, Func<Task<T>> cb)
        {
            await timerApi.Delay(delay);
            Task<T> t = cb?.Invoke();
            if (t != null)
            {
                return await t;
            }
            else
            {
                return default(T);
            }
        }

        public static async Task Delay(ITimerApi timerApi, int delay, Func<Task> cb)
        {
            await timerApi.Delay(delay);
            Task t = cb?.Invoke();
            if (t != null)
            {
                await t;
            }
        }
    }
}
