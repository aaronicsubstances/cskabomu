using CommandLine;
using Kabomu.Common;
using Kabomu.Examples.Shared;
using Kabomu.QuasiHttp;
using NLog;
using System;
using System.Threading.Tasks;

namespace Udp.FileServer
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
            var eventLoop = new DefaultEventLoopApi
            {
                ErrorHandler = (e, m) =>
                {
                    LOG.Error("Event Loop error! {0}: {1}", m, e);
                }
            };
            var udpTransport = new LocalhostUdpTransport(port)
            {
                EventLoop = eventLoop,
                ErrorHandler = (e, m) =>
                {
                    LOG.Error("Transport error! {0}: {1}", m, e);
                }
            };
            var instance = new KabomuQuasiHttpClient
            {
                DefaultTimeoutMillis = 5_000,
                EventLoop = eventLoop,
                ErrorHandler = (e, m) =>
                {
                    LOG.Error("Quasi Http Server error! {0}: {1}", m, e);
                }
            };
            udpTransport.Upstream = instance;
            instance.Transport = udpTransport;

            instance.Application = new FileReceiver(port, uploadDirPath, eventLoop);

            try
            {
                udpTransport.Start();
                LOG.Info("Started Udp.FileServer at {0}", port);

                Console.ReadLine();
            }
            catch (Exception e)
            {
                LOG.Error(e, "Fatal error encountered");
            }
            finally
            {
                LOG.Debug("Stopping Udp.FileServer...");
                udpTransport.Stop();
            }
        }
    }
}
