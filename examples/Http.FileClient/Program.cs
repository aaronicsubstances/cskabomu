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
            [Option('s', "server-port", Required = false,
                HelpText = "Server Host and Port. Defaults to localhost:5001")]
            public string ServerAuthority { get; set; }

            [Option('d', "upload-dir", Required = false,
                HelpText = "Path to directory of files to upload. Defaults to current directory")]
            public string UploadDirPath { get; set; }
        }

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                   .WithParsed<Options>(o =>
                   {
                       RunMain(o.ServerAuthority ?? "localhost:5001",
                           o.UploadDirPath ?? ".").Wait();
                   });
        }

        static async Task RunMain(string serverAuthority, string uploadDirPath)
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
                LOG.Info("Started Http.FileClient to {0}", serverAuthority);

                await FileSender.StartTransferringFiles(instance, serverAuthority, uploadDirPath);
            }
            catch (Exception e)
            {
                LOG.Error(e, "Fatal error encountered");
            }
            finally
            {
                LOG.Debug("Stopping Http.FileClient...");
                await instance.Reset(null);
            }
        }
    }
}
