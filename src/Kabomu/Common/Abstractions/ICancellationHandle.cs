using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common.Abstractions
{
    public interface ICancellationHandle
    {
        bool Cancelled { get; }
        void Cancel();
        bool TryAddCancellationListener(Action<object> cb, object cbState);
    }
}
