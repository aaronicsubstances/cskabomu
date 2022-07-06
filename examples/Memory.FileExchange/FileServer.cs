using Kabomu.Common;
using Kabomu.Concurrency;
using Kabomu.Examples.Shared;
using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.Transport;
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

        public static async Task RunMain(string endpoint, string uploadDirPath,
            IMemoryBasedTransportHub hub)
        {
            var eventLoop = new DefaultEventLoopApi();
            var transport = new MemoryBasedServerTransport
            {
                LocalEndpoint = endpoint
            };
            UncaughtErrorCallback errorHandler = (e, m) =>
            {
                LOG.Error("Quasi Http Server error! {0}: {1}", m, e);
            };
            var instance = new DefaultQuasiHttpServer
            {
                OverallReqRespTimeoutMillis = 5_000,
                Transport = transport,
                EventLoop = eventLoop,
                ErrorHandler = errorHandler,
            };
            instance.Application = new FileReceiver(endpoint, uploadDirPath);
            transport.Application = instance.Application;

            await hub.AddServer(transport);

            await instance.Start();
            LOG.Info("Started Memory.FileServer at {0}", endpoint);
        }
    }
}
