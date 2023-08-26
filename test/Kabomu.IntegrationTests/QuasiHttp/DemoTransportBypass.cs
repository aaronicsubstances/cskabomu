using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.Client;
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
            set
            {
                lock (_mutex)
                {
                    _cancellationRequested = value;
                }
            }
        }

        public IQuasiHttpSendOptions ActualSendOptions { get; set; }
        public object ActualRemoteEndpoint { get; set; }

        public Task CancelSendRequest(object sendCancellationHandle)
        {
            lock (_mutex)
            {
                _cancellationRequested = true;
            }
            return Task.CompletedTask;
        }

        private async Task<IQuasiHttpResponse> ProcessSendRequestInternal(
            object remoteEndpoint,
            Func<IDictionary<string, object>, Task<IQuasiHttpRequest>> requestFunc,
            IQuasiHttpSendOptions sendOptions)
        {
            ActualRemoteEndpoint = remoteEndpoint;
            ActualSendOptions = sendOptions;
            var request = await requestFunc(null);
            return await SendRequestCallback(request);
        }

        public Task<QuasiHttpSendResponse> ProcessSendRequest(
            object remoteEndpoint,
            IQuasiHttpRequest request,
            IQuasiHttpSendOptions sendOptions)
        {
            return ProcessSendRequest2(remoteEndpoint,
                _ => Task.FromResult(request),
                sendOptions);
        }

        public Task<QuasiHttpSendResponse> ProcessSendRequest2(
            object remoteEndpoint,
            Func<IDictionary<string, object>, Task<IQuasiHttpRequest>> requestFunc,
            IQuasiHttpSendOptions sendOptions)
        {
            var task = ProcessSendRequestInternal(remoteEndpoint, requestFunc, sendOptions);
            var result = new QuasiHttpSendResponse
            {
                ResponseTask = task
            };
            if (CreateCancellationHandles)
            {
                result.CancellationHandle = new object();
            }
            return Task.FromResult(result);
        }
    }
}
