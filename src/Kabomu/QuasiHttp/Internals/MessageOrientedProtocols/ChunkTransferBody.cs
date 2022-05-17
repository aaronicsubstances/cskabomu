using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.Internals.MessageOrientedProtocols
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
            Action<int> readCallback, Action closeCallback, IMutexApi mutexApi)
        {
            ContentLength = contentLength;
            ContentType = contentType;
            ReadCallback = readCallback;
            CloseCallback = closeCallback;
            MutexApi = mutexApi ?? new BlockingMutexApi(this);
        }

        public int ContentLength { get; }
        public string ContentType { get; }
        public Action<int> ReadCallback { get; }
        public Action CloseCallback { get; }
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
                if (length > _pendingBytesToRead)
                {
                    OnEndRead(new Exception("received chunk response larger than pending chunk request size"));
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

                Array.Copy(data, offset, _pendingData, _pendingDataOffset, length);
                _pendingCb.Invoke(null, length);
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
                _pendingCb?.Invoke(_srcEndError, 0);
                _pendingCb = null;
                CloseCallback.Invoke();
            }, null);
        }
    }
}
