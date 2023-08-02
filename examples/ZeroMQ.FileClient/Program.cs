using CommandLine;
using Kabomu.Examples.Shared;
using Kabomu.QuasiHttp.Client;
using NetMQ.Sockets;
using NLog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ZeroMQ.FileClient
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
            try
            {
                using (var publisher = new PublisherSocket())
                {
                    publisher.Bind("tcp://*:" + serverPort);
                    LOG.Info("Created ZeroMQ.FileClient to {0}", serverPort);
                    var transport = new ZeroMQClientTransport(publisher);
                    var defaultSendOptions = new DefaultQuasiHttpSendOptions
                    {
                        EnsureNonNullResponse = false
                    };
                    var instance = new StandardQuasiHttpClient
                    {
                        DefaultSendOptions = defaultSendOptions,
                        TransportBypass = transport
                    };
                    // give time for subscriber to subscribe.
                    Console.WriteLine("Hit ENTER when subscriber is ready...");
                    Console.ReadLine();

                    await FileSender.StartTransferringFiles(instance, serverPort, uploadDirPath);
                    LOG.Debug("Stopping ZeroMQ.FileClient...");
                }
            }
            catch (Exception e)
            {
                LOG.Error(e, "Fatal error encountered");
            }
        }
    }
}
