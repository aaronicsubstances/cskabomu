using Kabomu.Abstractions;
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

        /// <summary>
        /// The purpose of this flag and the dead code that is present
        /// as a result, is make the dead code serve as reference for
        /// porting in stages of HTTP/1.0 only, before HTTP/1.1.
        /// </summary>
        internal static readonly bool SupportHttp10Only = false;

        /// <summary>
        /// The purpose of this flag and the dead code that is present
        /// as a result, is to make the dead code serve as reference for
        /// porting in stages of postponing "complex" features such as
        /// <list type="bullet">
        /// <item>always set response content length to
        ///   positive values if a body is present.</item>
        ///   <item>enable response buffering</item>
        ///   <item>use Send2() instead of Send() method</item>
        /// </list>
        /// </summary>
        internal static readonly bool TurnOffComplexFeatures = false;

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
                LOG.Debug("Successfully transferred {0}", f.Name);
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
            var request = new DefaultQuasiHttpRequest
            {
                Headers = new Dictionary<string, IList<string>>()
            };
            request.Headers.Add("f", new List<string> { f.Name });
            var echoBodyOn = RandGen.NextDouble() < 0.5;
            if (echoBodyOn)
            {
                request.Headers.Add("echo-body",
                    new List<string> { f.FullName });
            }

            // add body.
            var fileStream = new FileStream(f.FullName, FileMode.Open, FileAccess.Read,
                FileShare.Read);
            request.ContentLength = f.Length;
            if (!SupportHttp10Only && RandGen.NextDouble() < 0.5)
            {
                request.ContentLength = -1;
            }
            request.Body = fileStream;

            // determine options
            IQuasiHttpProcessingOptions sendOptions = null;
            if (TurnOffComplexFeatures || RandGen.NextDouble() < 0.5)
            {
                sendOptions = new DefaultQuasiHttpProcessingOptions
                {
                    ResponseBufferingEnabled = false
                };
            }

            IQuasiHttpResponse res = null;
            try
            {
                if (TurnOffComplexFeatures || RandGen.NextDouble() < 0.5)
                {
                    res = await instance.Send(serverEndpoint, request,
                        sendOptions);
                }
                else
                {
                    res = await instance.Send2(serverEndpoint,
                        _ => Task.FromResult<IQuasiHttpRequest>(request),
                        sendOptions);
                }
                if (res.StatusCode == QuasiHttpUtils.StatusCodeOk)
                {
                    if (echoBodyOn)
                    {
                        var memStream = new MemoryStream();
                        await res.Body.CopyToAsync(memStream);
                        var actualResBody = Encoding.UTF8.GetString(
                            memStream.ToArray());
                        if (actualResBody != f.FullName)
                        {
                            throw new Exception("expected echo body to be " +
                                $"{f.FullName} but got {actualResBody}");
                        }
                    }
                    LOG.Info("File {0} sent successfully", f.FullName);
                }
                else
                {
                    string responseMsg = "";
                    if (res.Body != null)
                    {
                        try
                        {
                            var responseMsgBytes = new MemoryStream();
                            await res.Body.CopyToAsync(responseMsgBytes);
                            responseMsg = Encoding.UTF8.GetString(
                                responseMsgBytes.ToArray());
                        }
                        catch (Exception)
                        {
                            // ignore.
                        }
                    }
                    throw new Exception(string.Format(
                        "status code indicates error: {0}\n{1}",
                        res.StatusCode, responseMsg));
                }
            }
            catch (Exception e)
            {
                LOG.Warn("File {0} sent with error: {1}", f.FullName,
                    e.Message);
                throw;
            }
            finally
            {
                await fileStream.DisposeAsync();
                if (res != null)
                {
                    await res.DisposeAsync();
                }
            }
        }
    }
}
