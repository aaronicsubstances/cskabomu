using CommandLine;
using Kabomu.Examples.Shared;
using Kabomu.QuasiHttp.Client;
using NLog;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Http.FileClient
{
    class Program
    {
        static readonly Logger LOG = LogManager.GetCurrentClassLogger();

        public class Options
        {
            [Option('s', "server-url", Required = false,
                HelpText = "Server base url. Defaults to http://localhost:5001")]
            public string ServerUrl { get; set; }

            [Option('d', "upload-dir", Required = false,
                HelpText = "Path to directory of files to upload. Defaults to current directory")]
            public string UploadDirPath { get; set; }
        }

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                   .WithParsed<Options>(o =>
                   {
                       RunMain(o.ServerUrl ?? "http://localhost:5001",
                           o.UploadDirPath ?? ".").Wait();
                   });
        }

        static async Task RunMain(string serverUrlStr, string uploadDirPath)
        {
            var httpClient = new HttpClient();
            var transport = new HttpBasedTransport(httpClient);
            var defaultSendOptions = new DefaultQuasiHttpSendOptions
            {
                TimeoutMillis = 5_000
            };
            var instance = new StandardQuasiHttpClient
            {
                DefaultSendOptions = defaultSendOptions,
                TransportBypass = transport
            };

            try
            {
                var serverUrl = new Uri(serverUrlStr);
                LOG.Info("Created Http.FileClient to {0}", serverUrl);

                await FileSender.StartTransferringFiles(instance, serverUrl, uploadDirPath);
            }
            catch (Exception e)
            {
                LOG.Error(e, "Fatal error encountered");
            }
        }
    }
}
