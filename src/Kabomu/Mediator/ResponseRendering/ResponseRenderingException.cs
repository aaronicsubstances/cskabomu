using System;

namespace Kabomu.Mediator.ResponseRendering
{
    public class ResponseRenderingException : MediatorQuasiWebException
    {
        public ResponseRenderingException()
        {
        }

        public ResponseRenderingException(string message) : base(message)
        {
        }

        public ResponseRenderingException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}