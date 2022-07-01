using CommandLine;
using Kabomu.QuasiHttp.Transport;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Memory.FileExchange
{
    class Program
    {
        public class Options
        {
            [Option('s', "server-upload-dir", Required = false,
                HelpText = "Path to directory of files to upload. Defaults to current directory")]
            public string ServerUploadDirPath { get; set; }

            [Option('d', "client-upload-dir", Required = false,
                HelpText = "Path to directory for saving uploaded files. Defaults to images/ folder in current directory")]
            public string ClientUploadDirPath { get; set; }

            [Option('p', "direct-send-probability (0-1)", Required = false,
                HelpText = "Probability of processing request directly and skipping serialization. Defaults to 0" +
                            " (ie requests are never procesed directly and serialization always kicks in)")]
            public double? DirectSendProbability { get; set; }
        }

        static void Mai2n(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                   .WithParsed<Options>(o =>
                   {
                       var clientEndpoint = "takoradi";
                       var serverEndpoint = "kumasi";
                       var hub = new MemoryBasedTransportHub();
                       var serverTask = FileServer.RunMain(serverEndpoint, o.ServerUploadDirPath ?? ".",
                           hub);
                       var clientTask = FileClient.RunMain(clientEndpoint, serverEndpoint,
                           o.ClientUploadDirPath ?? "images", hub, o.DirectSendProbability ?? 0);
                       Task.WaitAll(serverTask, clientTask);
                   });
        }

        static async Task Main()
        {
            Console.WriteLine("Thread Id before Memb(): {0}", Thread.CurrentThread.ManagedThreadId);
            await Memb();
            Console.WriteLine("Thread Id after Memb(): {0}", Thread.CurrentThread.ManagedThreadId);
        }

        private static async Task Memb()
        {
            Console.WriteLine("Thread Id before 2-sec delay: {0}", Thread.CurrentThread.ManagedThreadId);
            await Task.Delay(2000);
            Console.WriteLine("Thread Id after 2-sec delay: {0}", Thread.CurrentThread.ManagedThreadId);
        }
    }
}
