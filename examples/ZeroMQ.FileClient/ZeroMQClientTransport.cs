using Kabomu.Common;
using Kabomu.Examples.Shared;
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

        public async Task<QuasiHttpSendResponse> ProcessSendRequest(
            object remoteEndpoint,
            IQuasiHttpRequest request,
            IQuasiHttpProcessingOptions sendOptions)
        {
            var resTask = ProcessSendRequestInternal(remoteEndpoint,
                request, sendOptions);
            return new QuasiHttpSendResponse
            {
                ResponseTask = resTask,
            };
        }

        public async Task<QuasiHttpSendResponse> ProcessSendRequest2(
            object remoteEndpoint,
            Func<IDictionary<string, object>, Task<IQuasiHttpRequest>> requestFunc,
            IQuasiHttpProcessingOptions sendOptions)
        {
            var request = await requestFunc.Invoke(null);
            if (request == null)
            {
                throw new QuasiHttpRequestProcessingException("no request");
            }
            var resTask = ProcessSendRequestInternal(remoteEndpoint,
                request, sendOptions);
            return new QuasiHttpSendResponse
            {
                ResponseTask = resTask
            };
        }

        public async Task CancelSendRequest(object sendCancellationHandle)
        {
            // do nothing
        }

        private async Task<IQuasiHttpResponse> ProcessSendRequestInternal(
            object remoteEndpoint,
            IQuasiHttpRequest request,
            IQuasiHttpProcessingOptions sendOptions)
        {
            var leadChunk = CustomChunkedTransferCodec.CreateFromRequest(request);
            var requestBody = request.Body;
            byte[] requestBodyBytes = null;
            if (requestBody != null)
            {
                requestBodyBytes = await IOUtils.ReadAllBytes(
                    request.Body.AsReader(), sendOptions.ResponseBodyBufferingSizeLimit);
            }
            var headerStream = new MemoryStream();
            await new CustomChunkedTransferCodec().WriteLeadChunk(headerStream, leadChunk);

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
                // give chance for other tasks to run, so as to avoid
                // spinning the CPU.
                await Task.Yield();
            }
        }
    }
}
