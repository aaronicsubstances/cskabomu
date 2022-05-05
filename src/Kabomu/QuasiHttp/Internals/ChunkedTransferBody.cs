using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.Internals
{
    internal class ChunkedTransferBody : IQuasiHttpBody
    {
        private QuasiHttpBodyCallback _pendingCb;
        private int _readContentLength;
        private Exception _srcEndError;

        public ChunkedTransferBody(int contentLength, string contentType, 
            Action<bool> readCallback, IMutexApi mutexApi)
        {
            ContentLength = contentLength;
            ContentType = contentType;
            ReadCallback = readCallback;
            MutexApi = mutexApi ?? new BlockingMutexApi(this);
        }

        public int ContentLength { get; }
        public string ContentType { get; }
        public Action<bool> ReadCallback { get; }
        public IMutexApi MutexApi { get; }

        public void OnDataRead(QuasiHttpBodyCallback cb)
        {
            if (cb == null)
            {
                throw new ArgumentException("null callback");
            }
            MutexApi.RunCallback(_ =>
            {
                if (_srcEndError != null)
                {
                    cb.Invoke(_srcEndError, null, 0, 0);
                }
                if (_pendingCb != null)
                {
                    cb.Invoke(new Exception("pending read unresolved"), null, 0, 0);
                }
                _pendingCb = cb;
                ReadCallback.Invoke(true);
            }, null);
        }

        public void OnDataWrite(byte[] data, int offset, int length)
        {
            if (!ByteUtils.IsValidMessagePayload(data, offset, length))
            {
                throw new ArgumentException("invalid buffer");
            }

            MutexApi.RunCallback(_ =>
            {
                if (_srcEndError != null)
                {
                    return;
                }
                if (_pendingCb == null)
                {
                    OnEndRead(new Exception("received chunk response for no pending chunk request"));
                    return;
                }
                if (ContentLength >= 0)
                {
                    if (_readContentLength + length > ContentLength)
                    {
                        OnEndRead(new Exception("content length exceeded"));
                        return;
                    }
                }

                _readContentLength += length;

                _pendingCb.Invoke(null, data, offset, length);
                _pendingCb = null;
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
                _pendingCb?.Invoke(_srcEndError, null, 0, 0);
                _pendingCb = null;
                ReadCallback.Invoke(false);
            }, null);
        }
    }
}
