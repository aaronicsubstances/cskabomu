using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.Exceptions
{
    /// <summary>
    /// Exception thrown when chunk decoding of data from transport connections and quasi http body fail.
    /// </summary>
    public class ChunkDecodingException : QuasiHttpException
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
