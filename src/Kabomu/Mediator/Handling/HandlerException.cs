using System;

namespace Kabomu.Mediator.Handling
{
    /// <summary>
    /// Indicates that an instance of <see cref="Handler"/> threw an exception during handle delegation by 
    /// an instance of <see cref="IContext"/> class.
    /// </summary>
    public class HandlerException : MediatorQuasiWebException
    {
        /// <summary>
        /// Creates a new instance with given error message.
        /// </summary>
        /// <param name="message">the error message</param>
        public HandlerException(string message) : base(message)
        {
        }

        /// <summary>
        /// Creates a new instance with given error message and underlying cause of the exception.
        /// </summary>
        /// <param name="message">the error message</param>
        /// <param name="innerException">any underlying cause of this exception</param>
        public HandlerException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}