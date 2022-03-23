using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common.Abstractions
{
    public interface IQpcReceiveOptions
    {
        int TimeoutMillis { get; }
        ICancellationHandle CancellationHandle { get; }
        bool IgnoreDuplicateProtection { get; }
    }
}
