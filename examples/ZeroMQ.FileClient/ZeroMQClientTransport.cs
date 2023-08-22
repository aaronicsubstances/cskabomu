using Kabomu.Common;
using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.ChunkedTransfer;
using Kabomu.QuasiHttp.Client;
using Kabomu.QuasiHttp.EntityBody;
using Kabomu.QuasiHttp.Transport;
using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZeroMQ.FileClient
{
    public class ZeroMQClientTransport : IQuasiHttpAltTransport
    {
        private readonly NetMQSocket _socket;

        public ZeroMQClientTransport(NetMQSocket socket)
        {
            _socket = socket;
        }

        public QuasiHttpSendResponse ProcessSendRequest(
            object remoteEndpoint,
            IQuasiHttpRequest request,
            IQuasiHttpSendOptions sendOptions)
        {
            var resTask = ProcessSendRequestInternal(remoteEndpoint,
                request, sendOptions);
            return new QuasiHttpSendResponse
            {
                ResponseTask = resTask,
            };
        }

        public QuasiHttpSendResponse ProcessSendRequest(
            object remoteEndpoint,
            Func<IDictionary<string, object>, Task<IQuasiHttpRequest>> requestFunc,
            IQuasiHttpSendOptions sendOptions)
        {
            var resTask = ProcessSendRequestInternal(remoteEndpoint,
                requestFunc, sendOptions);
            return new QuasiHttpSendResponse
            {
                ResponseTask = resTask,
            };
        }

        public void CancelSendRequest(object sendCancellationHandle)
        {
            // do nothing.
        }

        private async Task<IQuasiHttpResponse> ProcessSendRequestInternal(
            object remoteEndpoint,
            object requestOrRequestFunc,
            IQuasiHttpSendOptions sendOptions)
        {
            IQuasiHttpRequest request;
            if (requestOrRequestFunc is IQuasiHttpRequest r)
            {
                request = r;
            }
            else
            {
                var requestFunc =
                    (Func<IDictionary<string, object>, Task<IQuasiHttpRequest>>)requestOrRequestFunc;
                request = await requestFunc.Invoke(null);
            }
            if (request == null)
            {
                throw new QuasiHttpRequestProcessingException("no request");
            }
            // todo: ensure disposal of request if it was retrieved
            // from externally supplied request func.
            var leadChunk = ChunkedTransferCodec.CreateFromRequest(request);
            var requestBody = request.Body;
            byte[] requestBodyBytes = null;
            if (requestBody != null)
            {
                requestBodyBytes = await IOUtils.ReadAllBytes(
                    request.Body.AsReader(), sendOptions.ResponseBodyBufferingSizeLimit);
            }
            var headerStream = new MemoryStream();
            await new ChunkedTransferCodec().WriteLeadChunk(headerStream, leadChunk);

            if (requestBodyBytes == null)
            {
                await SendFrame(headerStream.ToArray(), false);
            }
            else
            {
                await SendFrame(headerStream.ToArray(), true);
                await SendFrame(requestBodyBytes, false);
            }
            return null;
        }

        /// <summary>
        /// Push-type sockets block main thread if sending without attached
        /// pull-type socket. Hence the need for testing for blocking.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="more"></param>
        /// <returns></returns>
        private async Task SendFrame(byte[] data, bool more)
        {
            while (true)
            {
                if (_socket.TrySendFrame(TimeSpan.FromSeconds(1), data, more))
                {
                    break;
                }
                // give chance for StandardQuasiHttpClient's timeout logic to kick in.
                await Task.Yield();
            }
        }
    }
}
