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
            [Option('p', "path", Required = false,
                HelpText = "Client Path. Defaults to 97771e99-7de1-4f29-81ce-c4aa223deb06.sock")]
            public string Path { get; set; }
            [Option('d', "upload-dir", Required = false,
                HelpText = "Path to directory of files to upload. Defaults to current directory")]
            public string UploadDirPath { get; set; }
        }

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                   .WithParsed<Options>(o =>
                   {
                       RunMain(o.Path ?? "97771e99-7de1-4f29-81ce-c4aa223deb06.sock",
                           o.ServerPath ?? "380d562f-554d-4b19-88ff-d92356a62b5f.sock",
                           o.UploadDirPath ?? ".").Wait();
                   });
        }

        static async Task RunMain(string path, string serverPath, string uploadDirPath)
        {
            var eventLoop = new DefaultEventLoopApi
            {
                ErrorHandler = (e, m) =>
                {
                    LOG.Error("Event Loop error! {0}: {1}", m, e);
                }
            };
            var tcpTransport = new UnixDomainSocketTransport(path)
            {
                ErrorHandler = (e, m) =>
                {
                    LOG.Warn(e, "UnixDomainSocket transport error: {0}", m);
                }
            };
            var instance = new KabomuQuasiHttpClient
            {
                DefaultTimeoutMillis = 5_000,
                EventLoop = eventLoop,
                ErrorHandler = (e, m) =>
                {
                    LOG.Error("Quasi Http Client error! {0}: {1}", m, e);
                }
            };
            tcpTransport.Upstream = instance;
            instance.Transport = tcpTransport;

            try
            {
                tcpTransport.Start();
                LOG.Info("Started UnixDomainSocket.FileClient at {0}", path);

                await FileSender.StartTransferringFiles(instance, serverPath, uploadDirPath);
            }
            catch (Exception e)
            {
                LOG.Error(e, "Fatal error encountered");
            }
            finally
            {
                LOG.Debug("Stopping UnixDomainSocket.FileClient...");
                tcpTransport.Stop();
            }
        }
    }
}
