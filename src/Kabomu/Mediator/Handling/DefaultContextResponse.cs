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
            Headers = new DefaultMutableHeadersWrapper(() => rawResponse.Headers, v => rawResponse.Headers = v);
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
    }
}
