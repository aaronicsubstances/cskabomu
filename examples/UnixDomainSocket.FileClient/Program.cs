using CommandLine;
using Kabomu.Common;
using Kabomu.Examples.Shared;
using Kabomu.QuasiHttp;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace UnixDomainSocket.FileClient
{
    class Program
    {
        static readonly Logger LOG = LogManager.GetCurrentClassLogger();

        public class Options
        {
            [Option('s', "server-path", Required = false,
                HelpText = "Server Path. Defaults to 380d562f-554d-4b19-88ff-d92356a62b5f.sock")]
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
                       RunMain(o.ServerPath ?? "380d562f-554d-4b19-88ff-d92356a62b5f.sock",
                           o.UploadDirPath ?? ".").Wait();
                   });
        }

        static async Task RunMain(string serverPath, string uploadDirPath)
        {
            var eventLoop = new DefaultEventLoopApi();
            var transport = new UnixDomainSocketClientTransport();
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
                LOG.Info("Started UnixDomainSocket.FileClient to {0}", serverPath);

                await FileSender.StartTransferringFiles(instance, serverPath, uploadDirPath);
            }
            catch (Exception e)
            {
                LOG.Error(e, "Fatal error encountered");
            }
            finally
            {
                LOG.Debug("Stopping UnixDomainSocket.FileClient...");
                await instance.Reset(null);
            }
        }
    }
}
