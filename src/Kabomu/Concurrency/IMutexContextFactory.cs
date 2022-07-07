using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Concurrency
{
    public interface IMutexContextFactory
    {
        bool IsExclusiveRunRequired { get; }
        IDisposable CreateMutexContext();
    }
}
