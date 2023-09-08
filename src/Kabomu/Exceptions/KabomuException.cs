using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Exceptions
{
    /// <summary>
    /// Base exception class for errors encountered in the library.
    /// </summary>
    public abstract class KabomuException : Exception
    {
        /// <summary>
        /// Creates new instance.
        /// </summary>
        public KabomuException()
        {

        }

        /// <summary>
        /// Creates a new instance with specified message.
        /// </summary>
        /// <param name="message">error message</param>
        public KabomuException(string message) : base(message)
        {
        }

        /// <summary>
        /// Creates a new instance with specified message and any cause of the exception.
        /// </summary>
        /// <param name="message">error message</param>
        /// <param name="innerException">cause of this exception</param>
        public KabomuException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
