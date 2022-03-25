using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common.Abstractions
{
    public interface IRecyclingFactory
    {
        void Recycle(IRecyclable garbage);
        T CreateRecyledObject<T>() where T : IRecyclable;
    }
}
