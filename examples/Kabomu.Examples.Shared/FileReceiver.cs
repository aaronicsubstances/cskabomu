using Kabomu.Abstractions;
using Kabomu.ProtocolImpl;
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
        private readonly object remoteEndpoint;
        private readonly string downloadDirPath;

        public FileReceiver(object remoteEndpoint,
                string downloadDirPath) {
            this.remoteEndpoint = remoteEndpoint;
            this.downloadDirPath = downloadDirPath;
        }

        public async Task<IQuasiHttpResponse> ProcessRequest(IQuasiHttpRequest request)
        {
            var fileName = Path.GetFileName(request.Headers["f"][0]);
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
                    await MiscUtils.CopyBytesToStream(request.Body, fileStream);
                }
            }
            catch (Exception e)
            {
                transferError = e;
            }

            LOG.Info(transferError, "File {0} received {1}", fileName,
                transferError == null ? "successfully" : "with error");

            var response = new DefaultQuasiHttpResponse();
            if (transferError == null)
            {
                response.StatusCode = QuasiHttpCodec.StatusCodeOk;
            }
            else
            {
                response.StatusCode = QuasiHttpCodec.StatusCodeServerError;
                response.HttpStatusMessage = transferError.Message;
            }
            return response;
        }
    }
}
