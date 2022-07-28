﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.Transport
{
    /// <summary>
    /// Thrown to indicate to a transport operation which requires the transport to be started and running,
    /// that the transport is yet to be started or is not running.
    /// </summary>
    public class TransportNotStartedException : QuasiHttpException
    {
        /// <summary>
        /// Creates an instance with default message of "transport not started".
        /// </summary>
        public TransportNotStartedException():
            this("transport not started")
        {

        }

        /// <summary>
        /// Creates a new instance with given error message.
        /// </summary>
        /// <param name="message">the error message</param>
        public TransportNotStartedException(string message) : base(message)
        {
        }

        /// <summary>
        /// Creates a new instance with given error message and underlying cause of the exception.
        /// </summary>
        /// <param name="message">the error message</param>
        /// <param name="innerException">any underlying cause of this exception</param>
        public TransportNotStartedException(string message, Exception innerException) :
            base(message, innerException)
        {
        }
    }
}
