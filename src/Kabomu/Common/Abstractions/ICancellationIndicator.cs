using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common.Abstractions
{
    public interface ICancellationIndicator
    {
        bool Cancelled { get; }
    }
}
