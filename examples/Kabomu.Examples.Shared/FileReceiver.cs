using Kabomu.Common;
using Kabomu.Mediator;
using Kabomu.Mediator.Handling;
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
    public class FileReceiver : IQuasiHttpApplication
    {
        private static readonly Logger LOG = LogManager.GetCurrentClassLogger();

        private readonly object _remoteEndpoint;
        private readonly string _downloadDirPath;

        public FileReceiver(object remoteEndpoint, string downloadDirPath)
        {
            _remoteEndpoint = remoteEndpoint;
            _downloadDirPath = downloadDirPath;
        }

        public Task<IQuasiHttpResponse> ProcessRequest(IQuasiHttpRequest request)
        {
            var initialHandlers = new Handler[]
            {
                context => ReceiveFileTransfer(context)
            };
            var delegateApp = new MediatorQuasiWebApplication
            {
                InitialHandlers = initialHandlers
            };
            return delegateApp.ProcessRequest(request);
        }

        private async Task ReceiveFileTransfer(IContext context)
        {
            var fileName = context.Request.Headers.Get("f");
            LOG.Debug("Starting receipt of file {0} from {1}...", fileName, _remoteEndpoint);

            Exception transferError = null;
            try
            {
                // ensure directory exists.
                // just in case remote endpoint contains invalid file path characters...
                var pathForRemoteEndpoint = Regex.Replace(_remoteEndpoint.ToString(), @"\W", "_");
                var directory = new DirectoryInfo(Path.Combine(_downloadDirPath, pathForRemoteEndpoint));
                directory.Create();
                string p = Path.Combine(directory.Name, fileName);
                using (var fileStream = new FileStream(p, FileMode.Create))
                {
                    var reader = context.Request.Body.AsReader();
                    var fileStreamWrapper = new LambdaBasedCustomWriter(
                        (data, offset, length) =>
                            fileStream.WriteAsync(data, offset, length));
                    await IOUtils.CopyBytes(reader, fileStreamWrapper, 0);
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
