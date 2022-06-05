using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Internals
{
    internal class ByteOrientedTransferBody : IQuasiHttpBody
    {
        private readonly IQuasiHttpTransport _transport;
        private readonly object _connection;
        private readonly Action _closeCallback;
        private byte[] _lastChunk;
        private int _lastChunkOffset;
        private int _lastChunkRem;
        private Exception _srcEndError;

        public ByteOrientedTransferBody(string contentType,
            IQuasiHttpTransport transport, object connection, Action closeCallback)
        {
            if (transport == null)
            {
                throw new ArgumentException("null transport");
            }
            ContentType = contentType;
            _transport = transport;
            _connection = connection;
            _closeCallback = closeCallback;
        }

        public string ContentType { get; }

        public void OnDataRead(IMutexApi mutex, byte[] data, int offset, int bytesToRead, Action<Exception, int> cb)
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
                    cb.Invoke(_srcEndError, 0);
                    return;
                }
                if (_lastChunk != null && (_lastChunk.Length == 0 || _lastChunkRem > 0))
                {
                    SupplyFromLastChunk(data, offset, bytesToRead, cb);
                    return;
                }
                var encodedLength = new byte[2];
                TransportUtils.ReadBytesFully(_transport, _connection,
                    encodedLength, 0, encodedLength.Length, e =>
                {
                    mutex.RunExclusively(_ =>
                    {
                        if (_srcEndError != null)
                        {
                            cb.Invoke(_srcEndError, 0);
                            return;
                        }
                        if (e != null)
                        {
                            EndRead(cb, e);
                            return;
                        }
                        int chunkLen = (int)ByteUtils.DeserializeUpToInt64BigEndian(encodedLength, 0,
                            encodedLength.Length);
                        _lastChunk = new byte[chunkLen];
                        _lastChunkOffset = 0;
                        _lastChunkRem = _lastChunk.Length;
                        if (_lastChunkRem == 0)
                        {
                            cb.Invoke(null, 0);
                            return;
                        }
                        TransportUtils.ReadBytesFully(_transport, _connection,
                            _lastChunk, 0, _lastChunk.Length, e =>
                        {
                            mutex.RunExclusively(_ =>
                            {
                                if (_srcEndError != null)
                                {
                                    cb.Invoke(_srcEndError, 0);
                                    return;
                                }
                                if (e != null)
                                {
                                    EndRead(cb, e);
                                    return;
                                }
                                SupplyFromLastChunk(data, offset, bytesToRead, cb);
                            }, null);
                        });
                    }, null);
                });
            }, null);
        }

        private void SupplyFromLastChunk(byte[] data, int offset, int bytesToRead, Action<Exception, int> cb)
        {
            int lengthToUse = Math.Min(_lastChunkRem, bytesToRead);
            Array.Copy(_lastChunk, _lastChunkOffset, data, offset, lengthToUse);
            _lastChunkOffset += lengthToUse;
            _lastChunkRem -= lengthToUse;
            cb.Invoke(null, lengthToUse);
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
                EndRead(null, e);
            }, null);
        }
        
        private void EndRead(Action<Exception, int> cb, Exception e)
        {
            _srcEndError = e ?? new Exception("end of read");
            cb?.Invoke(_srcEndError, 0);
            _closeCallback?.Invoke();
        }
    }
}
