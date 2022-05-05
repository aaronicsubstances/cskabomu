using Kabomu.QuasiHttp;
using NLog;
using System;
using System.IO;

namespace Udp.FileClient
{
    internal class FileBody : IQuasiHttpBody
    {
        private static readonly Logger LOG = LogManager.GetCurrentClassLogger();

        private readonly string _fileName;
        private readonly FileStream _fileStream;
        private readonly byte[] _buffer = new byte[8192];

        public FileBody(string uploadDirPath, string fileName)
        {
            _fileName = fileName;
            _fileStream = new FileStream(Path.Combine(uploadDirPath, fileName), FileMode.Open, FileAccess.Read,
                    FileShare.Read);
        }

        public string ContentType => "application/octet-stream";

        public int ContentLength => -1;

        public async void OnDataRead(QuasiHttpBodyCallback cb)
        {
            try
            {
                int bytesRead = await _fileStream.ReadAsync(_buffer, 0, _buffer.Length);
                cb.Invoke(null, _buffer, 0, bytesRead);
            }
            catch (Exception e)
            {
                cb.Invoke(e, null, 0, 0);
            }
        }

        public async void OnEndRead(Exception e)
        {
            try
            {
                if (_fileStream != null)
                {
                    await _fileStream.DisposeAsync();
                }
            }
            catch (Exception)
            {
                // ignore
            }
            LOG.Info(e, "File {0} sent {1}", _fileName, e == null ? "successfully" : "with error");
        }
    }
}