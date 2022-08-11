using Kabomu.Mediator.Registry;
using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.EntityBody;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Mediator
{
    public interface IRequest : IMutableRegistry
    {
        IQuasiHttpRequest RawRequest { get; }
        string Path { get; }
        IHeaders Headers { get; }
        IQuasiHttpBody Body { get; }
    }
}
