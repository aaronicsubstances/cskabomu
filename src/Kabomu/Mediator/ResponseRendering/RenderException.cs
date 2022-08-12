using System;

namespace Kabomu.Mediator.ResponseRendering
{
    public class RenderException : MediatorQuasiWebException
    {
        public RenderException()
        {
        }

        public RenderException(string message) : base(message)
        {
        }

        public RenderException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}