using CommandLine;
using Kabomu.Examples.Shared;
using Kabomu.QuasiHttp.Client;
using Kabomu.QuasiHttp.Transport;
using NetMQ;
using NetMQ.Sockets;
using NLog;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
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

            [Option('b', "use-publish-pattern", Required = false, Default = false,
                HelpText = "Uses publish pattern instead of pipeline pattern. Defaults to false.")]
            public bool UsePublishPattern { get; set; }
        }

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                   .WithParsed<Options>(o =>
                   {
                       RunMain(o.ServerPort ?? 5001,
                           o.UploadDirPath ?? ".", o.UsePublishPattern).Wait();
                   });
        }

        static async Task RunMain(int serverPort, string uploadDirPath,
            bool usePublishPattern)
        {
            try
            {
                using (var socket = await CreateClientSocket(serverPort,
                    usePublishPattern))
                {
                    LOG.Info("Created ZeroMQ.FileClient to {0}", serverPort);
                    var transport = new ZeroMQClientTransport(socket);
                    var defaultSendOptions = new DefaultQuasiHttpSendOptions
                    {
                        TimeoutMillis = 5000,
                        EnsureNonNullResponse = false
                    };
                    var instance = new StandardQuasiHttpClient
                    {
                        DefaultSendOptions = defaultSendOptions,
                        TransportBypass = transport
                    };

                    await FileSender.StartTransferringFiles(instance, serverPort, uploadDirPath);
                    LOG.Debug("Stopping ZeroMQ.FileClient...");
                }
            }
            catch (Exception e)
            {
                LOG.Error(e, "Fatal error encountered");
            }
        }

        private static async Task<NetMQSocket> CreateClientSocket(int port,
            bool usePublishPattern)
        {
            if (usePublishPattern)
            {
                var socket = new PublisherSocket();
                socket.Bind("tcp://*:" + port);
                Console.WriteLine("Pausing for 2s for subscriber to subscribe...");
                await Task.Delay(2000);
                return socket;
            }
            else
            {
                var socket = new PushSocket();
                socket.Bind("tcp://*:" + port);
                return socket;
            }
        }
    }
}
