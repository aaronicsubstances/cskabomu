using System;
using System.Runtime.Serialization;

namespace Kabomu.Mediator.Handling
{
    [Serializable]
    internal class HandlerException : Exception
    {
        private Exception e;

        public HandlerException()
        {
        }

        public HandlerException(Exception e)
        {
            this.e = e;
        }

        public HandlerException(string message) : base(message)
        {
        }

        public HandlerException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected HandlerException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}