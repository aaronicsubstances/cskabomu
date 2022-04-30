using System;
using System.Collections.Generic;

namespace Kabomu.QuasiHttp
{
    public class QuasiHttpContext
    {
        private object _response;
        private Exception _error;

        public QuasiHttpContext(QuasiHttpRequestMessage request)
        {
            Request = request;
            RequestAttributes = new Dictionary<string, object>();
        }

        public QuasiHttpRequestMessage Request { get; }
        public Dictionary<string, object> RequestAttributes { get; }

        public object Response
        {
            get
            {
                return _response;
            }
            internal set
            {
                _response = value ?? throw new ArgumentNullException(nameof(Response));
            }
        }

        public Exception Error
        {
            get
            {
                return _error;
            }
            internal set
            {
                _error = value ?? throw new ArgumentNullException(nameof(Error));
            }
        }
    }
}