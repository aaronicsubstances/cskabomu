﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common.Bodies
{
    public class ChunkDecodingBody : IQuasiHttpBody
    {
        private readonly IQuasiHttpBody _wrappedBody;
        private readonly Action _closeCallback;
        private SubsequentChunk _lastChunk;
        private int _lastChunkUsedBytes;
        private Exception _srcEndError;

        public ChunkDecodingBody(IQuasiHttpBody wrappedBody, Action closeCallback)
        {
            if (wrappedBody == null)
            {
                throw new ArgumentException("null wrapped body");
            }
            _wrappedBody = wrappedBody;
            _closeCallback = closeCallback;
        }

        public long ContentLength => -1;

        public string ContentType => _wrappedBody.ContentType;

        public void ReadBytes(IMutexApi mutex, byte[] data, int offset, int bytesToRead, Action<Exception, int> cb)
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
                // once empty data chunk is seen, return 0 for all subsequent reads.
                if (_lastChunk != null && (_lastChunk.DataLength == 0 || _lastChunkUsedBytes < _lastChunk.DataLength))
                {
                    SupplyFromLastChunk(data, offset, bytesToRead, cb);
                    return;
                }
                var encodedLength = new byte[2];
                TransportUtils.ReadBytesFully(mutex, _wrappedBody,
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
                            var chunkBytes = new byte[chunkLen];
                            TransportUtils.ReadBytesFully(mutex, _wrappedBody,
                                chunkBytes, 0, chunkBytes.Length, e =>
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
                                        _lastChunk = SubsequentChunk.Deserialize(chunkBytes, 0, chunkBytes.Length);
                                        _lastChunkUsedBytes = 0;
                                        SupplyFromLastChunk(data, offset, bytesToRead, cb);
                                    }, null);
                                });
                        }, null);
                    });
            }, null);
        }

        private void SupplyFromLastChunk(byte[] data, int offset, int bytesToRead, Action<Exception, int> cb)
        {
            int lengthToUse = Math.Min(_lastChunk.DataLength - _lastChunkUsedBytes, bytesToRead);
            Array.Copy(_lastChunk.Data, _lastChunk.DataOffset + _lastChunkUsedBytes, data, offset, lengthToUse);
            _lastChunkUsedBytes += lengthToUse;
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

