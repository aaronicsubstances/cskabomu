using Kabomu.Examples.Shared;
using Kabomu.QuasiHttp.Client;
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

        private static IQuasiHttpClient _instance;

        public static async Task RunMain(string clientEndpoint, string serverEndpoint,
            string uploadDirPath, IMemoryBasedTransportHub hub, double directSendProbability)
        {
            var transport = new MemoryBasedClientTransport
            {
                LocalEndpoint = clientEndpoint,
                Hub = hub
            };
            var defaultSendOptions = new DefaultQuasiHttpSendOptions
            {
                TimeoutMillis = 5_000
            };
            _instance = new DefaultQuasiHttpClient
            {
                DefaultSendOptions = defaultSendOptions,
                Transport = transport,
                TransportBypass = transport,
                TransportBypassProbabilty = directSendProbability
            };

            LOG.Info("Started Memory.FileClient to {0}", serverEndpoint);

            await FileSender.StartTransferringFiles(_instance, serverEndpoint, uploadDirPath);
            LOG.Debug("Completed Memory.FileClient.");
        }

        public static async Task EndMain()
        {
            if (_instance != null)
            {
                LOG.Debug("Stopping Memory.FileClient...");
                await _instance.Reset(null);
            }
        }
    }
}
