using CommandLine;
using Kabomu.Common;
using Kabomu.Examples.Shared;
using Kabomu.QuasiHttp;
using NLog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tcp.FileClient
{
    class Program
    {
        static readonly Logger LOG = LogManager.GetCurrentClassLogger();

        public class Options
        {
            [Option('s', "server-port", Required = false,
                HelpText = "Server Port. Defaults to 5001")]
            public int? ServerPort { get; set; }
            [Option('p', "port", Required = false,
                HelpText = "Client Port. Defaults to 5002")]
            public int? Port { get; set; }
            [Option('d', "upload-dir", Required = false,
                HelpText = "Path to directory of files to upload. Defaults to current directory")]
            public string UploadDirPath { get; set; }
        }

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                   .WithParsed<Options>(o =>
                   {
                       RunMain(o.Port ?? 5002, o.ServerPort ?? 5001,
                           o.UploadDirPath ?? ".").Wait();
                   });
        }

        static async Task RunMain(int port, int serverPort, string uploadDirPath)
        {
            var eventLoop = new DefaultEventLoopApi();
            var transport = new LocalhostTcpTransport(port);
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
                LOG.Info("Started Tcp.FileClient at {0}", port);

                await FileSender.StartTransferringFiles(instance, serverPort, uploadDirPath);
            }
            catch (Exception e)
            {
                LOG.Error(e, "Fatal error encountered");
            }
            finally
            {
                LOG.Debug("Stopping Tcp.FileClient...");
                await instance.Reset(null);
            }
        }
    }
}
