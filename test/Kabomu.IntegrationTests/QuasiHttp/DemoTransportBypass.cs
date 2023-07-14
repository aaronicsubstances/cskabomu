using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.IntegrationTests.QuasiHttp
{
    public class DemoTransportBypass : IQuasiHttpAltTransport
    {
        private readonly object _mutex = new object();
        private bool _cancellationRequested;

        public Func<IQuasiHttpRequest, Task<IQuasiHttpResponse>> SendRequestCallback { get; set; }

        public bool CreateCancellationHandles { get; set; }
        
        public bool IsCancellationRequested
        {
            get
            {
                lock (_mutex)
                {
                    return _cancellationRequested;
                }
            }
        }

        public void CancelSendRequest(object sendCancellationHandle)
        {
            lock (_mutex)
            {
                _cancellationRequested = true;
            }
        }

        private async Task<IQuasiHttpResponse> ProcessSendRequestX(
            Func<IDictionary<string, object>, Task<IQuasiHttpRequest>> requestFunc,
            IConnectivityParams connectivityParams)
        {
            var request = await requestFunc(null);
            return await SendRequestCallback(request);
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
            var task = ProcessSendRequestX(requestFunc, connectivityParams);
            return (task, CreateCancellationHandles ?
                new object() : null);
        }
    }
}
