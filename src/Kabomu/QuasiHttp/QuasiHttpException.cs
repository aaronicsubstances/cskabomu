using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp
{
    /// <summary>
    /// Base exception class for errors encountered during quasi http exchanges.
    /// </summary>
    public abstract class QuasiHttpException : Exception
    {
        /// <summary>
        /// Creates new instance.
        /// </summary>
        public QuasiHttpException()
        {

        }

        /// <summary>
        /// Creates a new instance with specified message.
        /// </summary>
        /// <param name="message">error message</param>
        public QuasiHttpException(string message) : base(message)
        {
        }

        /// <summary>
        /// Creates a new instance with specified message and any cause of the exception.
        /// </summary>
        /// <param name="message">error message</param>
        /// <param name="innerException">cause of this exception</param>
        public QuasiHttpException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
