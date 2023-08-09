using Kabomu.Common;
using Kabomu.Mediator;
using Kabomu.Mediator.Handling;
using Kabomu.Mediator.Registry;
using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.EntityBody;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Kabomu.Examples.Shared
{
    public class FileReceiver
    {
        private static readonly Logger LOG = LogManager.GetCurrentClassLogger();

        private FileReceiver() { }

        public static IQuasiHttpApplication Create(object remoteEndpoint, string downloadDirPath)
        {
            var appConstants = new DefaultMutableRegistry();
            appConstants.Add("remoteEndpoint", remoteEndpoint);
            appConstants.Add("downloadDirPath", downloadDirPath);
            var initialHandlers = new Handler[]
            {
                context => ReceiveFileTransfer(context)
            };
            var app = new MediatorQuasiWebApplication
            {
                InitialHandlers = initialHandlers,
                HandlerConstants = appConstants
            };
            return app;
        }

        private static async Task ReceiveFileTransfer(IContext context)
        {
            var remoteEndpoint = context.Get("remoteEndpoint");
            var downloadDirPath = (string)context.Get("downloadDirPath");
            var fileName = context.Request.Headers.Get("f");
            LOG.Debug("Starting receipt of file {0} from {1}...", fileName, remoteEndpoint);

            Exception transferError = null;
            try
            {
                // ensure directory exists.
                // just in case remote endpoint contains invalid file path characters...
                var pathForRemoteEndpoint = Regex.Replace(remoteEndpoint.ToString(), @"\W", "_");
                var directory = new DirectoryInfo(Path.Combine(downloadDirPath, pathForRemoteEndpoint));
                directory.Create();
                string p = Path.Combine(directory.Name, fileName);
                using (var fileStream = new FileStream(p, FileMode.Create))
                {
                    var reader = context.Request.Body.AsReader();
                    await IOUtils.CopyBytes(reader, fileStream);
                }
            }
            catch (Exception e)
            {
                transferError = e;
            }

            LOG.Info(transferError, "File {0} received {1}", fileName, transferError == null ? "successfully" : "with error");

            if (transferError == null)
            {
                context.Response.SetSuccessStatusCode();
            }
            else
            {
                context.Response.SetServerErrorStatusCode();
                context.Response.RawResponse.HttpStatusMessage = transferError.Message;
            }

            context.Response.Send();
        }
    }
}
