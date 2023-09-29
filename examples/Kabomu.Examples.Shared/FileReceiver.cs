using Kabomu.Abstractions;
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
                    LOG.Debug("Starting receipt of file {0} from {1}...", fileName, remoteEndpoint);
                    await request.Body.CopyToAsync(fileStream);
                }
            }
            catch (Exception e)
            {
                transferError = e;
            }

            var response = new DefaultQuasiHttpResponse();
            string responseBody = null;
            if (transferError == null)
            {
                LOG.Info("File {0} received successfully", fileName);
                response.StatusCode = QuasiHttpUtils.StatusCodeOk;
                if (request.Headers.ContainsKey("echo-body"))
                {
                    responseBody = string.Join(',', request.Headers["echo-body"]);
                }
            }
            else
            {
                LOG.Error(transferError, "File {0} received with error",
                    fileName);
                response.StatusCode = QuasiHttpUtils.StatusCodeServerError;
                responseBody = transferError.Message;
            }
            if (responseBody != null)
            {
                var responseBytes = Encoding.UTF8.GetBytes(responseBody);
                response.Body = new MemoryStream(responseBytes);
            }
            return response;
        }
    }
}
