﻿using CommandLine;
using Kabomu.Examples.Shared;
using Kabomu.QuasiHttp.Server;
using NetMQ;
using NetMQ.Sockets;
using NLog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ZeroMQ.FileServer
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

            var cancellationTokenSource = new CancellationTokenSource();
            var zeroMQTask = Task.Run(() =>
            {
                try
                {
                    RunZeroMQ(cancellationTokenSource, instance, port);
                }
                catch (Exception e)
                {
                    LOG.Error(e, "Fatal error encountered");
                }
            });
            Console.ReadLine();
            LOG.Debug("Stopping ZeroMQ.FileServer...");
            cancellationTokenSource.Cancel();
            await zeroMQTask;
        }

        private static void RunZeroMQ(CancellationTokenSource cancellationTokenSource,
            StandardQuasiHttpServer instance, int port)
        {
            using (var runtime = new NetMQRuntime())
            {
                using (var subscriber = new SubscriberSocket())
                {
                    subscriber.Connect("tcp://127.0.0.1:" + port);
                    subscriber.Subscribe("");
                    LOG.Info("Started ZeroMQ.FileServer at {0}", port);
                    var transport = new ZeroMQServerTransport(subscriber)
                    {
                        Server = instance
                    };
                    runtime.Run(cancellationTokenSource.Token,
                        transport.AcceptRequests());
                }
            }
        }
    }
}