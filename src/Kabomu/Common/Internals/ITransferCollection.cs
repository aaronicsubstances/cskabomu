using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common.Internals
{
    internal interface ITransferCollection<T> where T : class
    {
        bool TryAdd(T itemWithKeyIncluded);
        T TryRemove(T key);
        T TryGet(T key);
        void Clear();
        void ForEach(Action<T> perItemAction);
        int Count { get; }
    }
}
