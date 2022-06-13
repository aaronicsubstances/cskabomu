using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Kabomu.Common.Bodies
{
    public class FileBody : IQuasiHttpBody
    {
        private IQuasiHttpBody _backingBody;
        private Exception _srcEndError;

        public FileBody(string fileName, long contentLength, string contentType)
        {
            if (fileName == null)
            {
                throw new ArgumentException("null file name");
            }
            FileName = fileName;
            ContentLength = contentLength;
            ContentType = contentType;
        }

        public string FileName { get; }

        public long ContentLength { get; }

        public string ContentType { get; }

        public void ReadBytes(IMutexApi mutex, byte[] data, int offset, int bytesToRead, Action<Exception, int> cb)
        {
            if (mutex == null)
            {
                throw new ArgumentException("null mutex api");
            }
            if (!ByteUtils.IsValidMessagePayload(data, offset, bytesToRead))
            {
                throw new ArgumentException("invalid destination buffer");
            }
            if (cb == null)
            {
                throw new ArgumentException("null callback");
            }
            mutex.RunExclusively(_ =>
            {
                if (_srcEndError != null)
                {
                    return;
                }
                if (_backingBody == null)
                {
                    try
                    {
                        var fileStream = new FileStream(FileName, FileMode.Open, FileAccess.Read,
                                FileShare.Read);
                        _backingBody = new StreamBackedBody(fileStream, null);
                    }
                    catch (Exception e)
                    {
                        EndRead(mutex, cb, e);
                        return;
                    }
                }
                _backingBody.ReadBytes(mutex, data, offset, bytesToRead, cb);

            }, null);
        }

        public void OnEndRead(IMutexApi mutex, Exception e)
        {
            if (mutex == null)
            {
                throw new ArgumentException("null mutex api");
            }
            mutex.RunExclusively(_ =>
            {
                if (_srcEndError != null)
                {
                    return;
                }
                EndRead(mutex, null, e);
            }, null);
        }

        private void EndRead(IMutexApi mutex, Action<Exception, int> cb, Exception e)
        {
            _srcEndError = e ?? new Exception("end of read");
            _backingBody?.OnEndRead(mutex, e);
            cb?.Invoke(_srcEndError, 0);
        }
    }
}
