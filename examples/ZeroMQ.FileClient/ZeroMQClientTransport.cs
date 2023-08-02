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
        private readonly PublisherSocket _publisher;

        public ZeroMQClientTransport(PublisherSocket publisher)
        {
            _publisher = publisher;
        }

        public void CancelSendRequest(object sendCancellationHandle)
        {

        }

        public (Task<IQuasiHttpResponse>, object) ProcessSendRequest(
            IQuasiHttpRequest request, IConnectivityParams connectivityParams)
        {
            var task = ProcessSendRequestInternal(_ => Task.FromResult(request),
                connectivityParams);
            return (task, null);
        }

        public (Task<IQuasiHttpResponse>, object) ProcessSendRequest(
            Func<IDictionary<string, object>, Task<IQuasiHttpRequest>> requestFunc,
            IConnectivityParams connectivityParams)
        {
            var task = ProcessSendRequestInternal(requestFunc, connectivityParams);
            return (task, null);
        }

        private async Task<IQuasiHttpResponse> ProcessSendRequestInternal(
            Func<IDictionary<string, object>, Task<IQuasiHttpRequest>> requestFunc,
            IConnectivityParams connectivityParams)
        {
            var request = await requestFunc.Invoke(null);
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
            await ChunkedTransferUtils.WriteLeadChunk(writer, 0, leadChunk);

            if (requestBodyBytes == null)
            {
                _publisher.SendFrame(headerStream.ToArray());
            }
            else
            {
                _publisher.SendMoreFrame(headerStream.ToArray());
                _publisher.SendFrame(requestBodyBytes);
            }
            return null;
        }
    }
}
