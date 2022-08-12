using System;

namespace Kabomu.Mediator.Handling
{
    public class HandlerException : MediatorQuasiWebException
    {
        public HandlerException()
        {
        }

        public HandlerException(string message) : base(message)
        {
        }

        public HandlerException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}