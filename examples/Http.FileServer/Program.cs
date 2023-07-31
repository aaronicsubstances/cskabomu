using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Kabomu.Examples.Shared;
using Kabomu.QuasiHttp.Server;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NLog;

namespace Http.FileServer
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
            // adapted from https://github.com/PeteX/StandaloneKestrel

            var serverOptions = new KestrelServerOptions();
            serverOptions.ListenLocalhost(port);

            var transportOptions = new SocketTransportOptions();
            var loggerFactory = new NullLoggerFactory();

            var transportFactory = new SocketTransportFactory(
                new OptionsWrapper<SocketTransportOptions>(transportOptions), loggerFactory);

            var instance = new StandardQuasiHttpServer
            {
                DefaultProcessingOptions = new DefaultQuasiHttpProcessingOptions
                {
                    TimeoutMillis = 5_000
                }
            };
            var serverUrl = new Uri($"http://127.0.0.1:{port}");
            instance.Application = FileReceiver.Create(serverUrl, uploadDirPath);
            using (var server = new KestrelServer(new OptionsWrapper<KestrelServerOptions>(serverOptions),
                transportFactory, loggerFactory))
            {
                await server.StartAsync(new DemoHttpMiddleware(instance), CancellationToken.None);
                LOG.Info("Started Http.FileServer at {0}", serverUrl);

                Console.ReadLine();
                LOG.Debug("Stopping Http.FileServer...");
            }
        }
    }
}
