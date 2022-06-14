using Kabomu.Common;
using Kabomu.Examples.Shared;
using Kabomu.QuasiHttp;
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

        public static async Task RunMain(string endpoint, string serverEndpoint,
            string uploadDirPath, MemoryBasedTransportHub hub, double directSendProb)
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
                DirectSendRequestProcessingProbability = directSendProb,
                Mutex = eventLoop,
                ErrorHandler = (e, m) =>
                {
                    LOG.Warn(e, "Memory-based transport error: {0}", m);
                }
            };
            var instance = new KabomuQuasiHttpClient
            {
                DefaultTimeoutMillis = 5_000,
                EventLoop = eventLoop,
                ErrorHandler = (e, m) =>
                {
                    LOG.Error("Quasi Http Client error! {0}: {1}", m, e);
                }
            };
            hub.Clients.Add(endpoint, instance);
            memTransport.Hub = hub;
            instance.Transport = memTransport;

            try
            {
                LOG.Info("Started Memory.FileClient at {0}", endpoint);

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
