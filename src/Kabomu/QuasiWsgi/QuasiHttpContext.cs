using Kabomu.Common;
using System;
using System.Collections.Generic;

namespace Kabomu.QuasiWsgi
{
    public class QuasiHttpContext
    {
        private IQuasiHttpResponseMessage _response;
        private Exception _error;
        private bool _responseMarkedAsSent;

        public QuasiHttpContext(IQuasiHttpRequestMessage request)
        {
            Request = request;
            RequestAttributes = new Dictionary<string, object>();
        }

        public IQuasiHttpRequestMessage Request { get; }
        public Dictionary<string, object> RequestAttributes { get; }

        public IQuasiHttpResponseMessage Response
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