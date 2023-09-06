﻿using Kabomu.Common;
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
        private static readonly Random RandGen = new Random();

        public static async Task StartTransferringFiles(StandardQuasiHttpClient instance, object serverEndpoint,
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
            double rate = Math.Round(megaBytesTransferred / timeTaken, 2);
            LOG.Info("Successfully transferred {0} bytes ({1} MB) worth of data in {2} files in {3} seconds = {4} MB/s",
                bytesTransferred, megaBytesTransferred, count, timeTaken, rate);
        }

        private static async Task TransferFile(StandardQuasiHttpClient instance, object serverEndpoint, FileInfo f)
        {
            var requestHeaders = new DefaultQuasiHttpRequestHeaderPart
            {
                Headers = new Dictionary<string, IList<string>>()
            };
            var request = new DefaultQuasiHttpRequest
            {
                Headers = requestHeaders
            };
            requestHeaders.Headers.Add("f", new List<string> { f.Name });
            var fileStream = new FileStream(f.FullName, FileMode.Open, FileAccess.Read,
                FileShare.Read);
            requestHeaders.ContentLength = RandGen.NextDouble() < 0.5 ? -1 : f.Length;
            //requestHeaders.ContentLength = f.Length;
            request.Body = fileStream;
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
            if (res == null)
            {
                LOG.Warn("Received no response.");
                return;
            }
            if (res.Headers.StatusCode == QuasiHttpUtils.StatusCodeOk)
            {
                LOG.Info("File {0} sent successfully", f.FullName);
            }
            else
            {
                string responseMsg = "";
                if (res.Body != null)
                {
                    try
                    {
                        var responseMsgBytes = await IOUtils.ReadAllBytes(res.Body);
                        responseMsg = Encoding.UTF8.GetString(responseMsgBytes);
                    }
                    catch (Exception)
                    {
                        // ignore.
                    }
                }
                throw new Exception(string.Format("status code indicates error: {0}\n{1}",
                    res.Headers.StatusCode, responseMsg));
            }
        }
    }
}
