using Kabomu.Abstractions;
using Kabomu.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.ProtocolImpl
{
    internal static class ProtocolUtilsInternal
    {
        public static bool? GetEnvVarAsBoolean(IDictionary<string, object> environment,
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

        public static async Task WrapTimeoutTask(Task<bool> timeoutTask,
            string timeoutMsg)
        {
            if (timeoutTask == null)
            {
                return;
            }
            if (await timeoutTask)
            {
                throw new QuasiHttpException(timeoutMsg,
                    QuasiHttpException.ReasonCodeTimeout);
            }
        }

        public static async Task WriteEntityToTransport(bool isResponse,
            object entity, Stream writableStream,
            IQuasiHttpConnection connection)
        {
            if (writableStream == null)
            {
                throw new MissingDependencyException(
                    "no writable stream found for transport");
            }
            Stream body;
            long contentLength;
            IList<string> reqOrStatusLine;
            IDictionary<string, IList<string>> headers;
            if (isResponse)
            {
                var response = (IQuasiHttpResponse)entity;
                headers = response.Headers;
                body = response.Body;
                contentLength = response.ContentLength;
                reqOrStatusLine = new string[] {
                    response.StatusCode.ToString(),
                    response.HttpStatusMessage,
                    response.HttpVersion,
                    null
                };
            }
            else
            {
                var request = (IQuasiHttpRequest)entity;
                headers = request.Headers;
                body = request.Body;
                contentLength = request.ContentLength;
                reqOrStatusLine = new string[] {
                    request.HttpMethod,
                    request.Target,
                    request.HttpVersion,
                    null
                };
            }
            if (contentLength == 0)
            {
                contentLength = body != null ? -1 : 0;
            }
            reqOrStatusLine[3] = contentLength.ToString();
            await QuasiHttpCodec.WriteQuasiHttpHeaders(writableStream,
                reqOrStatusLine, headers,
                connection.ProcessingOptions?.MaxHeadersSize ?? 0,
                connection.CancellationToken);
            if (body == null)
            {
                return;
            }
            if (contentLength < 0)
            {
                var bodyWriter = TlvUtils.CreateTlvEncodingWritableStream(
                    writableStream, QuasiHttpCodec.TagForBody);
                await body.CopyToAsync(bodyWriter, connection.CancellationToken);
                await TlvUtils.WriteEndOfTlvStream(writableStream,
                    QuasiHttpCodec.TagForBody, connection.CancellationToken);
            }
            else
            {
                // don't enforce positive content lengths when writing out
                // quasi http bodies
                await body.CopyToAsync(writableStream,
                    connection.CancellationToken);
            }
        }

        public static async Task<object> ReadEntityFromTransport(
            bool isResponse, Stream readableStream,
            IQuasiHttpConnection connection)
        {
            if (readableStream == null)
            {
                throw new MissingDependencyException(
                    "no readable stream found for transport");
            }
            var headersReceiver = new Dictionary<string, IList<string>>();
            var reqOrStatusLine = await QuasiHttpCodec.ReadQuasiHttpHeaders(
                readableStream,
                headersReceiver,
                connection.ProcessingOptions?.MaxHeadersSize ?? 0,
                connection.CancellationToken);

            if (reqOrStatusLine.Count < 4)
            {
                throw new QuasiHttpException(
                    $"invalid quasi http {(isResponse ? "status" : "request")} line",
                    QuasiHttpException.ReasonCodeProtocolViolation);
            }
            long contentLength;
            try
            {
                contentLength = MiscUtilsInternal.ParseInt48(
                    reqOrStatusLine[3]);
            }
            catch (Exception e)
            {
                throw new QuasiHttpException(
                    $"invalid quasi http {(isResponse ? "response" : "request")} content length",
                    QuasiHttpException.ReasonCodeProtocolViolation,
                    e);
            }
            Stream body = null;
            if (contentLength != 0)
            {
                if (contentLength > 0)
                {
                    body = TlvUtils.CreateContentLengthEnforcingStream(
                        readableStream, contentLength);
                }
                else
                {
                    body = TlvUtils.CreateTlvDecodingReadableStream(readableStream,
                        QuasiHttpCodec.TagForBody);
                }
            }
            if (isResponse)
            {
                var response = new DefaultQuasiHttpResponse();
                try
                {
                    response.StatusCode = MiscUtilsInternal.ParseInt32(
                        reqOrStatusLine[0]);
                }
                catch (Exception e)
                {
                    throw new QuasiHttpException(
                        "invalid quasi http response status code",
                        QuasiHttpException.ReasonCodeProtocolViolation,
                        e);
                }
                response.HttpStatusMessage = reqOrStatusLine[1];
                response.HttpVersion = reqOrStatusLine[2];
                response.Headers = headersReceiver;
                if (body != null)
                {
                    var bodySizeLimit = connection.ProcessingOptions?.
                        MaxResponseBodySize ?? 0;
                    if (bodySizeLimit >= 0)
                    {
                        body = TlvUtils.CreateMaxLengthEnforcingStream(body,
                            bodySizeLimit);
                    }
                }
                response.Body = body;
                return response;
            }
            else
            {
                var request = new DefaultQuasiHttpRequest
                {
                    Environment = connection.Environment
                };
                request.HttpMethod = reqOrStatusLine[0];
                request.Target = reqOrStatusLine[1];
                request.HttpVersion = reqOrStatusLine[2];
                request.Headers = headersReceiver;
                request.Body = body;
                return request;
            }
        }
    }
}
