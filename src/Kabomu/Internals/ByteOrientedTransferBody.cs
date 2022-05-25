using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Internals
{
    internal class ByteOrientedTransferBody : IQuasiHttpBody
    {
        private int _readContentLength;
        private Exception _srcEndError;

        public ByteOrientedTransferBody(int contentLength, string contentType,
            IQuasiHttpTransport transport, object connection, Action<Exception> cb)
        {
            ContentLength = contentLength;
            ContentType = contentType;
            Transport = transport;
            Connection = connection;
            CompletionCallback = cb;
        }

        public string ContentType { get; }
        public int ContentLength { get; }
        public IQuasiHttpTransport Transport { get; }
        public object Connection { get; }
        public Action<Exception> CompletionCallback { get; }


        public void OnDataRead(IMutexApi mutex, byte[] data, int offset, int bytesToRead, Action<Exception, int> cb)
        {
            if (cb == null)
            {
                throw new ArgumentException("null callback");
            }
            if (bytesToRead < 0)
            {
                throw new ArgumentException("received negative bytes to read");
            }
            mutex.RunExclusively(_ =>
            {
                if (_srcEndError != null)
                {
                    cb.Invoke(_srcEndError, 0);
                    return;
                }
                Action<Exception, int> wrapperCb = (e, bytesRead) =>
                {
                    if (e != null)
                    {
                        cb.Invoke(e, 0);
                        return;
                    }
                    mutex.RunExclusively(_ =>
                    {
                        if (_srcEndError != null)
                        {
                            return;
                        }
                        if (ContentLength >= 0)
                        {
                            if (_readContentLength + bytesRead > ContentLength)
                            {
                                EndRead(new Exception("content length exceeded"));
                                return;
                            }
                            _readContentLength += bytesRead;
                        }
                        cb.Invoke(null, bytesRead);
                    }, null);
                };
                Transport.ReadBytes(Connection, data, offset, bytesToRead, wrapperCb);
            }, null);
        }

        public void OnEndRead(IMutexApi mutex, Exception e)
        {
            mutex.RunExclusively(_ =>
            {
                if (_srcEndError != null)
                {
                    return;
                }
                EndRead(e);
            }, null);
        }
        
        private void EndRead(Exception e)
        {
            CompletionCallback.Invoke(e);
            _srcEndError = e ?? new Exception("end of read");
        }
    }
}
