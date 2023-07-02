using Kabomu.Examples.Shared;
using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Memory.FileExchange
{
    public class CustomMemoryBasedTransport : IQuasiHttpAltTransport
    {
        private readonly FileReceiver _application;

        public CustomMemoryBasedTransport(string endpoint, string downloadDirPath)
        {
            _application = new FileReceiver(endpoint, downloadDirPath);
        }

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
            var request = await requestFunc.Invoke(null);
            if (request == null)
            {
                throw new QuasiHttpRequestProcessingException("no request");
            }
            return await _application.ProcessRequest(request);
        }

        public void CancelSendRequest(object sendCancellationHandle)
        {
            // do nothing.
        }
    }
}
