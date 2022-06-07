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

                await StartTransferringFiles(instance, serverPath, uploadDirPath);
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

        private static async Task StartTransferringFiles(KabomuQuasiHttpClient instance, object serverEndpoint,
            string uploadDirPath)
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
                await TransferFile(instance, serverEndpoint, f);
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
        private static Task TransferFile(KabomuQuasiHttpClient instance, object serverEndpoint, FileInfo f)
        {
            var request = new DefaultQuasiHttpRequest
            {
                Headers = new Dictionary<string, List<string>>(),
                Body = new FileBody(f.DirectoryName, f.Name)
            };
            request.Headers.Add("f", new List<string> { f.Name });
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            instance.Send(serverEndpoint, request, null,
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
