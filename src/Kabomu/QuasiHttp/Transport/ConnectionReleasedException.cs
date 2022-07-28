using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.Transport
{
    /// <summary>
    /// Thrown to indicate to reads or writes on a connection of a transport, that the 
    /// connection has been released.
    /// </summary>
    public class ConnectionReleasedException : QuasiHttpException
    {
        /// <summary>
        /// Creates an instance with default message of "connection released".
        /// </summary>
        public ConnectionReleasedException() : this("connection released")
        {

        }

        /// <summary>
        /// Creates a new instance with given error message.
        /// </summary>
        /// <param name="message">the error message</param>
        public ConnectionReleasedException(string message) : base(message)
        {
        }

        /// <summary>
        /// Creates a new instance with given error message and underlying cause of the exception.
        /// </summary>
        /// <param name="message">the error message</param>
        /// <param name="innerException">any underlying cause of this exception</param>
        public ConnectionReleasedException(string message, Exception innerException) :
            base(message, innerException)
        {
        }
    }
}
