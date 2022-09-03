using Kabomu.Mediator.Registry;
using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.EntityBody;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Mediator.Handling
{
    public interface IContextRequest : IMutableRegistry
    {
        IQuasiHttpRequest RawRequest { get; }
        IDictionary<string, object> Environment { get; }
        string Target { get; }
        IHeadersWrapper Headers { get; }
        IQuasiHttpBody Body { get; }
    }
}
