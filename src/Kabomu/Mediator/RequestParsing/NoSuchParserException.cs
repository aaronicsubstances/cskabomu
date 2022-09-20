using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Mediator.RequestParsing
{
    /// <summary>
    /// Thrown by <see cref="Handling.ContextExtensions.ParseRequest"/> method when an 
    /// appropriate request parser is not found in instance of <see cref="Handling.IContext"/> class.
    /// </summary>
    public class NoSuchParserException : MediatorQuasiWebException
    {
        /// <summary>
        /// Creates a new instance with a default error message.
        /// </summary>
        public NoSuchParserException() :
            base("No appropriate request parser found")
        {
        }

        /// <summary>
        /// Creates a new instance with given error message.
        /// </summary>
        /// <param name="message">the error message</param>
        public NoSuchParserException(string message) : base(message)
        {

        }
    }
}
