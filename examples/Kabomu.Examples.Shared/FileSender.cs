using Kabomu.Common;
using Kabomu.Common.Bodies;
using Kabomu.QuasiHttp;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Examples.Shared
{
    public class FileSender
    {
        private static readonly Logger LOG = LogManager.GetCurrentClassLogger();

        public static async Task StartTransferringFiles(IQuasiHttpClient instance, object serverEndpoint,
            string uploadDirPath)
        {
            var directory = new DirectoryInfo(uploadDirPath);
            int count = 0;
            long bytesTransferred = 0L;
            DateTime startTime = DateTime.Now;
            foreach (var f in directory.GetFiles())
            {
                LOG.Debug("Transferring {0}", f.Name);
                await TransferFile(instance, serverEndpoint, f);
                LOG.Info("Successfully transferred {0}", f.Name);
                bytesTransferred += f.Length;
                count++;
            }
            double timeTaken = Math.Round((DateTime.Now - startTime).TotalSeconds, 2);
            double megaBytesTransferred = Math.Round(bytesTransferred / (1024.0 * 1024.0), 2);
            LOG.Info("Successfully transferred {0} bytes ({1} MB) worth of data in {2} files in {3} seconds",
                bytesTransferred, megaBytesTransferred, count, timeTaken);
        }

        private static async Task TransferFile(IQuasiHttpClient instance, object serverEndpoint, FileInfo f)
        {
            var request = new DefaultQuasiHttpRequest
            {
                Headers = new Dictionary<string, List<string>>()
            };
            request.Headers.Add("f", new List<string> { f.Name });
            var fileStream = new FileStream(f.FullName, FileMode.Open, FileAccess.Read,
                FileShare.Read);
            request.Body = new StreamBackedBody(fileStream, null);
            IQuasiHttpResponse res;
            try
            {
                res = await instance.Send(serverEndpoint, request, null);
            }
            catch (Exception)
            {
                LOG.Info("File {0} sent with error", f.FullName);
                throw;
            }
            if (res.StatusIndicatesSuccess)
            {
                LOG.Info("File {0} sent successfully", f.FullName);
            }
            else
            {
                throw new Exception(string.Format("status code indicates problem from {0}: {1}",
                    res.StatusIndicatesClientError ? "client" : "server", res.StatusMessage));
            }
        }
    }
}
