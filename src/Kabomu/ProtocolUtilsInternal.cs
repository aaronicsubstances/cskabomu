using Kabomu.Abstractions;
using Kabomu.Exceptions;
using Kabomu.Impl;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu
{
    internal static class ProtocolUtilsInternal
    {
        internal static bool? GetEnvVarAsBoolean(IDictionary<string, object> environment,
            string key)
        {
            if (environment != null && environment.ContainsKey(key))
            {
                var value = environment[key];
                if (value is bool b)
                {
                    return b;
                }
                else if (value != null)
                {
                    return bool.Parse((string)value);
                }
            }
            return null;
        }

        internal static Stream EncodeBodyToTransport(bool isResponse,
            long contentLength, Stream body)
        {
            if (contentLength == 0)
            {
                return null;
            }
            if (!isResponse && contentLength < 0)
            {
                return null;
            }
            // don't enforce positive content lengths when writing out
            // quasi http bodies
            return body;
        }

        internal static Stream DecodeRequestBodyFromTransport(
            long contentLength, Stream body)
        {
            if (contentLength == 0)
            {
                return null;
            }
            if (contentLength < 0)
            {
                return null;
            }
            return new ContentLengthEnforcingStream(body, contentLength);
        }

        internal static async Task<bool> DecodeResponseBodyFromTransport(
            DefaultQuasiHttpResponse response,
            IDictionary<string, object> environment,
            IQuasiHttpProcessingOptions processingOptions,
            CancellationToken cancellationToken)
        {
            var responseStreamingEnabled = processingOptions?.ResponseBufferingEnabled == false;
            if (GetEnvVarAsBoolean(
                environment, QuasiHttpCodec.EnvKeySkipResBodyDecoding) == true)
            {
                return responseStreamingEnabled;
            }
            if (responseStreamingEnabled)
            {
                if (response.ContentLength > 0)
                {
                    response.Body = new ContentLengthEnforcingStream(response.Body,
                        response.ContentLength);
                }
                return true;
            }
            int bufferingLimit = processingOptions?.ResponseBodyBufferingSizeLimit ?? 0;
            if (bufferingLimit <= 0)
            {
                bufferingLimit = QuasiHttpCodec.DefaultDataBufferLimit;
            }
            if (response.ContentLength < 0)
            {
                response.Body = await QuasiHttpCodec.ReadAllBytes(response.Body,
                    bufferingLimit, cancellationToken);
            }
            else
            {
                if (response.ContentLength > bufferingLimit)
                {
                    throw new QuasiHttpRequestProcessingException(
                        "response body length exceeds buffering limit " +
                        $"({response.ContentLength} > {bufferingLimit})",
                        QuasiHttpRequestProcessingException.ReasonCodeMessageLengthLimitExceeded);
                }
                var buffer = new byte[(int)response.ContentLength];
                await MiscUtils.ReadBytesFully(response.Body,
                    buffer, 0, buffer.Length,
                    cancellationToken);
                response.Body = new MemoryStream(buffer);
            }
            return false;
        }
    }
}
