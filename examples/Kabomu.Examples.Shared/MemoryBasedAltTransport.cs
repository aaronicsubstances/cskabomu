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
            IQuasiHttpRequest request, IConnectivityParams connectivityParams)
        {
            return ProcessSendRequest(_ => Task.FromResult(request),
                connectivityParams);
        }

        public (Task<IQuasiHttpResponse>, object) ProcessSendRequest(
            Func<IDictionary<string, object>, Task<IQuasiHttpRequest>> requestFunc,
            IConnectivityParams connectivityParams)
        {
            var resTask = ProcessSendRequestInternal(requestFunc, connectivityParams);
            return (resTask, null);
        }

        public async Task<IQuasiHttpResponse> ProcessSendRequestInternal(
            Func<IDictionary<string, object>, Task<IQuasiHttpRequest>> requestFunc,
            IConnectivityParams connectivityParams)
        {
            var request = await requestFunc.Invoke(connectivityParams?.ExtraParams);
            if (request == null)
            {
                throw new QuasiHttpRequestProcessingException("no request");
            }
            return await Application.ProcessRequest(request);
        }

        public void CancelSendRequest(object sendCancellationHandle)
        {
            // do nothing.
        }
    }
}
