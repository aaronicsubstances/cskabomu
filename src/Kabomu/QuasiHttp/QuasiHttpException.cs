using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Kabomu.QuasiHttp
{
    public class QuasiHttpException : Exception
    {
        public QuasiHttpException(QuasiHttpResponseMessage response)
        {
            Response = response;
        }

        public QuasiHttpException(string message, QuasiHttpResponseMessage response) : 
            base(message)
        {
            Response = response;
        }

        public QuasiHttpException(string message, QuasiHttpResponseMessage response, Exception innerException) : 
            base(message, innerException)
        {
            Response = response;
        }

        public QuasiHttpResponseMessage Response { get; }
    }
}
