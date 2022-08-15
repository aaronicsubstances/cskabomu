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
        int StatusCode { get; }
        bool IsSuccessStatusCode { get; }
        bool IsClientErrorStatusCode { get; }
        bool IsServerErrorStatusCode { get; }
        IMutableHeaders Headers { get; }
        IResponse SetSuccessStatusCode();
        IResponse SetClientErrorStatusCode();
        IResponse SetServerErrorStatusCode();
        IResponse SetStatusCode(int value);
        IResponse SetBody(IQuasiHttpBody value);
        Task Send();
        Task SendWithBody(IQuasiHttpBody value);
        Task<bool> TrySend();
        Task<bool> TrySendWithBody(IQuasiHttpBody value);
    }
}
