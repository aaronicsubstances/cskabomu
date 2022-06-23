using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common.Transports
{
    public class DefaultConnectionAllocationRequest : IConnectionAllocationRequest
    {
        public object RemoteEndpoint { get; set; }
        public IDictionary<string, object> Environment { get; set; }
    }
}
