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

        public int StatusCode => RawResponse.StatusCode;

        public bool IsSuccessStatusCode => RawResponse.StatusCode >= 200 && RawResponse.StatusCode <= 299;

        public bool IsClientErrorStatusCode => RawResponse.StatusCode >= 400 && RawResponse.StatusCode <= 499;

        public bool IsServerErrorStatusCode => RawResponse.StatusCode >= 500 && RawResponse.StatusCode <= 599;
        
        public IResponse SetSuccessStatusCode()
        {
            RawResponse.StatusCode = 200;
            return this;
        }

        public IResponse SetClientErrorStatusCode()
        {
            RawResponse.StatusCode = 400;
            return this;
        }

        public IResponse SetServerErrorStatusCode()
        {
            RawResponse.StatusCode = 500;
            return this;
        }

        public IResponse SetStatusCode(int value)
        {
            RawResponse.StatusCode = value;
            return this;
        }

        public IResponse SetBody(IQuasiHttpBody value)
        {
            RawResponse.Body = value;
            return this;
        }

        public Task<bool> TrySend()
        {
            var replySucceeded = _responseTransmitter.TrySetResult(RawResponse);
            return Task.FromResult(replySucceeded);
        }

        public Task<bool> TrySendWithBody(IQuasiHttpBody value)
        {
            RawResponse.Body = value;
            return TrySend();
        }

        public IMutableHeaders Headers { get; }

        public async Task Send()
        {
            if (await TrySend())
            {
                return;
            }
            throw new ResponseCommittedException();
        }

        public async Task SendWithBody(IQuasiHttpBody value)
        {
            if (await TrySendWithBody(value))
            {
                return;
            }
            throw new ResponseCommittedException();
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
