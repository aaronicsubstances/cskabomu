using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.Client;
using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.Examples.Shared
{
    public class MemoryBasedTransport : IQuasiHttpAltTransport
    {
        public IDictionary<object, IQuasiHttpApplication> Applications { get; set; }

        public async Task<QuasiHttpSendResponse> ProcessSendRequest(
            object remoteEndpoint,
            IQuasiHttpRequest request,
            IQuasiHttpSendOptions sendOptions)
        {
            var resTask = ProcessSendRequestInternal(remoteEndpoint,
                request, sendOptions);
            return new QuasiHttpSendResponse
            {
                ResponseTask = resTask,
            };
        }

        public async Task<QuasiHttpSendResponse> ProcessSendRequest2(
            object remoteEndpoint,
            Func<IDictionary<string, object>, Task<IQuasiHttpRequest>> requestFunc,
            IQuasiHttpSendOptions sendOptions)
        {
            var request = await requestFunc.Invoke(null);
            if (request == null)
            {
                throw new QuasiHttpRequestProcessingException("no request");
            }
            var resTask = ProcessSendRequestInternal(remoteEndpoint,
                request, sendOptions);
            return new QuasiHttpSendResponse
            {
                ResponseTask = resTask,
                CancellationHandle = new RequestCancellationHandle
                {
                    Request = request
                }
            };
        }

        public async Task CancelSendRequest(object sendCancellationHandle)
        {
            if (sendCancellationHandle is RequestCancellationHandle r)
            {
                await r.Release();
            }
        }

        private async Task<IQuasiHttpResponse> ProcessSendRequestInternal(
            object remoteEndpoint,
            IQuasiHttpRequest request,
            IQuasiHttpSendOptions sendOptions)
        {
            var application = Applications[remoteEndpoint];
            return WrapResponse(await application.ProcessRequest(
                WrapRequest(request)));
        }

        private static IQuasiHttpRequest WrapRequest(
            IQuasiHttpRequest originalRequest)
        {
            // todo: decide with some probability
            // (e.g. 50% in development, 10% in production) not to return
            // originalRequest, but instead to wrap originalRequest,
            // originalRequest.Body and originalRequest.Body.Reader
            // in new instances, to prevent any reliance about their types
            return originalRequest;
        }

        private static IQuasiHttpResponse WrapResponse(
            IQuasiHttpResponse originalResponse)
        {
            // todo: decide with some probability
            // (e.g. 50% in development, 10% in production) not to return
            // originalResponse, but instead to wrap originalResponse,
            // originalResponse.Body and originalResponse.Body.Reader
            // in new instances, to prevent any reliance about their types
            return originalResponse;
        }
    }
}
