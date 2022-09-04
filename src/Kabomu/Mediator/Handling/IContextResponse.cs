using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.EntityBody;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Mediator.Handling
{
    public interface IContextResponse
    {
        IQuasiHttpMutableResponse RawResponse { get; }
        int StatusCode { get; }
        bool IsSuccessStatusCode { get; }
        bool IsClientErrorStatusCode { get; }
        bool IsServerErrorStatusCode { get; }
        IQuasiHttpBody Body { get; }
        IMutableHeadersWrapper Headers { get; }
        IContextResponse SetSuccessStatusCode();
        IContextResponse SetClientErrorStatusCode();
        IContextResponse SetServerErrorStatusCode();
        IContextResponse SetStatusCode(int value);
        IContextResponse SetBody(IQuasiHttpBody value);
        Task Send();
        Task SendWithBody(IQuasiHttpBody value);
        Task<bool> TrySend();
        Task<bool> TrySendWithBody(IQuasiHttpBody value);
    }
}
