using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.Transport
{
    public interface ITransportContext
    {
        object Transport { get; }
        object Connection { get; }
    }
}
