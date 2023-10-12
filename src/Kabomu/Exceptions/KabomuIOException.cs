using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Exceptions
{
    /// <summary>
    /// Represents errors encountered when reading from or writing to byte streams.
    /// </summary>
    public class KabomuIOException : KabomuException
    {
        /// <summary>
        /// Creates a new instance with specified error message.
        /// </summary>
        /// <param name="message">error message</param>
        public KabomuIOException(string message) : base(message)
        {
        }

        /// <summary>
        /// Creates a new instance with specified error message and
        /// underlying cause of error
        /// </summary>
        /// <param name="message">error message</param>
        /// <param name="innerException">cause of error</param>
        public KabomuIOException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Creates error indicating that reading from a stream has
        /// unexpectedly ended.
        /// </summary>
        public static KabomuIOException CreateEndOfReadError()
        {
            return new KabomuIOException("unexpected end of read");
        }
    }
}
