using System;
using System.Runtime.Serialization;

namespace Kabomu.QuasiHttp.Exceptions
{
    /// <summary>
    /// Thrown by IQuasiHttpBody implementations to cause Read() calls following EndOfRead() calls to fail.
    /// </summary>
    public class EndOfReadException : QuasiHttpException
    {
        /// <summary>
        /// Creates an instance with default message of "end of read"
        /// </summary>
        public EndOfReadException() :
            this("end of read")
        {
        }

        /// <summary>
        /// Creates a new instance with given error message.
        /// </summary>
        /// <param name="message">the error message</param>
        public EndOfReadException(string message) : base(message)
        {
        }

        /// <summary>
        /// Creates a new instance with given error message and underlying cause of the exception.
        /// </summary>
        /// <param name="message">the error message</param>
        /// <param name="innerException">any underlying cause of this exception</param>
        public EndOfReadException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}