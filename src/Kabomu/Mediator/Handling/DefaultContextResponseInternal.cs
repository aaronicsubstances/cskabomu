using Kabomu.Concurrency;
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
            Headers = new DefaultMutableHeadersWrapper(() => rawResponse.Headers, value => rawResponse.Headers = value);
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
            if (!_commitAllowanceHandle.Cancel())
            {
                return false;
            }
            _responseTransmitter.SetResult(RawResponse);
            return true;
        }

        public bool TrySendWithBody(IQuasiHttpBody value)
        {
            if (!_commitAllowanceHandle.Cancel())
            {
                return false;
            }
            RawResponse.Body = value;
            _responseTransmitter.SetResult(RawResponse);
            return true;
        }

        public void Send()
        {
            if (!TrySend())
            {
                throw new ResponseCommittedException();
            }
        }

        public void SendWithBody(IQuasiHttpBody value)
        {
            if (!TrySendWithBody(value))
            {
                throw new ResponseCommittedException();
            }
        }
    }
}
