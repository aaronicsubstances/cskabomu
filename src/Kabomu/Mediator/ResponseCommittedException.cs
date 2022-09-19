using System;

namespace Kabomu.Mediator
{
    /// <summary>
    /// Thrown to indicate that an instance of the <see cref="Handling.IContextResponse"/> class has been
    /// committed, ie that its <see cref="Handling.IContextResponse.Send"/> or <see cref="Handling.IContextResponse.TrySend"/>
    /// methods have been invoked.
    /// </summary>
    public class ResponseCommittedException : MediatorQuasiWebException
    {
        /// <summary>
        /// Creates an instance with default error message.
        /// </summary>
        public ResponseCommittedException():
            base("quasi http response has already been committed")
        {
        }

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