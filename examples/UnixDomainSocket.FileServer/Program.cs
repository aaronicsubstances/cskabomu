using CommandLine;
using Kabomu;
using Kabomu.Abstractions;
using Kabomu.Examples.Shared;
using Kabomu.ProtocolImpl;
using NLog;
using System;
using System.IO;
using System.Threading.Tasks;

namespace UnixDomainSocket.FileServer
{
    class Program
    {
        static readonly Logger LOG = LogManager.GetCurrentClassLogger();

        public class Options
        {
            [Option('p', "path", Required = false,
                HelpText = "Server Path. Defaults to 380d562f-554d-4b19-88ff-d92356a62b5f.sock " +
                    "in the current user's temp directory")]
            public string Path { get; set; }

            [Option('d', "upload-dir", Required = false,
                HelpText = "Path to directory for saving uploaded files. Defaults to current directory")]
            public string UploadDirPath { get; set; }
        }

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                   .WithParsed<Options>(o =>
                   {
                       RunMain(
                           o.Path ?? Path.Combine(Path.GetTempPath(),
                                "380d562f-554d-4b19-88ff-d92356a62b5f.sock"),
                           o.UploadDirPath ?? ".").Wait();
                   });
        }

        static async Task RunMain(string path, string uploadDirPath)
        {
            var instance = new StandardQuasiHttpServer
            {
                Application = new FileReceiver(path, uploadDirPath).ProcessRequest
            };
            var transport = new UnixDomainSocketServerTransport(path)
            {
                Server = instance,
                DefaultProcessingOptions = new DefaultQuasiHttpProcessingOptions
                {
                    TimeoutMillis = 5_000
                }
            };
            instance.Transport = transport;

            try
            {
                await transport.Start();
                LOG.Info("Started UnixDomainSocket.FileServer at {0}", path);

                Console.ReadLine();
            }
            catch (Exception e)
            {
                LOG.Error(e, "Fatal error encountered");
            }
            finally
            {
                LOG.Debug("Stopping UnixDomainSocket.FileServer...");
                await transport.Stop();
            }
        }
    }
}
