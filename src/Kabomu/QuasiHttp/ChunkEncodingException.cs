using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp
{
    /// <summary>
    /// Exception thrown to indicate failure in encoding byte streams
    /// according to custom chunked transfer defined in Kabomu library.
    /// </summary>
    public class ChunkEncodingException : QuasiHttpRequestProcessingException
    {
        /// <summary>
        /// Creates a new instance with the specified error message.
        /// </summary>
        /// <param name="message">the error message.</param>
        public ChunkEncodingException(string message) : base(message)
        {
        }

        /// <summary>
        /// Creates a new instance with the specified error message and underlying cause of the exception.
        /// </summary>
        /// <param name="message">the error message</param>
        /// <param name="innerException">any underlying cause of the exception</param>
        public ChunkEncodingException(string message, Exception innerException) :
            base(message, ReasonCodeGeneral, innerException)
        {
        }
    }
}
