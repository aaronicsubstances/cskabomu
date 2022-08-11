using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.EntityBody;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Mediator.Handling
{
    internal class DefaultContextResponse : IResponse
    {
        private readonly TaskCompletionSource<IQuasiHttpResponse> _responseTransmitter;

        public DefaultContextResponse(IQuasiHttpMutableResponse rawResponse,
            TaskCompletionSource<IQuasiHttpResponse> responseTransmitter)
        {
            RawResponse = rawResponse ?? throw new ArgumentNullException(nameof(rawResponse));
            _responseTransmitter = responseTransmitter ?? throw new ArgumentNullException(nameof(responseTransmitter));
            Headers = new DefaultMutableHeadersWrapper(rawResponse);
        }

        public IQuasiHttpMutableResponse RawResponse { get; }

        public bool StatusIndicatesSuccess => RawResponse.StatusIndicatesSuccess;

        public bool StatusIndicatesClientError => RawResponse.StatusIndicatesClientError;

        public IMutableHeaders Headers { get; }

        public Task Send()
        {
            _responseTransmitter.SetResult(RawResponse);
            return Task.CompletedTask;
        }

        public Task SendWithBody(IQuasiHttpBody value)
        {
            RawResponse.Body = value;
            return Send();
        }

        public IResponse SetBody(IQuasiHttpBody value)
        {
            RawResponse.Body = value;
            return this;
        }

        public IResponse SetStatusIndicatesClientError(bool value)
        {
            RawResponse.StatusIndicatesClientError = value;
            return this;
        }

        public IResponse SetStatusIndicatesSuccess(bool value)
        {
            RawResponse.StatusIndicatesSuccess = value;
            return this;
        }

        public IResponse SetStatusMessage(string value)
        {
            RawResponse.StatusMessage = value;
            return this;
        }
        
        class DefaultMutableHeadersWrapper : IMutableHeaders
        {
            private readonly IQuasiHttpMutableResponse _parent;

            public DefaultMutableHeadersWrapper(IQuasiHttpMutableResponse parent)
            {
                _parent = parent;
            }

            private IDictionary<string, IList<string>> GetRawHeaders(bool createIfNecessary)
            {
                var rawHeaders = _parent.Headers;
                if (rawHeaders == null && createIfNecessary)
                {
                    rawHeaders = new Dictionary<string, IList<string>>();
                    _parent.Headers = rawHeaders;
                }
                return rawHeaders;
            }

            public string Get(string name)
            {
                var rawHeaders = GetRawHeaders(false);
                if (rawHeaders != null)
                {
                    IList<string> values = null;
                    if (rawHeaders.ContainsKey(name))
                    {
                        values = rawHeaders[name];
                    }
                    if (values != null && values.Count > 0)
                    {
                        return values[0];
                    }
                }
                return null;
            }

            public IMutableHeaders Add(string name, string value)
            {
                var rawHeaders = GetRawHeaders(true);
                IList<string> values;
                if (rawHeaders.ContainsKey(name))
                {
                    values = rawHeaders[name];
                }
                else
                {
                    values = new List<string>();
                    rawHeaders.Add(name, values);
                }
                values.Add(value);
                return this;
            }

            public IMutableHeaders Clear()
            {
                var rawHeaders = GetRawHeaders(false);
                rawHeaders?.Clear();
                return this;
            }

            public IMutableHeaders Remove(string name)
            {
                var rawHeaders = GetRawHeaders(false);
                rawHeaders?.Remove(name);
                return this;
            }

            public IMutableHeaders Set(string name, string value)
            {
                var rawHeaders = GetRawHeaders(true);
                rawHeaders.Remove(name);
                rawHeaders.Add(name, new List<string> { value });
                return this;
            }

            public IMutableHeaders Set(string name, IEnumerable<string> values)
            {
                var rawHeaders = GetRawHeaders(true);
                rawHeaders.Remove(name);
                rawHeaders.Add(name, new List<string>(values));
                return this;
            }
        }
    }
}
