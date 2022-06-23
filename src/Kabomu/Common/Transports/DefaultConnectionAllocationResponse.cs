using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common.Transports
{
    public class DefaultConnectionAllocationResponse : IConnectionAllocationResponse
    {
        public object Connection { get; set; }
        public IDictionary<string, object> Environment { get; set; }
    }
}
