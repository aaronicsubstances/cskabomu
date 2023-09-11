﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Exceptions
{
    /// <summary>
    /// Represents errors encountered when reading from or writing to byte streams.
    /// </summary>
    public class CustomIOException : KabomuException
    {
        /// <summary>
        /// Creates a new instance with specified error message.
        /// </summary>
        /// <param name="message">error message</param>
        public CustomIOException(string message) : base(message)
        {
        }

        /// <summary>
        /// Creates a new instance with specified error message and
        /// underlying cause of error
        /// </summary>
        /// <param name="message">error message</param>
        /// <param name="innerException">cause of error</param>
        public CustomIOException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Creates error indicating that a number of bytes
        /// indicated by quasi http content length could not be fully
        /// read from a reader or source of bytes.
        /// </summary>
        /// <param name="contentLength">content length to include in error message</param>
        /// <param name="remainingBytesToRead">remaining bytes to read</param>
        public static CustomIOException CreateContentLengthNotSatisfiedError(long contentLength,
            long remainingBytesToRead)
        {
            return new CustomIOException($"insufficient bytes available to satisfy " +
                $"content length of {contentLength} bytes (could not read remaining " +
                $"{remainingBytesToRead} bytes before end of read)");
        }
    }
}