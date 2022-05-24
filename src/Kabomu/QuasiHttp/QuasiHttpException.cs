using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Kabomu.QuasiHttp
{
    public class QuasiHttpException : Exception
    {
        public QuasiHttpException(IQuasiHttpResponseMessage response)
        {
            Response = response;
        }

        public QuasiHttpException(string message, IQuasiHttpResponseMessage response) : 
            base(message)
        {
            Response = response;
        }

        public QuasiHttpException(string message, IQuasiHttpResponseMessage response, Exception innerException) : 
            base(message, innerException)
        {
            Response = response;
        }

        public IQuasiHttpResponseMessage Response { get; }
    }
}
