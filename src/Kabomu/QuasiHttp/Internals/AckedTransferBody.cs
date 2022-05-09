using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.Internals
{
    internal class AckedTransferBody : IQuasiHttpBody
    {
        private int _readContentLength;
        private Exception _srcEndError;

        public AckedTransferBody(bool isResponseBody, int contentLength, string contentType,
            IQuasiHttpTransport transport, object connection,
            IMutexApi mutexApi)
        {
            IsResponseBody = isResponseBody;
            ContentLength = contentLength;
            ContentType = contentType;
            Transport = transport;
            Connection = connection;
            MutexApi = mutexApi ?? new BlockingMutexApi(this);
        }
        public bool IsResponseBody { get; }
        public string ContentType { get; }
        public int ContentLength { get; }
        public IQuasiHttpTransport Transport { get; }
        public object Connection { get; }
        public IMutexApi MutexApi { get; }

        public void OnDataRead(byte[] data, int offset, int bytesToRead, Action<Exception, int> cb)
        {
            if (cb == null)
            {
                throw new ArgumentException("null callback");
            }
            if (bytesToRead < 0)
            {
                throw new ArgumentException("received negative bytes to read");
            }
            MutexApi.RunCallback(_ =>
            {
                if (_srcEndError != null)
                {
                    cb.Invoke(_srcEndError, 0);
                    return;
                }
                Action<Exception, int> wrapperCb = (e, bytesRead) =>
                {
                    if (ContentLength >= 0)
                    {
                        if (_readContentLength + bytesRead > ContentLength)
                        {
                            OnEndRead(new Exception("content length exceeded"));
                            return;
                        }
                        _readContentLength += bytesRead;
                    }
                    cb.Invoke(e, bytesRead);
                };
                Transport.Read(Connection, data, offset, bytesToRead, wrapperCb);
            }, null);
        }

        public void OnEndRead(Exception e)
        {
            MutexApi.RunCallback(_ =>
            {
                if (_srcEndError != null)
                {
                    return;
                }
                if (IsResponseBody)
                {
                    Transport.ReleaseConnection(Connection);
                }
            }, null);
        }
    }
}
