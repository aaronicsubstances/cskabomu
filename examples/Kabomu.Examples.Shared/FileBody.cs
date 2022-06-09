using Kabomu.Common;
using Kabomu.Common.Bodies;
using NLog;
using System;
using System.IO;

namespace Kabomu.Examples.Shared
{
    public class FileBody : IQuasiHttpBody
    {
        private static readonly Logger LOG = LogManager.GetCurrentClassLogger();

        private readonly IQuasiHttpBody _backingBody;
        private readonly string _fileName;

        public FileBody(string uploadDirPath, string fileName)
        {
            FileInfo f = new FileInfo(Path.Combine(uploadDirPath, fileName));
            var fileStream = new FileStream(f.FullName, FileMode.Open, FileAccess.Read,
                    FileShare.Read);
            _backingBody = new StreamBackedBody(fileStream, null);
            _fileName = fileName;
        }

        public string ContentType => _backingBody.ContentType;

        public void ReadBytes(IMutexApi mutex, byte[] data, int offset, int bytesToRead, Action<Exception, int> cb)
        {
            _backingBody.ReadBytes(mutex, data, offset, bytesToRead, cb);
        }

        public void OnEndRead(IMutexApi mutex, Exception e)
        {
            _backingBody.OnEndRead(mutex, e);
            LOG.Info(e, "File {0} sent {1}", _fileName, e == null ? "successfully" : "with error");
        }
    }
}