using CommandLine;
using Kabomu.Examples.Shared;
using Kabomu.QuasiHttp;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace WindowsNamedPipe.FileClient
{
    class Program
    {
        static readonly Logger LOG = LogManager.GetCurrentClassLogger();

        public class Options
        {
            [Option('s', "server-path", Required = false,
                HelpText = "Server Path. Defaults to 34dc4fb1-71e0-4682-a64f-52d2635df2f5")]
            public string ServerPath { get; set; }

            [Option('d', "upload-dir", Required = false,
                HelpText = "Path to directory of files to upload. Defaults to current directory")]
            public string UploadDirPath { get; set; }
        }

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                   .WithParsed<Options>(o =>
                   {
                       RunMain(o.ServerPath ?? "34dc4fb1-71e0-4682-a64f-52d2635df2f5",
                           o.UploadDirPath ?? ".").Wait();
                   });
        }

        static async Task RunMain(string serverPath, string uploadDirPath)
        {
            var transport = new WindowsNamedPipeClientTransport
            {
                DefaultSendOptions = new DefaultQuasiHttpProcessingOptions
                {
                    TimeoutMillis = 5_000
                }
            };
            var instance = new StandardQuasiHttpClient
            {
                Transport = transport
            };

            try
            {
                LOG.Info("Created WindowsNamedPipe.FileClient to {0}", serverPath);

                await FileSender.StartTransferringFiles(instance, serverPath, uploadDirPath);
            }
            catch (Exception e)
            {
                LOG.Error(e, "Fatal error encountered");
            }
        }
    }
}
