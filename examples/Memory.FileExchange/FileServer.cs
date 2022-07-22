using Kabomu.Common;
using Kabomu.Examples.Shared;
using Kabomu.QuasiHttp.Server;
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

        private static IQuasiHttpServer _instance;

        public static async Task RunMain(string endpoint, string uploadDirPath,
            IMemoryBasedTransportHub hub)
        {
            var transport = new MemoryBasedServerTransport();
            UncaughtErrorCallback errorHandler = (e, m) =>
            {
                LOG.Error("Quasi Http Server error! {0}: {1}", m, e);
            };
            _instance = new DefaultQuasiHttpServer
            {
                DefaultProcessingOptions = new DefaultQuasiHttpProcessingOptions
                {
                    TimeoutMillis = 5_000
                },
                Transport = transport,
                ErrorHandler = errorHandler,
            };
            _instance.Application = new FileReceiver(endpoint, uploadDirPath);

            await hub.AddServer(endpoint, _instance);

            await _instance.Start();
            LOG.Info("Started Memory.FileServer at {0}", endpoint);
        }

        public static async Task EndMain()
        {
            if (_instance != null)
            {
                LOG.Debug("Stopping Memory.FileServer...");
                await _instance.Stop(0);
            }
        }
    }
}
