using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.Transport
{
    public interface IConnectionAllocationResponse
    {
        object Connection { get; }
        IDictionary<string, object> Environment { get; }
    }
}
