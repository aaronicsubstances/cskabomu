using System;

namespace Kabomu.Mediator.RequestParsing
{
    public class ParseException : MediatorQuasiWebException
    {
        public ParseException()
        {
        }

        public ParseException(string message) : base(message)
        {
        }

        public ParseException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}