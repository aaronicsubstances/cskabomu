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

        public string ContentType { get; set; }
        public int ContentLength { get; set; }
        public Action ReadCallback { get; set; }
        public Action CloseCallback { get; set; }
        public IEventLoopApi EventLoop { get; set; }

        public void OnDataRead(QuasiHttpBodyCallback cb)
        {
            if (cb == null)
            {
                throw new ArgumentException("null callback");
            }
            EventLoop.PostCallback(_ =>
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
                ReadCallback.Invoke();

            }, null);
        }

        public void OnDataWrite(byte[] data, int offset, int length)
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
                _readContentLength += length;
            }

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
            ReadCallback = null;
        }

        public void Close()
        {
            EventLoop.PostCallback(_ =>
            {
                if (_srcEndError != null)
                {
                    return;
                }
                _srcEndError = new Exception("closed");
                _pendingCb?.Invoke(_srcEndError, null, 0, 0, false);
                _pendingCb = null;
                CloseCallback?.Invoke();
                CloseCallback = null;
            }, null);
        }
    }
}
