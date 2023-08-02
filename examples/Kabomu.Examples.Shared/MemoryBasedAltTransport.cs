using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Examples.Shared
{
    public class MemoryBasedAltTransport : IQuasiHttpAltTransport
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
            return await Application.ProcessRequest(request);
        }

        public void CancelSendRequest(object sendCancellationHandle)
        {
            // do nothing.
        }
    }
}
