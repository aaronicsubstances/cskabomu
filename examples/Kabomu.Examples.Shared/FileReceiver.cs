using Kabomu.Common;
using Kabomu.QuasiHttp;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Examples.Shared
{
    public class FileReceiver : IQuasiHttpApplication
    {
        private static readonly Logger LOG = LogManager.GetCurrentClassLogger();

        private readonly object _remoteEndpoint;
        private readonly string _uploadDirPath;

        public FileReceiver(object remoteEndpoint, string uploadDirPath)
        {
            _remoteEndpoint = remoteEndpoint;
            _uploadDirPath = uploadDirPath;
        }

        public async Task<IQuasiHttpResponse> ProcessRequest(IQuasiHttpRequest request, IQuasiHttpProcessingOptions options)
        {
            var fileName = request.Headers["f"][0];
            LOG.Debug("Starting receipt of file {0} from {1}...", fileName, _remoteEndpoint);

            Exception transferError = null;
            try
            {
                // ensure directory exists.
                var directory = new DirectoryInfo(Path.Combine(_uploadDirPath, _remoteEndpoint.ToString()));
                directory.Create();
                string p = Path.Combine(directory.Name, fileName);
                using (var fileStream = new FileStream(p, FileMode.Create))
                {
                    var buffer = new byte[4096];
                    while (true)
                    {
                        var length = await request.Body.ReadBytes(buffer, 0, buffer.Length);
                        if (length == 0)
                        {
                            break;
                        }
                        await fileStream.WriteAsync(buffer, 0, length);
                    }
                }
            }
            catch (Exception e)
            {
                transferError = e;
            }

            LOG.Info(transferError, "File {0} received {1}", fileName, transferError == null ? "successfully" : "with error");

            var response = new DefaultQuasiHttpResponse
            {
                StatusIndicatesSuccess = transferError == null,
                StatusMessage = transferError?.Message ?? "OK"
            };
            return response;
        }
    }
}
