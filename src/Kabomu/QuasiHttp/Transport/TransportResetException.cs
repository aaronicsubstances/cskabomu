using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.Transport
{
    /// <summary>
    /// Thrown to indicate to pending receive connection requests on an instance of <see cref="IQuasiHttpServerTransport"/>
    /// class, that the instance has been stopped and so any pending receive request cannot be honoured.
    /// </summary>
    public class TransportResetException : QuasiHttpException
    {
        /// <summary>
        /// Creates an instance with default message of "transport stopped".
        /// </summary>
        public TransportResetException() :
            this("transport stopped")
        {

        }

        /// <summary>
        /// Creates a new instance with given error message.
        /// </summary>
        /// <param name="message">the error message</param>
        public TransportResetException(string message) : base(message)
        {
        }

        /// <summary>
        /// Creates a new instance with given error message and underlying cause of the exception.
        /// </summary>
        /// <param name="message">the error message</param>
        /// <param name="innerException">any underlying cause of this exception</param>
        public TransportResetException(string message, Exception innerException) :
            base(message, innerException)
        {
        }
    }
}
