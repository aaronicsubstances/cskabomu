using System;
using System.Runtime.Serialization;

namespace Kabomu.QuasiHttp.EntityBody
{
    public class EndOfReadException : QuasiHttpException
    {
        public EndOfReadException():
            this("end of read")
        {
        }

        public EndOfReadException(string message) : base(message)
        {
        }

        public EndOfReadException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}