using Kabomu.Common;
using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.EntityBody;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Mediator.Handling
{
    internal class DefaultContextResponseInternal : IContextResponse
    {
        private readonly TaskCompletionSource<IQuasiHttpResponse> _responseTransmitter;
        private readonly ICancellationHandle _commitAllowanceHandle = new DefaultCancellationHandle();

        public DefaultContextResponseInternal(IQuasiHttpMutableResponse rawResponse,
            TaskCompletionSource<IQuasiHttpResponse> responseTransmitter)
        {
            RawResponse = rawResponse ?? throw new ArgumentNullException(nameof(rawResponse));
            _responseTransmitter = responseTransmitter ?? throw new ArgumentNullException(nameof(responseTransmitter));
            Headers = new DefaultMutableHeadersWrapper(createIfNecessary => 
            {
                if (createIfNecessary && rawResponse.Headers == null)
                {
                    rawResponse.Headers = new Dictionary<string, IList<string>>();
                }
                return rawResponse.Headers;
            });
        }

        public IQuasiHttpMutableResponse RawResponse { get; }

        public int StatusCode => RawResponse.StatusCode;

        public bool IsSuccessStatusCode => RawResponse.StatusCode >= 200 && RawResponse.StatusCode <= 299;

        public bool IsClientErrorStatusCode => RawResponse.StatusCode >= 400 && RawResponse.StatusCode <= 499;

        public bool IsServerErrorStatusCode => RawResponse.StatusCode >= 500 && RawResponse.StatusCode <= 599;

        public IQuasiHttpBody Body => RawResponse.Body;

        public IMutableHeadersWrapper Headers { get; }

        public IContextResponse SetSuccessStatusCode()
        {
            RawResponse.StatusCode = 200;
            return this;
        }

        public IContextResponse SetClientErrorStatusCode()
        {
            RawResponse.StatusCode = 400;
            return this;
        }

        public IContextResponse SetServerErrorStatusCode()
        {
            RawResponse.StatusCode = 500;
            return this;
        }

        public IContextResponse SetStatusCode(int value)
        {
            RawResponse.StatusCode = value;
            return this;
        }

        public IContextResponse SetBody(IQuasiHttpBody value)
        {
            RawResponse.Body = value;
            return this;
        }

        public bool TrySend()
        {
            return TrySend(null);
        }

        public bool TrySend(Action changesCb)
        {
            if (!_commitAllowanceHandle.Cancel())
            {
                return false;
            }
            changesCb?.Invoke();
            _responseTransmitter.SetResult(RawResponse);
            return true;
        }

        public void Send()
        {
            Send(null);
        }

        public void Send(Action changesCb)
        {
            if (!TrySend(changesCb))
            {
                throw new ResponseCommittedException("quasi http response has already been sent");
            }
        }
    }
}
