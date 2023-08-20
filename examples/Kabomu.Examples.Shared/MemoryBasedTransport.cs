using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.Client;
using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Examples.Shared
{
    public class MemoryBasedTransport : IQuasiHttpAltTransport
    {
        public IQuasiHttpApplication Application { get; set; }

        public (Task<IQuasiHttpResponse>, object) ProcessSendRequest(
            object remoteEndpoint,
            IQuasiHttpRequest request,
            IQuasiHttpSendOptions sendOptions)
        {
            return ProcessSendRequest(remoteEndpoint,
                _ => Task.FromResult(request),
                sendOptions);
        }

        public (Task<IQuasiHttpResponse>, object) ProcessSendRequest(
            object remoteEndpoint,
            Func<IDictionary<string, object>, Task<IQuasiHttpRequest>> requestFunc,
            IQuasiHttpSendOptions sendOptions)
        {
            var resTask = ProcessSendRequestInternal(remoteEndpoint, requestFunc,
                sendOptions);
            return (resTask, null);
        }

        public async Task<IQuasiHttpResponse> ProcessSendRequestInternal(
            object remoteEndpoint,
            Func<IDictionary<string, object>, Task<IQuasiHttpRequest>> requestFunc,
            IQuasiHttpSendOptions sendOptions)
        {
            var request = await requestFunc.Invoke(sendOptions?.ExtraConnectivityParams);
            if (request == null)
            {
                throw new QuasiHttpRequestProcessingException("no request");
            }
            // todo: ensure disposal of request if it was retrieved
            // from externally supplied request func.
            return WrapResponse(await Application.ProcessRequest(
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

        public void CancelSendRequest(object sendCancellationHandle)
        {
            // do nothing.
        }
    }
}
