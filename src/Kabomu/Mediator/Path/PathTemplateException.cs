using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Mediator.Path
{
    /// <summary>
    /// Thrown to indicate errors which occur during path template generation or usage in matching or interpolation.
    /// </summary>
    public class PathTemplateException : MediatorQuasiWebException
    {
        /// <summary>
        /// Creates a new instance with given error message.
        /// </summary>
        /// <param name="message">the error message</param>
        public PathTemplateException(string message) : base(message)
        {
        }

        /// <summary>
        /// Creates a new instance with given error message and underlying cause of the exception.
        /// </summary>
        /// <param name="message">the error message</param>
        /// <param name="innerException">any underlying cause of this exception</param>
        public PathTemplateException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
