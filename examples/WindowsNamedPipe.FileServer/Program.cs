using CommandLine;
using Kabomu.Common;
using Kabomu.Examples.Shared;
using Kabomu.QuasiHttp.Server;
using NLog;
using System;
using System.Threading.Tasks;

namespace WindowsNamedPipe.FileServer
{
    class Program
    {
        static readonly Logger LOG = LogManager.GetCurrentClassLogger();

        public class Options
        {
            [Option('p', "path", Required = false,
                HelpText = "Server Path. Defaults to 34dc4fb1-71e0-4682-a64f-52d2635df2f5")]
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
                       RunMain(o.Path ?? "34dc4fb1-71e0-4682-a64f-52d2635df2f5", o.UploadDirPath ?? ".").Wait();
                   });
        }

        static async Task RunMain(string path, string uploadDirPath)
        {
            var transport = new WindowsNamedPipeServerTransport(path);
            UncaughtErrorCallback errorHandler = (e, m) =>
            {
                LOG.Error("Quasi Http Server error! {0}: {1}", m, e);
            };
            var instance = new DefaultQuasiHttpServer
            {
                DefaultProcessingOptions = new DefaultQuasiHttpProcessingOptions
                {
                    TimeoutMillis = 5_000
                },
                Transport = transport,
                ErrorHandler = errorHandler
            };
            instance.Application = new FileReceiver(path, uploadDirPath);

            try
            {
                await instance.Start();
                LOG.Info("Started WindowsNamedPipe.FileServer at {0}", path);

                Console.ReadLine();
            }
            catch (Exception e)
            {
                LOG.Error(e, "Fatal error encountered");
            }
            finally
            {
                LOG.Debug("Stopping WindowsNamedPipe.FileServer...");
                await instance.Stop(0);
            }
        }
    }
}
