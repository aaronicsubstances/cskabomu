using CommandLine;
using Kabomu.QuasiHttp.Transport;
using NLog;
using System;
using System.Threading.Tasks;

namespace Memory.FileExchange
{
    class Program
    {
        static readonly Logger LOG = LogManager.GetCurrentClassLogger();

        public class Options
        {
            [Option('s', "server-upload-dir", Required = false,
                HelpText = "Path to directory of files to upload. Defaults to current directory")]
            public string ServerUploadDirPath { get; set; }

            [Option('d', "client-upload-dir", Required = false,
                HelpText = "Path to directory for saving uploaded files. Defaults to current directory")]
            public string ClientUploadDirPath { get; set; }

            [Option('p', "direct-send-probability (0-1)", Required = false,
                HelpText = "Probability of processing request directly and skipping serialization. Defaults to 0.5")]
            public double? DirectSendProbability { get; set; }
        }

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                   .WithParsed<Options>(o =>
                   {
                       var clientEndpoint = "takoradi";
                       var serverEndpoint = "kumasi";
                       var hub = new DefaultMemoryBasedTransportHub();
                       var serverTask = FileServer.RunMain(serverEndpoint, o.ServerUploadDirPath ?? ".",
                           hub);
                       var clientTask = FileClient.RunMain(clientEndpoint, serverEndpoint,
                           o.ClientUploadDirPath ?? ".", hub, o.DirectSendProbability ?? 0.5);
                       try
                       {
                           Task.WaitAll(serverTask, clientTask);
                       }
                       catch (Exception e)
                       {
                           LOG.Error(e, "Fatal error encountered");
                       }
                       finally
                       {
                           Task.WaitAll(FileServer.EndMain(), FileClient.EndMain());
                       }
                   });
        }
    }
}
