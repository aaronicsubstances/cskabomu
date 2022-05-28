using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Internals
{
    internal class ChunkTransferBody : IQuasiHttpBody
    {
        private Action<int> _readCallback;
        private Action _closeCallback;
        private Action<Exception, int> _pendingCb;
        private byte[] _pendingData;
        private int _pendingDataOffset;
        private int _pendingBytesToRead;
        private int _readContentLength;
        private Exception _srcEndError;

        public ChunkTransferBody(int contentLength, string contentType, 
            Action<int> readCallback, Action closeCallback)
        {
            if (readCallback == null)
            {
                throw new ArgumentException("null read callback");
            }
            ContentLength = contentLength;
            ContentType = contentType;
            _readCallback = readCallback;
            _closeCallback = closeCallback;
        }

        public int ContentLength { get; }
        public string ContentType { get; }

        public void OnDataRead(IMutexApi mutex, byte[] data, int offset, int length, Action<Exception, int> cb)
        {
            if (mutex == null)
            {
                throw new ArgumentException("null mutex api");
            }
            if (!ByteUtils.IsValidMessagePayload(data, offset, length))
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
                    cb.Invoke(_srcEndError, 0);
                    return;
                }
                if (_pendingCb != null)
                {
                    cb.Invoke(new Exception("pending read unresolved"), 0);
                    return;
                }
                _pendingCb = cb;
                _pendingData = data;
                _pendingDataOffset = offset;
                _pendingBytesToRead = length;
                _readCallback.Invoke(length);
            }, null);
        }

        public void OnDataWrite(IMutexApi mutex, byte[] data, int offset, int length)
        {
            if (mutex == null)
            {
                throw new ArgumentException("null mutex api");
            }
            if (!ByteUtils.IsValidMessagePayload(data, offset, length))
            {
                throw new ArgumentException("invalid source buffer");
            }

            mutex.RunExclusively(_ =>
            {
                if (_srcEndError != null)
                {
                    return;
                }
                if (_pendingCb == null)
                {
                    EndRead(new Exception("received chunk response for no pending chunk request"));
                    return;
                }
                if (length > _pendingBytesToRead)
                {
                    EndRead(new Exception("received chunk response larger than pending chunk request size"));
                    return;
                }
                if (ContentLength >= 0)
                {
                    if (length == 0 && _readContentLength != ContentLength)
                    {
                        EndRead(new Exception("content length not achieved"));
                        return;
                    }
                    if (_readContentLength + length > ContentLength)
                    {
                        EndRead(new Exception("content length exceeded"));
                        return;
                    }
                    _readContentLength += length;
                }

                Array.Copy(data, offset, _pendingData, _pendingDataOffset, length);
                _pendingCb.Invoke(null, length);
                _pendingCb = null;
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
                EndRead(e);
            }, null);
        }

        private void EndRead(Exception e)
        {
            _srcEndError = e ?? new Exception("end of read");
            _pendingCb?.Invoke(_srcEndError, 0);
            _pendingCb = null;
            _closeCallback?.Invoke();
        }
    }
}
