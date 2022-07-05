using Kabomu.Common;
using Kabomu.Concurrency;
using Kabomu.Examples.Shared;
using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.Transport;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Memory.FileExchange
{
    public class FileClient
    {
        static readonly Logger LOG = LogManager.GetCurrentClassLogger();

        public static async Task RunMain(string clientEndpoint, string serverEndpoint,
            string uploadDirPath, IMemoryBasedTransportHub hub, double directSendProb)
        {
            var eventLoop = new DefaultEventLoopApi();
            var transport = new MemoryBasedClientTransport
            {
                LocalEndpoint = clientEndpoint,
                DirectSendRequestProcessingProbability = directSendProb,
                Hub = hub
            };
            var defaultSendOptions = new DefaultQuasiHttpSendOptions
            {
                OverallReqRespTimeoutMillis = 5_000
            };
            var instance = new DefaultQuasiHttpClient
            {
                DefaultSendOptions = defaultSendOptions,
                EventLoop = eventLoop,
                Transport = transport
            };

            try
            {
                LOG.Info("Started Memory.FileClient to {0}", serverEndpoint);

                await FileSender.StartTransferringFiles(instance, serverEndpoint, uploadDirPath);
                LOG.Debug("Completed Memory.FileClient.");
            }
            catch (Exception e)
            {
                LOG.Error(e, "Fatal error encountered");
            }
        }
    }
}
