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

        public (Task<IQuasiHttpResponse>, object) ProcessSendRequest(
            IQuasiHttpRequest request, IConnectivityParams connectivityParams)
        {
            var task = _application.ProcessRequest(request);
            return (task, null);
        }
    }
}
