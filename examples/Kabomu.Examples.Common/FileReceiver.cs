using Kabomu.Common;
using Kabomu.QuasiHttp;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Examples.Common
{
    public class FileReceiver : IQuasiHttpApplication
    {
        private static readonly Logger LOG = LogManager.GetCurrentClassLogger();

        private readonly int _port;
        private readonly string _uploadDirPath;

        public FileReceiver(int port, string uploadDirPath)
        {
            _port = port;
            _uploadDirPath = uploadDirPath;
        }

        public async void ProcessRequest(IQuasiHttpRequestMessage request, Action<Exception, IQuasiHttpResponseMessage> cb)
        {
            var fileName = request.Headers["f"][0];
            LOG.Debug("Starting receipt of file {0} from {1}...", fileName, _port);

            Exception transferError = null;
            try
            {
                // ensure directory exists.
                var directory = new DirectoryInfo(Path.Combine(_uploadDirPath, _port.ToString()));
                directory.Create();
                string p = Path.Combine(directory.Name, fileName);
                using (var fileStream = new FileStream(p, FileMode.Create))
                {
                    var wrapper = new AsyncBody(request.Body);
                    var buffer = new byte[4096];
                    while (true)
                    {
                        var length = await wrapper.DataReadAsync(buffer, 0, buffer.Length);
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

            var response = new DefaultQuasiHttpResponseMessage
            {
                StatusIndicatesSuccess = transferError == null,
                StatusMessage = transferError?.Message ?? "OK"
            };
            cb.Invoke(null, response);
        }

        class AsyncBody
        {
            private readonly IQuasiHttpBody _body;

            public AsyncBody(IQuasiHttpBody body)
            {
                _body = body;
            }

            public Task<int> DataReadAsync(byte[] data, int offset, int bytesToRead)
            {
                var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
                _body.OnDataRead(data, offset, bytesToRead, (e, bytesRead) =>
                {
                    if (e != null)
                    {
                        tcs.SetException(e);
                    }
                    else
                    {
                        tcs.SetResult(bytesRead);
                    }
                });
                return tcs.Task;
            }
        }
    }
}
