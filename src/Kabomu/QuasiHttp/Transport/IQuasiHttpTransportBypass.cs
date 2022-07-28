using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.Transport
{
    public interface IQuasiHttpTransportBypass
    {
        Tuple<Task<IQuasiHttpResponse>, object> ProcessSendRequest(IQuasiHttpRequest request,
               IConnectivityParams connectivityParams);
        void CancelSendRequest(object sendCancellationHandle);
    }
}
