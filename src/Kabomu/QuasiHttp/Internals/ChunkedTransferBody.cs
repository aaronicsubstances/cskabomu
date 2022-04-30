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
            Action<object> readCallback, object readCallbackState,
            Action<object> closeCallback, object closeCallbackState,
            IMutexApi mutexApi)
        {
            ContentLength = contentLength;
            ContentType = contentType;
            ReadCallback = readCallback;
            ReadCallbackState = readCallbackState;
            CloseCallback = closeCallback;
            CloseCallbackState = closeCallbackState;
            MutexApi = mutexApi ?? new BlockingMutexApi(this);
        }

        public int ContentLength { get; }
        public string ContentType { get; }
        public Action<object> ReadCallback { get; }
        public object ReadCallbackState { get; }
        public Action<object> CloseCallback { get; }
        public object CloseCallbackState { get; }
        public IMutexApi MutexApi { get; }

        public void OnDataRead(QuasiHttpBodyCallback cb)
        {
            if (cb == null)
            {
                throw new ArgumentException("null callback");
            }
            MutexApi.PostCallback(_ =>
            {
                if (_srcEndError != null)
                {
                    cb.Invoke(_srcEndError, null, 0, 0, false);
                }
                if (_pendingCb != null)
                {
                    cb.Invoke(new Exception("pending read unresolved"), null, 0, 0, false);
                }
                _pendingCb = cb;
                ReadCallback.Invoke(ReadCallbackState);
            }, null);
        }

        public void OnDataWrite(byte[] data, int offset, int length)
        {
            if (!ByteUtils.IsValidMessagePayload(data, offset, length))
            {
                throw new ArgumentException("invalid buffer");
            }

            MutexApi.PostCallback(_ =>
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
                if (ContentLength == 0)
                {
                    if (length > 0)
                    {
                        OnEndRead(new Exception("content length exceeded"));
                        return;
                    }
                }
                else if (ContentLength > 0)
                {
                    if (length == 0)
                    {
                        OnEndRead(new Exception("unacceptable zero length chunk if content length is specified"));
                        return;
                    }
                    if (_readContentLength + length > ContentLength)
                    {
                        OnEndRead(new Exception("content length exceeded"));
                        return;
                    }
                }

                _readContentLength += length;

                bool hasMore = true;
                if (ContentLength >= 0)
                {
                    if (_readContentLength == ContentLength)
                    {
                        hasMore = false;
                    }
                }
                else if (length == 0)
                {
                    hasMore = false;
                }

                _pendingCb.Invoke(null, data, offset, length, hasMore);
                _pendingCb = null;

                if (!hasMore)
                {
                    OnEndRead(null);
                }
            }, null);
        }

        private void OnEndRead(Exception error)
        {
            if (_srcEndError != null)
            {
                return;
            }
            _srcEndError = error ?? new Exception("end of read");
            _pendingCb?.Invoke(_srcEndError, null, 0, 0, false);
            _pendingCb = null;
        }

        public void Close()
        {
            MutexApi.PostCallback(_ =>
            {
                if (_srcEndError != null)
                {
                    return;
                }
                _srcEndError = new Exception("closed");
                _pendingCb?.Invoke(_srcEndError, null, 0, 0, false);
                _pendingCb = null;
                CloseCallback.Invoke(CloseCallbackState);
            }, null);
        }
    }
}
