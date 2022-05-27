using CommandLine;
using Kabomu.Common;
using Kabomu.Examples.Shared;
using Kabomu.QuasiHttp;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
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
            [Option('a', "alt", Required = false,
                HelpText = "Run alternative (currently means serving content lengths)")]
            public bool? ServeContentLength { get; set; }
        }

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                   .WithParsed<Options>(o =>
                   {
                       RunMain(o.Port ?? 5002, o.ServerPort ?? 5001, o.UploadDirPath ?? ".",
                           o.ServeContentLength ?? false).Wait();
                   });
        }

        static async Task RunMain(int port, int serverPort, string uploadDirPath, bool serveContentLength)
        {
            var eventLoop = new DefaultEventLoopApi
            {
                ErrorHandler = (e, m) =>
                {
                    LOG.Error("Event Loop error! {0}: {1}", m, e);
                }
            };
            var stopHandle = new CancellationTokenSource();
            var tcpTransport = new LocalhostTcpTransport(port)
            {
                ErrorHandler = (e, m) =>
                {
                    LOG.Warn(e, "TCP transport error: {0}", m);
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
                LOG.Info("Started Tcp.FileClient at {0}", port);

                await StartTransferringFiles(instance, serverPort, uploadDirPath, serveContentLength);
            }
            catch (Exception e)
            {
                LOG.Error(e, "Fatal error encountered");
            }
            finally
            {
                LOG.Debug("Stopping Tcp.FileClient...");
                tcpTransport.Stop();
            }
        }

        private static async Task StartTransferringFiles(KabomuQuasiHttpClient instance, int serverPort, string uploadDirPath,
            bool serveContentLength)
        {
            var directory = new DirectoryInfo(uploadDirPath);
            int count = 0;
            long bytesTransferred = 0L;
            DateTime startTime = DateTime.Now;
            var tasks = new List<Task>();
            foreach (var f in directory.GetFiles())
            {
                LOG.Debug("Transferring {0}", f.Name);
                //tasks.Add(TransferFile(instance, serverPort, f));
                await TransferFile(instance, serverPort, f, serveContentLength);
                LOG.Info("Successfully transferred {0}", f.Name);
                bytesTransferred += f.Length;
                count++;
            }
            Task.WaitAll(tasks.ToArray());
            double timeTaken = Math.Round((DateTime.Now - startTime).TotalSeconds, 2);
            double megaBytesTransferred = Math.Round(bytesTransferred / (1024.0 * 1024.0), 2);
            LOG.Info("Successfully transferred {0} bytes ({1} MB) worth of data in {2} files in {3} seconds",
                bytesTransferred, megaBytesTransferred, count, timeTaken);
        }
        private static Task TransferFile(KabomuQuasiHttpClient instance, int serverPort, FileInfo f, bool serveContentLength)
        {
            var request = new DefaultQuasiHttpRequestMessage
            {
                Headers = new Dictionary<string, List<string>>(),
                Body = new FileBody(f.DirectoryName, f.Name, serveContentLength)
            };
            request.Headers.Add("f", new List<string> { f.Name });
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            instance.Send(serverPort, request, null,
                (ex, res) =>
                {
                    if (ex != null)
                    {
                        tcs.SetException(ex);
                    }
                    else
                    {
                        if (!res.StatusIndicatesSuccess)
                        {
                            tcs.SetException(new Exception(string.Format("status code indicates problem from {0}: {1}",
                                res.StatusIndicatesClientError ? "client" : "server", res.StatusMessage)));
                        }
                        tcs.SetResult(res.StatusIndicatesSuccess);
                    }
                });
            return tcs.Task;
        }
    }
}
