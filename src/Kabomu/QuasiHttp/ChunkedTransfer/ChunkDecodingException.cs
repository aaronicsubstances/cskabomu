using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.ChunkedTransfer
{
    /// <summary>
    /// Exception thrown to indicate failure in decoding of byte streams expected to be
    /// encoded according to custom chunked transfer defined in Kabomu library.
    /// </summary>
    public class ChunkDecodingException : KabomuException
    {
        /// <summary>
        /// Creates a new instance with the specified error message.
        /// </summary>
        /// <param name="message">the error message.</param>
        public ChunkDecodingException(string message) : base(message)
        {
        }

        /// <summary>
        /// Creates a new instance with the specified error message and underlying cause of the exception.
        /// </summary>
        /// <param name="message">the error message</param>
        /// <param name="innerException">any underlying cause of this exception</param>
        public ChunkDecodingException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
