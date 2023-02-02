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

        public void CancelSendRequest(object sendCancellationHandle)
        {
            // do nothing.
        }

        public (Task<IDirectSendResult>, object) ProcessSendRequest(
            IQuasiHttpRequest request, IConnectivityParams connectivityParams)
        {
            var task = ProcessSendRequestInternal(request);
            return (task, null);
        }

        private async Task<IDirectSendResult> ProcessSendRequestInternal(IQuasiHttpRequest request)
        {
            var res = await _application.ProcessRequest(request, null);
            return new DefaultDirectSendResult
            {
                Response = res
            };
        }
    }
}
