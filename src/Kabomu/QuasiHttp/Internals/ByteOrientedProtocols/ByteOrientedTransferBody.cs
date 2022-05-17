using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.Internals.ByteOrientedProtocols
{
    internal class ByteOrientedTransferBody : IQuasiHttpBody
    {
        private int _readContentLength;
        private Exception _srcEndError;

        public ByteOrientedTransferBody(bool releaseConnectionOnEndOfTransfer, int contentLength, string contentType,
            IQuasiHttpTransport transport, object connection,
            IMutexApi mutexApi)
        {
            ReleaseConnectionOnEndOfTransfer = releaseConnectionOnEndOfTransfer;
            ContentLength = contentLength;
            ContentType = contentType;
            Transport = transport;
            Connection = connection;
            MutexApi = mutexApi ?? new BlockingMutexApi(this);
        }
        public bool ReleaseConnectionOnEndOfTransfer { get; }
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
                    if (e != null)
                    {
                        cb.Invoke(e, 0);
                        return;
                    }
                    if (ContentLength >= 0)
                    {
                        MutexApi.RunCallback(_ =>
                        {
                            if (_readContentLength + bytesRead > ContentLength)
                            {
                                OnEndRead(new Exception("content length exceeded"));
                                return;
                            }
                            _readContentLength += bytesRead;
                            cb.Invoke(null, bytesRead);
                        }, null);
                    }
                    else
                    {
                        cb.Invoke(null, bytesRead);
                    }
                };
                Transport.ReadBytes(Connection, data, offset, bytesToRead, wrapperCb);
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
                _srcEndError = e ?? new Exception("end of read");
                if (ReleaseConnectionOnEndOfTransfer)
                {
                    Transport.ReleaseConnection(Connection);
                }
            }, null);
        }
    }
}
