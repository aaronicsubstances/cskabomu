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
    public class FileReceiver
    {
        private static readonly Logger LOG = LogManager.GetCurrentClassLogger();
        private static readonly Random RandGen = new Random();
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
            string responseBody = null;
            if (transferError == null)
            {
                response.StatusCode = QuasiHttpCodec.StatusCodeOk;
                if (request.Headers.ContainsKey("echo-body"))
                {
                    responseBody = string.Join(',', request.Headers["echo-body"]);
                }
            }
            else
            {
                response.StatusCode = QuasiHttpCodec.StatusCodeServerError;
                responseBody = transferError.Message;
            }
            if (responseBody != null)
            {
                var responseBytes = MiscUtils.StringToBytes(responseBody);
                response.Body = new MemoryStream(responseBytes);
                response.ContentLength = responseBytes.Length;
                if (!FileSender.TurnOffComplexFeatures &&
                    RandGen.NextDouble() < 0.5)
                {
                    response.ContentLength = -1;
                }
            }
            return response;
        }
    }
}
