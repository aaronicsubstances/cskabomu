using System;

namespace Kabomu.Mediator.RequestParsing
{
    public class RequestParsingException : MediatorQuasiWebException
    {
        public RequestParsingException()
        {
        }

        public RequestParsingException(string message) : base(message)
        {
        }

        public RequestParsingException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}