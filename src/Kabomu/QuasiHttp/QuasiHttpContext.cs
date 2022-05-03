using System;
using System.Collections.Generic;

namespace Kabomu.QuasiHttp
{
    public class QuasiHttpContext
    {
        private QuasiHttpResponseMessage _response;
        private Exception _error;
        private bool _responseMarkedAsSent;

        public QuasiHttpContext(QuasiHttpRequestMessage request)
        {
            Request = request;
            RequestAttributes = new Dictionary<string, object>();
        }

        public QuasiHttpRequestMessage Request { get; }
        public Dictionary<string, object> RequestAttributes { get; }

        public QuasiHttpResponseMessage Response
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

        public bool ResponseMarkedAsSent
        {

            get
            {
                return _responseMarkedAsSent;
            }
            internal set
            {
                if (!value)
                {
                    throw new ArgumentException("can only mark response as sent", nameof(ResponseMarkedAsSent));
                }
                _responseMarkedAsSent = value;
            }
        }
    }
}