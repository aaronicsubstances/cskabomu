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

        public FileBody(string uploadDirPath, string fileName, bool serveContentLength)
        {
            _fileName = fileName;
            FileInfo f = new FileInfo(Path.Combine(uploadDirPath, fileName));
            _fileStream = new FileStream(f.FullName, FileMode.Open, FileAccess.Read,
                    FileShare.Read);
            if (serveContentLength)
            {
                ContentLength = (int)f.Length;
            }
            else
            {
                ContentLength = -1;
            }
        }

        public string ContentType => "application/octet-stream";

        public int ContentLength { get; }

        public async void OnDataRead(int bytesToRead, QuasiHttpBodyCallback cb)
        {
            try
            {
                bytesToRead = Math.Min(bytesToRead, _buffer.Length);
                int bytesRead = await _fileStream.ReadAsync(_buffer, 0, bytesToRead);
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