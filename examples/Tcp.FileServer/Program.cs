﻿using CommandLine;
using Kabomu.Examples.Shared;
using Kabomu.QuasiHttp.Server;
using NLog;
using System;
using System.Threading.Tasks;

namespace Tcp.FileServer
{
    class Program
    {
        static readonly Logger LOG = LogManager.GetCurrentClassLogger();

        public class Options
        {
            [Option('p', "port", Required = false,
                HelpText = "Server Port. Defaults to 5001")]
            public int? Port { get; set; }

            [Option('d', "upload-dir", Required = false,
                HelpText = "Path to directory for saving uploaded files. Defaults to current directory")]
            public string UploadDirPath { get; set; }
        }

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                   .WithParsed<Options>(o =>
                   {
                       RunMain(o.Port ?? 5001, o.UploadDirPath ?? ".").Wait();
                   });
        }

        static async Task RunMain(int port, string uploadDirPath)
        {
            var instance = new StandardQuasiHttpServer
            {
                DefaultProcessingOptions = new DefaultQuasiHttpProcessingOptions
                {
                    TimeoutMillis = 5_000
                }
            };
            instance.Application = FileReceiver.Create(port, uploadDirPath);
            var transport = new LocalhostTcpServerTransport(port)
            {
                Server = instance
            };
            instance.Transport = transport;

            try
            {
                await transport.Start();
                LOG.Info("Started Tcp.FileServer at {0}", port);

                Console.ReadLine();
            }
            catch (Exception e)
            {
                LOG.Error(e, "Fatal error encountered");
            }
            finally
            {
                LOG.Debug("Stopping Tcp.FileServer...");
                await transport.Stop();
            }
        }
    }
}
