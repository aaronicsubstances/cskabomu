using System;

namespace Kabomu.Mediator
{
    /// <summary>
    /// Thrown to indicate that an instance of the <see cref="Handling.IContextResponse"/> class has been
    /// committed, ie that one of its Send* methods have been invoked.
    /// </summary>
    public class ResponseCommittedException : MediatorQuasiWebException
    {
        /// <summary>
        /// Creates a new instance with given error message.
        /// </summary>
        /// <param name="message">the error message</param>
        public ResponseCommittedException(string message) : base(message)
        {
        }

        /// <summary>
        /// Creates a new instance with given error message and underlying cause of the exception.
        /// </summary>
        /// <param name="message">the error message</param>
        /// <param name="innerException">any underlying cause of this exception</param>
        public ResponseCommittedException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}