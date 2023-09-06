using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Exceptions
{
    /// <summary>
    /// Exception thrown to indicate that the caller of a method or function didn't find the output or outcome
    /// satisfactory. E.g. the return value from a function is invalid; the function took too long to complete.
    /// </summary>
    public class ExpectationViolationException : KabomuException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExpectationViolationException"/> class with a string
        /// describing the violated expectation.
        /// </summary>
        /// <param name="message">description of expectation which has been violated</param>
        public ExpectationViolationException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpectationViolationException"/> class with a specified error 
        /// message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">description of expectation which has been violated</param>
        /// <param name="innerException">cause of exception</param>
        public ExpectationViolationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
