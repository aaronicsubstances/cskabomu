﻿using CommandLine;
using Kabomu.Examples.Shared;
using Kabomu.QuasiHttp.Client;
using NLog;
using System;
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
        }

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                   .WithParsed<Options>(o =>
                   {
                       RunMain(o.ClientUploadDirPath ?? ".",
                           o.ServerDownloadDirPath ?? ".").Wait();
                   });
        }

        public static async Task RunMain(string uploadDirPath, string downloadDirPath)
        {
            var clientEndpoint = "takoradi";
            var serverEndpoint = "kumasi";
            var transport = new CustomMemoryBasedTransport(clientEndpoint, downloadDirPath);
            var defaultSendOptions = new DefaultQuasiHttpSendOptions
            {
                //TimeoutMillis = 5_000
            };
            var instance = new StandardQuasiHttpClient
            {
                DefaultSendOptions = defaultSendOptions,
                TransportBypass = transport,
            };

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
