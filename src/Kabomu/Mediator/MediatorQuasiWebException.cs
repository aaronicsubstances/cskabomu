using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Mediator
{
    /// <summary>
    /// Parent exception class for all exceptions thrown in Kabomu.Mediator quasi web framework.
    /// </summary>
    public abstract class MediatorQuasiWebException : Exception
    {
        /// <summary>
        /// Creates an instance without a message.
        /// </summary>
        protected MediatorQuasiWebException()
        {
        }

        /// <summary>
        /// Creates a new instance with given error message.
        /// </summary>
        /// <param name="message">the error message</param>
        protected MediatorQuasiWebException(string message) : base(message)
        {
        }

        /// <summary>
        /// Creates a new instance with given error message and underlying cause of the exception.
        /// </summary>
        /// <param name="message">the error message</param>
        /// <param name="innerException">any underlying cause of this exception</param>
        protected MediatorQuasiWebException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
