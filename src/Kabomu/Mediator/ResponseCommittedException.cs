using System;

namespace Kabomu.Mediator
{
    public class ResponseCommittedException : MediatorQuasiWebException
    {
        public ResponseCommittedException()
        {
        }

        public ResponseCommittedException(string message) : base(message)
        {
        }

        public ResponseCommittedException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}