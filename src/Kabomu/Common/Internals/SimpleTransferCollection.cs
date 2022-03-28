using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common.Internals
{
    internal class SimpleTransferCollection<T> : ITransferCollection<T> where T : class
    {
        private readonly Dictionary<T, T> _backingStore = new Dictionary<T, T>();

        public void Clear()
        {
            _backingStore.Clear();
        }

        public int Count => _backingStore.Count;

        public void ForEach(Action<T> perItemAction)
        {
            foreach (var item in _backingStore.Values)
            {
                perItemAction.Invoke(item);
            }
        }

        public bool TryAdd(T itemWithKeyIncluded)
        {
            if (_backingStore.ContainsKey(itemWithKeyIncluded))
            {
                return false;
            }
            _backingStore.Add(itemWithKeyIncluded, itemWithKeyIncluded);
            return true;
        }

        public T TryGet(T key)
        {
            if (_backingStore.ContainsKey(key))
            {
                return _backingStore[key];
            }
            return null;
        }

        public T TryRemove(T key)
        {
            T item = null;
            if (_backingStore.ContainsKey(key))
            {
                item = _backingStore[key];
                _backingStore.Remove(key);
            }
            return item;
        }
    }
}
