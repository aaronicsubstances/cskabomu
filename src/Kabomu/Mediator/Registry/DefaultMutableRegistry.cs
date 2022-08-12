using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kabomu.Mediator.Registry
{
    public class DefaultMutableRegistry : IMutableRegistry
    {
        private readonly LinkedList<(Type, IRegistryValueSource)> _typeKeyEntries; // serves as deque data structure.
        private readonly Dictionary<string, Stack<IRegistryValueSource>> _stringKeyEntries;

        public DefaultMutableRegistry()
        {
            _typeKeyEntries = new LinkedList<(Type, IRegistryValueSource)>();
            _stringKeyEntries = new Dictionary<string, Stack<IRegistryValueSource>>();
        }

        public IMutableRegistry Add(object key, object value)
        {
            return AddValueSource(key, new ConstantRegistryValueSource(value));
        }

        public IMutableRegistry AddValueSource(object key, IRegistryValueSource valueSource)
        {
            if (key is string stringKey)
            {
                Stack<IRegistryValueSource> selectedValueSources;
                if (_stringKeyEntries.ContainsKey(stringKey))
                {
                    selectedValueSources = _stringKeyEntries[stringKey];
                }
                else
                {
                    selectedValueSources = new Stack<IRegistryValueSource>();
                    _stringKeyEntries.Add(stringKey, selectedValueSources);
                }
                selectedValueSources.Push(valueSource);
            }
            else if (key is Type typeKey)
            {
                _typeKeyEntries.AddFirst((typeKey, valueSource));
            }
            else
            {
                throw new ArgumentException("key must be a string or Type object", nameof(key));
            }
            return this;
        }

        public IMutableRegistry Remove(object key)
        {
            if (key is string stringKey)
            {
                _stringKeyEntries.Remove(stringKey);
            }
            else if (key is Type typeKey)
            {
                // remove efficiently by re-adding all items in queue unto itself
                // enough number of times, exempting only the items which have to
                // be removed.
                int snapshotCount = _typeKeyEntries.Count;
                for (int i = 0; i < snapshotCount; i++)
                {
                    var node = _typeKeyEntries.First;
                    _typeKeyEntries.RemoveFirst();
                    if (typeKey.IsAssignableFrom(node.Value.Item1))
                    {
                        // remove by not re-adding.
                    }
                    else
                    {
                        // preserve ordering by enqueueing rather than pushing.
                        _typeKeyEntries.AddLast(node.Value);
                    }
                }
            }
            return this;
        }

        public (bool, object) TryGet(object key)
        {
            if (key is string stringKey)
            {
                if (_stringKeyEntries.ContainsKey(stringKey))
                {
                    var valueToUse = _stringKeyEntries[stringKey].Peek().Get();
                    return (true, valueToUse);
                }
            }
            else if (key is Type typeKey)
            {
                foreach (var entry in _typeKeyEntries)
                {
                    if (typeKey.IsAssignableFrom(entry.Item1))
                    {
                        return (true, entry.Item2.Get());
                    }
                }
            }
            return (false, null);
        }

        public IEnumerable<object> GetAll(object key)
        {
            var selected = new List<object>();
            if (key is string stringKey)
            {
                if (_stringKeyEntries.ContainsKey(stringKey))
                {
                    foreach (var valueSource in _stringKeyEntries[stringKey])
                    {
                        selected.Add(valueSource.Get());
                    }
                }
            }
            else if (key is Type typeKey)
            {
                foreach (var entry in _typeKeyEntries)
                {
                    if (typeKey.IsAssignableFrom(entry.Item1))
                    {
                        selected.Add(entry.Item2.Get());
                    }
                }
            }
            return selected;
        }

        public object Get(object key)
        {
            return RegistryUtils.Get(this, key);
        }

        public (bool, object) TryGetFirst(object key, Func<object, (bool, object)> transformFunction)
        {
            return RegistryUtils.TryGetFirst(this, key, transformFunction);
        }
    }
}
