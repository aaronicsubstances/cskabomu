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
        private readonly IEventLoopApi _eventLoop;

        public FileReceiver(object remoteEndpoint, string uploadDirPath, IEventLoopApi eventLoop)
        {
            _remoteEndpoint = remoteEndpoint;
            _uploadDirPath = uploadDirPath;
            _eventLoop = eventLoop;
        }

        public async void ProcessRequest(IQuasiHttpRequest request, Action<Exception, IQuasiHttpResponse> cb)
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
                    var wrapper = new AsyncBody(request.Body);
                    var buffer = new byte[4096];
                    while (true)
                    {
                        var length = await wrapper.DataReadAsync(_eventLoop, buffer, 0, buffer.Length);
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
            cb.Invoke(null, response);
        }

        class AsyncBody
        {
            private readonly IQuasiHttpBody _body;

            public AsyncBody(IQuasiHttpBody body)
            {
                _body = body;
            }

            public Task<int> DataReadAsync(IMutexApi mutex, byte[] data, int offset, int bytesToRead)
            {
                var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
                _body.ReadBytes(mutex, data, offset, bytesToRead, (e, bytesRead) =>
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
