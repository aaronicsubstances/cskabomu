using Kabomu.Common.Abstractions;
using Kabomu.Common.Helpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common.Components
{
    public class SimpleMessageSource : IMessageSource
    {
        private Exception _srcEndError;
        private int _nextOffset;

        public SimpleMessageSource(byte[] data)
            : this(data, 0, data?.Length ?? 0)
        { }

        public SimpleMessageSource(byte[] data, int offset, int length)
        {
            if (!ByteUtils.IsValidMessagePayload(data, offset, length))
            {
                throw new ArgumentException("invalid buffer");
            }
            Buffer = data;
            Offset = offset;
            Length = length;
            _nextOffset = offset;
        }

        public byte[] Buffer { get; }

        public int Offset { get; }

        public int Length { get; }

        public void OnDataRead(MessageSourceCallback cb, object cbState)
        {
            if (cb == null)
            {
                throw new ArgumentException("null callback");
            }
            if (_srcEndError != null)
            {
                throw _srcEndError;
            }
            // simply send all of data without sending in bits or chunks.
            var offsetToUse = _nextOffset;
            var lengthToUse = Offset + Length - offsetToUse;
            _nextOffset += lengthToUse;
            cb.Invoke(cbState, null, Buffer, offsetToUse, lengthToUse, null, false);
        }

        public void OnEndRead(Exception error)
        {
            if (_srcEndError != null)
            {
                return;
            }
            _srcEndError = error ?? new Exception("end of read");
        }
    }
}
