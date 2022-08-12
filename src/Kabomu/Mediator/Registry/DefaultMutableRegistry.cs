using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Mediator.Registry
{
    public class DefaultMutableRegistry : IMutableRegistry
    {
        private readonly LinkedList<IRegistryEntry> _typeKeyEntries; // serves as deque data structure.
        private readonly Dictionary<string, Stack<IRegistryEntry>> _stringKeyEntries;

        public DefaultMutableRegistry()
        {
            _typeKeyEntries = new LinkedList<IRegistryEntry>();
            _stringKeyEntries = new Dictionary<string, Stack<IRegistryEntry>>();
        }

        public IMutableRegistry Add(object key, object value)
        {
            return AddGenerator(key, () => value);
        }

        public IMutableRegistry AddGenerator(object key, Func<object> valueGenerator)
        {
            if (key is string stringKey)
            {
                Stack<IRegistryEntry> selectedEntries;
                if (_stringKeyEntries.ContainsKey(stringKey))
                {
                    selectedEntries = _stringKeyEntries[stringKey];
                }
                else
                {
                    selectedEntries = new Stack<IRegistryEntry>();
                    _stringKeyEntries.Add(stringKey, selectedEntries);
                }
                selectedEntries.Push(new DefaultRegistryEntry
                {
                    ValueGenerator = valueGenerator
                });
            }
            else if (key is Type)
            {
                _typeKeyEntries.AddFirst(new DefaultRegistryEntry
                {
                    Key = key,
                    ValueGenerator = valueGenerator
                });
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
                    if (typeKey.IsAssignableFrom((Type)node.Value.Key))
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
                    var valueToUse = _stringKeyEntries[stringKey].Peek().ValueGenerator.Invoke();
                    return (true, valueToUse);
                }
            }
            else if (key is Type typeKey)
            {
                foreach (var entry in _typeKeyEntries)
                {
                    if (typeKey.IsAssignableFrom((Type)entry.Key))
                    {
                        return (true, entry.ValueGenerator.Invoke());
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
                    foreach (var entry in _stringKeyEntries[stringKey])
                    {
                        selected.Add(entry.ValueGenerator.Invoke());
                    }
                }
            }
            else if (key is Type typeKey)
            {
                foreach (var entry in _typeKeyEntries)
                {
                    if (typeKey.IsAssignableFrom((Type)entry.Key))
                    {
                        selected.Add(entry.ValueGenerator.Invoke());
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
