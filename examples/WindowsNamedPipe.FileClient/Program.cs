using CommandLine;
using Kabomu.Common;
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
            [Option('p', "path", Required = false,
                HelpText = "Client Path. Defaults to 8e562101-d3cd-49a0-9d47-020a3ff97aa3")]
            public string Path { get; set; }
            [Option('d', "upload-dir", Required = false,
                HelpText = "Path to directory of files to upload. Defaults to current directory")]
            public string UploadDirPath { get; set; }
        }

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                   .WithParsed<Options>(o =>
                   {
                       RunMain(o.Path ?? "8e562101-d3cd-49a0-9d47-020a3ff97aa3", 
                           o.ServerPath ?? "34dc4fb1-71e0-4682-a64f-52d2635df2f5",
                           o.UploadDirPath ?? ".").Wait();
                   });
        }

        static async Task RunMain(string path, string serverPath, string uploadDirPath)
        {
            var eventLoop = new DefaultEventLoopApi();
            var transport = new WindowsNamedPipeTransport(path);
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
                LOG.Info("Started WindowsNamedPipe.FileClient at {0}", path);

                await FileSender.StartTransferringFiles(instance, serverPath, uploadDirPath);
            }
            catch (Exception e)
            {
                LOG.Error(e, "Fatal error encountered");
            }
            finally
            {
                LOG.Debug("Stopping WindowsNamedPipe.FileClient...");
                await instance.Reset(null);
            }
        }
    }
}
