using Kabomu.Common;
using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.ChunkedTransfer;
using Kabomu.QuasiHttp.EntityBody;
using Kabomu.QuasiHttp.Server;
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

        private readonly SubscriberSocket _subscriber;

        public ZeroMQServerTransport(SubscriberSocket subscriber)
        {
            _subscriber = subscriber;
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
                var (data, more) = await _subscriber.ReceiveFrameBytesAsync();
                var headerReader = new StreamCustomReaderWriter(new MemoryStream(data));
                var leadChunk = await ChunkedTransferUtils.ReadLeadChunk(headerReader, 0);
                var request = new DefaultQuasiHttpRequest
                {
                    Target = leadChunk.RequestTarget,
                    Method = leadChunk.Method,
                    Headers = leadChunk.Headers,
                    HttpVersion = leadChunk.HttpVersion
                };
                if (more)
                {
                    (data, more) = await _subscriber.ReceiveFrameBytesAsync();
                    if (more)
                    {
                        throw new Exception("more frames not expected here");
                    }
                    request.Body = new ByteBufferBody(data)
                    {
                        ContentLength = leadChunk.ContentLength,
                        ContentType = leadChunk.ContentType
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