using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Common
{
    /// <summary>
    /// Represents errors usable by implementations of custom readers and writers,
    /// as well as errors encountered by <see cref="IOUtils"/> class.
    /// </summary>
    public class CustomIOException : KabomuException
    {
        /// <summary>
        /// Creates a new instance with specified error message.
        /// </summary>
        /// <param name="message">error message</param>
        public CustomIOException(string message) : base(message)
        {
        }

        /// <summary>
        /// Creates a new instance with specified error message and
        /// underlying cause of error
        /// </summary>
        /// <param name="message">error message</param>
        /// <param name="innerException">cause of error</param>
        public CustomIOException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
         /// Creates error indicating that a number of bytes
         /// indicated by quasi http content length could not be fully
         /// read from a reader or source of bytes.
         /// </summary>
         /// <param name="contentLength">content length to include in error message</param>
        public static CustomIOException CreateContentLengthNotSatisfiedError(long contentLength)
        {
            return new CustomIOException($"insufficient bytes available to satisfy " +
                $"content length of {contentLength} bytes (could not read remaining " +
                $"{{remainingBytesToRead}} bytes before end of read)");
        }
        
        /// <summary>
         /// Creates error indicating overflow of a byte buffer with
         /// an imposed maximum limit.
         /// </summary>
         /// <param name="bufferSizeLimit">maximum data buffer limit to include in error message</param>
        public static CustomIOException CreateDataBufferLimitExceededErrorMessage(int bufferSizeLimit)
        {
            return new CustomIOException($"data buffer size limit of " +
                $"{bufferSizeLimit} bytes exceeded");
        }
    }
}
