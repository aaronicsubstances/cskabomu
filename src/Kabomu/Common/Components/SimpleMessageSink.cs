using Kabomu.Common.Abstractions;
using Kabomu.Common.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Kabomu.Common.Components
{
    /// <summary>
    /// Message sink which saves incoming data into memory,
    /// or discards them.
    /// </summary>
    public class SimpleMessageSink : IMessageSink
    {
        private Exception _sinkEndError;

        /// <summary>
        /// Create a new instance.
        /// </summary>
        /// <param name="sinkBuffer">Destination buffer. May be null to indicate incoming data
        /// should be discarded.</param>
        public SimpleMessageSink(MemoryStream sinkBuffer)
        {
            Buffer = sinkBuffer;
        }

        public MemoryStream Buffer { get; }

        public void OnDataWrite(byte[] data, int offset, int length, object fallbackPayload,
            bool isMoreExpected, MessageSinkCallback cb, object cbState)
        {
            if (!ByteUtils.IsValidMessagePayload(data, offset, length))
            {
                throw new ArgumentException("invalid chunk");
            }
            if (fallbackPayload != null)
            {
                throw new ArgumentException("fallback payload not supported");
            }
            if (cb == null)
            {
                throw new ArgumentException("null callback");
            }
            if (_sinkEndError != null)
            {
                throw _sinkEndError;
            }
            Buffer?.Write(data, offset, length);
            cb.Invoke(cbState, null);
        }

        public void OnEndWrite(Exception error)
        {
            if (_sinkEndError != null)
            {
                return;
            }
            _sinkEndError = error ?? new Exception("end of write");
        }
    }
}
