using System;
using System.Runtime.Serialization;

namespace Kabomu.Mediator.RequestParsing
{
    [Serializable]
    internal class NoSuchParserException : Exception
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

        protected NoSuchParserException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}