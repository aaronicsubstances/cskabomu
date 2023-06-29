using System;

namespace Kabomu.QuasiHttp.Exceptions
{
    /// <summary>
    /// Thrown by IQuasiHttpBody implementations which serve Read() calls from some form of write calls,
    /// when there is some indication that the last write call has been made.
    /// </summary>
    public class EndOfWriteException : QuasiHttpException
    {
        /// <summary>
        /// Creates an instance with default message of "end of write"
        /// </summary>
        public EndOfWriteException() :
            this("end of write")
        {
        }

        /// <summary>
        /// Creates a new instance with given error message.
        /// </summary>
        /// <param name="message">the error message</param>
        public EndOfWriteException(string message) : base(message)
        {
        }

        /// <summary>
        /// Creates a new instance with given error message and underlying cause of the exception.
        /// </summary>
        /// <param name="message">the error message</param>
        /// <param name="innerException">any underlying cause of this exception</param>
        public EndOfWriteException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}