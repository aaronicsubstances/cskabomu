using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Kabomu.QuasiHttp
{
    public class QuasiHttpException : Exception
    {
        public QuasiHttpException(IQuasiHttpResponse response)
        {
            Response = response;
        }

        public QuasiHttpException(string message, IQuasiHttpResponse response) : 
            base(message)
        {
            Response = response;
        }

        public QuasiHttpException(string message, IQuasiHttpResponse response, Exception innerException) : 
            base(message, innerException)
        {
            Response = response;
        }

        public IQuasiHttpResponse Response { get; }
    }
}
