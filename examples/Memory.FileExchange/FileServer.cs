using Kabomu.Common;
using Kabomu.Examples.Shared;
using Kabomu.QuasiHttp;
using NLog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Memory.FileExchange
{
    public class FileServer
    {
        static readonly Logger LOG = LogManager.GetCurrentClassLogger();

        public static Task RunMain(string endpoint, string uploadDirPath,
            MemoryBasedTransportHub hub)
        {
            var eventLoop = new DefaultEventLoopApi
            {
                ErrorHandler = (e, m) =>
                {
                    LOG.Error("Event Loop error! {0}: {1}", m, e);
                }
            };
            var memTransport = new MemoryBasedTransport
            {
                MaxChunkSize = 512,
                Mutex = eventLoop,
                ErrorHandler = (e, m) =>
                {
                    LOG.Error("Transport error! {0}: {1}", m, e);
                }
            };
            var instance = new KabomuQuasiHttpClient
            {
                DefaultTimeoutMillis = 5_000,
                EventLoop = eventLoop,
                ErrorHandler = (e, m) =>
                {
                    LOG.Error("Quasi Http Server error! {0}: {1}", m, e);
                }
            };
            hub.Clients.Add(endpoint, instance);
            memTransport.Hub = hub;
            instance.Transport = memTransport;

            instance.Application = new FileReceiver(endpoint, uploadDirPath, eventLoop);

            LOG.Info("Started Memory.FileServer at {0}", endpoint);
            return Task.CompletedTask;
        }
    }
}
