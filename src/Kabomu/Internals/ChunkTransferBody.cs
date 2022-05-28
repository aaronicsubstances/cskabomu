using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Internals
{
    internal class ChunkTransferBody : IQuasiHttpBody
    {
        private Action<Exception, int> _pendingCb;
        private byte[] _pendingData;
        private int _pendingDataOffset;
        private int _pendingBytesToRead;
        private int _readContentLength;
        private Exception _srcEndError;

        public ChunkTransferBody(int contentLength, string contentType, 
            Action<int> readCallback, Action closeCallback)
        {
            ContentLength = contentLength;
            ContentType = contentType;
            ReadCallback = readCallback;
            CloseCallback = closeCallback;
        }

        public int ContentLength { get; }
        public string ContentType { get; }
        public Action<int> ReadCallback { get; }
        public Action CloseCallback { get; }

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
                if (_pendingCb != null)
                {
                    cb.Invoke(new Exception("pending read unresolved"), 0);
                    return;
                }
                _pendingCb = cb;
                _pendingData = data;
                _pendingDataOffset = offset;
                _pendingBytesToRead = bytesToRead;
                ReadCallback.Invoke(bytesToRead);
            }, null);
        }

        public void OnDataWrite(IMutexApi mutex, byte[] data, int offset, int length)
        {
            if (!ByteUtils.IsValidMessagePayload(data, offset, length))
            {
                throw new ArgumentException("invalid buffer");
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
            CloseCallback.Invoke();
        }
    }
}
