using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common.Abstractions
{
    public interface ICancellationHandle
    {
        bool Cancelled { get; }
        void Cancel();
        void AddCancellationListener(Action<object> cb, object cbState);
        void RemoveCancellationListener(Action<object> cb);
    }
}
