using Kabomu.Common;
using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.ChunkedTransfer;
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

        public void CancelSendRequest(object sendCancellationHandle)
        {

        }

        public (Task<IQuasiHttpResponse>, object) ProcessSendRequest(
            object remoteEndpoint,
            IQuasiHttpRequest request,
            IQuasiHttpSendOptions sendOptions)
        {
            var task = ProcessSendRequestInternal(
                remoteEndpoint,
                _ => Task.FromResult(request),
                sendOptions);
            return (task, null);
        }

        public (Task<IQuasiHttpResponse>, object) ProcessSendRequest(
            object remoteEndpoint,
            Func<IDictionary<string, object>, Task<IQuasiHttpRequest>> requestFunc,
            IQuasiHttpSendOptions sendOptions)
        {
            var task = ProcessSendRequestInternal(remoteEndpoint, requestFunc, sendOptions);
            return (task, null);
        }

        private async Task<IQuasiHttpResponse> ProcessSendRequestInternal(
            object remoteEndpoint,
            Func<IDictionary<string, object>, Task<IQuasiHttpRequest>> requestFunc,
            IQuasiHttpSendOptions sendOptions)
        {
            var request = await requestFunc.Invoke(null);
            // todo: ensure disposal of request if it was retrieved
            // from externally supplied request func.
            var leadChunk = LeadChunk.CreateFromRequest(request);
            var requestBody = request.Body;
            byte[] requestBodyBytes = null;
            if (requestBody != null)
            {
                requestBodyBytes = await IOUtils.ReadAllBytes(
                    request.Body.AsReader());
            }
            var headerStream = new MemoryStream();
            var writer = new StreamCustomReaderWriter(headerStream);
            await ChunkedTransferUtils.WriteLeadChunk(writer, leadChunk);

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
