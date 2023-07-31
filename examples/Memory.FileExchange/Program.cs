using CommandLine;
using Kabomu.Examples.Shared;
using Kabomu.QuasiHttp.Client;
using Kabomu.QuasiHttp.Server;
using NLog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Memory.FileExchange
{
    class Program
    {
        static readonly Logger LOG = LogManager.GetCurrentClassLogger();

        public class Options
        {
            [Option('d', "client-upload-dir", Required = false,
                HelpText = "Path to directory of files to upload. Defaults to current directory")]
            public string ClientUploadDirPath { get; set; }

            [Option('s', "server-download-dir", Required = false,
                HelpText = "Path to directory for saving uploaded files. Defaults to current directory")]
            public string ServerDownloadDirPath { get; set; }

            [Option('b', "use-transport-bypass", Required = false, Default = false,
                HelpText = "Uses transport bypass instead of server/client transports. Defaults to false.")]
            public bool UseTransportBypass { get; set; }
        }

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                   .WithParsed<Options>(o =>
                   {
                       RunMain(o.ClientUploadDirPath ?? ".",
                           o.ServerDownloadDirPath ?? ".",
                           o.UseTransportBypass).Wait();
                   });
        }

        public static async Task RunMain(string uploadDirPath, string downloadDirPath,
            bool useTransportBypass)
        {
            var clientEndpoint = "takoradi";
            var serverEndpoint = "kumasi";
            var instance = new StandardQuasiHttpClient
            {
                DefaultSendOptions = new DefaultQuasiHttpSendOptions
                {
                    TimeoutMillis = 5_000
                }
            };
            var application = FileReceiver.Create(clientEndpoint, downloadDirPath);
            if (useTransportBypass)
            {
                instance.TransportBypass = new MemoryBasedAltTransport
                {
                    Application = application
                };
            }
            else
            {
                var serverInstance = new StandardQuasiHttpServer
                {
                    Application = application
                };
                var serverTransport = new MemoryBasedServerTransport
                {
                    Server = serverInstance
                };
                serverInstance.Transport = serverTransport;
                var clientTransport = new MemoryBasedClientTransport
                {
                    Servers = new Dictionary<object, MemoryBasedServerTransport>
                    {
                        { serverEndpoint, serverTransport }
                    }
                };
                instance.Transport = clientTransport;
            }

            try
            {
                LOG.Info("Started Memory.FileClient to {0}", serverEndpoint);

                await FileSender.StartTransferringFiles(instance, serverEndpoint, uploadDirPath);
            }
            catch (Exception e)
            {
                LOG.Error(e, "Fatal error encounstered");
            }
        }
    }
}
