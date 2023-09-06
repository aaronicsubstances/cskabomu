using Kabomu.Common;
using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.ChunkedTransfer;
using Kabomu.QuasiHttp.EntityBody;
using NetMQ;
using NetMQ.Sockets;
using NLog;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ZeroMQ.FileServer
{
    public class ZeroMQServerTransport
    {
        private static readonly Logger LOG = LogManager.GetCurrentClassLogger();

        private readonly NetMQSocket _socket;

        public ZeroMQServerTransport(NetMQSocket socket)
        {
            _socket = socket;
        }

        public StandardQuasiHttpServer Server { get; set; }

        public async Task AcceptRequests()
        {
            try
            {
                await AcceptRequestsInternal();
            }
            catch (Exception e)
            {
                if (e is TaskCanceledException)
                {
                    LOG.Info("request accept ended");
                }
                else
                {
                    LOG.Warn(e, "request accept error");
                }
            }
        }

        private async Task AcceptRequestsInternal()
        {
            while (true)
            {
                var (data, more) = await _socket.ReceiveFrameBytesAsync();
                var headerReader = new MemoryStream(data);
                var leadChunk = await new CustomChunkedTransferCodec().ReadLeadChunk(headerReader);
                var request = new DefaultQuasiHttpRequest();
                CustomChunkedTransferCodec.UpdateRequest(request, leadChunk);
                if (more)
                {
                    (data, more) = await _socket.ReceiveFrameBytesAsync();
                    if (more)
                    {
                        throw new Exception("more frames not expected here");
                    }
                    request.Body = new ByteBufferBody(data)
                    {
                        ContentLength = leadChunk.ContentLength
                    };
                }
                // don't wait
                _ = ProcessRequest(request);
            }
        }

        private async Task ProcessRequest(IQuasiHttpRequest request)
        {
            try
            {
                await Server.AcceptRequest(request, null);
            }
            catch (Exception ex)
            {
                LOG.Warn(ex, "request processing error");
            }
        }
    }
}