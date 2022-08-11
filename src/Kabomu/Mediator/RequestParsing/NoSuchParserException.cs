using System;
using System.Runtime.Serialization;

namespace Kabomu.Mediator.RequestParsing
{
    public class NoSuchParserException : Exception
    {
        public NoSuchParserException()
        {
        }

        public NoSuchParserException(string message) : base(message)
        {
        }

        public NoSuchParserException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}