using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Kabomu.Common
{
    /// <summary>
    /// Exception that is thrown by clients to indicate that a required dependency (often a property of the client)
    /// has not been set up properly for use (often because the property is null).
    /// </summary>
    public class MissingDependencyException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MissingDependencyException"/> class with a string
        /// describing the expected dependency.
        /// </summary>
        /// <param name="message">description of expected dependency</param>
        public MissingDependencyException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MissingDependencyException"/> class with a specified error 
        /// message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">description of expected dependency</param>
        /// <param name="innerException">cause of exception</param>
        public MissingDependencyException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected MissingDependencyException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
