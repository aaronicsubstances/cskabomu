using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common
{
    public interface IConnectionAllocationRequest
    {
        object RemoteEndpoint { get; }
        IDictionary<string, object> Environment { get; }
    }
}
