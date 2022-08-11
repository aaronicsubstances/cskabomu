using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.EntityBody;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Mediator.Handling
{
    public interface IResponse
    {
        IQuasiHttpMutableResponse RawResponse { get; }
        bool StatusIndicatesSuccess { get; }
        bool StatusIndicatesClientError { get; }
        IMutableHeaders Headers { get; }
        IResponse SetStatusIndicatesSuccess(bool value);
        IResponse SetStatusIndicatesClientError(bool value);
        IResponse SetStatusMessage(string value);
        IResponse SetBody(IQuasiHttpBody value);
        Task Send();
        Task SendWithBody(IQuasiHttpBody value);
    }
}
