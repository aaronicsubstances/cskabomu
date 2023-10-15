using CommandLine;
using Kabomu;
using Kabomu.Abstractions;
using Kabomu.Examples.Shared;
using Kabomu.Tlv;
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

            [Option('d', "upload-dir", Required = false,
                HelpText = "Path to directory of files to upload. Defaults to current directory")]
            public string UploadDirPath { get; set; }
        }

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                   .WithParsed<Options>(o =>
                   {
                       RunMain(o.ServerPort ?? 5001,
                           o.UploadDirPath ?? ".").Wait();
                   });
        }

        static async Task RunMain(int serverPort, string uploadDirPath)
        {
            var transport = new LocalhostTcpClientTransport
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
                LOG.Info("Connecting Tcp.FileClient to {0}...", serverPort);

                await FileSender.StartTransferringFiles(instance, serverPort, uploadDirPath);
            }
            catch (Exception e)
            {
                LOG.Error(e, "Fatal error encountered");
            }
        }
    }
}
