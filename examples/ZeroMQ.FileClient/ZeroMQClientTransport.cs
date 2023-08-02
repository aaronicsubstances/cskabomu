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
            var leadChunk = new LeadChunk
            {
                Version = LeadChunk.Version01,
                Headers = request.Headers,
                HttpVersion = request.HttpVersion,
                Method = request.Method,
                RequestTarget = request.Target
            };
            var requestBody = request.Body;
            byte[] requestBodyBytes = null;
            if (requestBody != null)
            {
                requestBodyBytes = await IOUtils.ReadAllBytes(
                    request.Body.AsReader());
                leadChunk.ContentLength = requestBodyBytes.Length;
                leadChunk.ContentType = requestBody.ContentType;
            }
            var headerStream = new MemoryStream();
            var writer = new StreamCustomReaderWriter(headerStream);
            await ChunkedTransferUtils.WriteLeadChunk(writer, leadChunk);

            if (requestBodyBytes == null)
            {
                _socket.SendFrame(headerStream.ToArray());
            }
            else
            {
                _socket.SendMoreFrame(headerStream.ToArray());
                _socket.SendFrame(requestBodyBytes);
            }
            return null;
        }
    }
}
