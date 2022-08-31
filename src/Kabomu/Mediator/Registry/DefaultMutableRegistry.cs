using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kabomu.Mediator.Registry
{
    public class DefaultMutableRegistry : IMutableRegistry
    {
        // use data structure which maintains order of insertion of keys.
        private readonly Dictionary<object, LinkedList<Func<object>>> _entries;

        public DefaultMutableRegistry()
        {
            _entries = new Dictionary<object, LinkedList<Func<object>>>();
        }

        public IMutableRegistry Add(object key, object value)
        {
            return AddGenerator(key, () => value);
        }

        public IMutableRegistry AddGenerator(object key, Func<object> valueGenerator)
        {
            LinkedList<Func<object>> selectedEntries;
            if (_entries.ContainsKey(key))
            {
                selectedEntries = _entries[key];
            }
            else
            {
                selectedEntries = new LinkedList<Func<object>>();
                _entries.Add(key, selectedEntries);
            }
            // insert in FIFO order.
            selectedEntries.AddFirst(valueGenerator);
            return this;
        }

        public IMutableRegistry Remove(object key)
        {
            _entries.Remove(key);
            return this;
        }

        public (bool, object) TryGet(object key)
        {
            if (key is IRegistryKeyPattern keyPattern)
            {
                foreach (var e in _entries.Reverse())
                {
                    if (keyPattern.IsMatch(e.Key))
                    {
                        var valueGenerator = e.Value.First.Value;
                        var value = valueGenerator.Invoke();
                        return (true, value);
                    }
                }
            }
            else
            {
                if (_entries.ContainsKey(key))
                {
                    var valueGenerator = _entries[key].First.Value;
                    var value = valueGenerator.Invoke();
                    return (true, value);
                }
            }
            return (false, null);
        }

        public IEnumerable<object> GetAll(object key)
        {
            var selected = new List<object>();
            if (key is IRegistryKeyPattern keyPattern)
            {
                foreach (var e in _entries.Reverse())
                {
                    if (keyPattern.IsMatch(e.Key))
                    {
                        foreach (var valueGenerator in e.Value)
                        {
                            var value = valueGenerator.Invoke();
                            selected.Add(value);
                        }
                    }
                }
            }
            else
            {
                if (_entries.ContainsKey(key))
                {
                    foreach (var valueGenerator in _entries[key])
                    {
                        var value = valueGenerator.Invoke();
                        selected.Add(value);
                    }
                }
            }
            return selected;
        }

        public (bool, object) TryGetFirst(object key, Func<object, (bool, object)> transformFunction)
        {
            if (transformFunction == null)
            {
                throw new ArgumentNullException(nameof(transformFunction));
            }
            if (key is IRegistryKeyPattern keyPattern)
            {
                foreach (var e in _entries.Reverse())
                {
                    if (keyPattern.IsMatch(e.Key))
                    {
                        var valueGenerator = e.Value.First.Value;
                        var value = valueGenerator.Invoke();
                        var result = transformFunction.Invoke(value);
                        if (result.Item1)
                        {
                            return result;
                        }
                    }
                }
            }
            else
            {
                if (_entries.ContainsKey(key))
                {
                    var valueGenerator = _entries[key].First.Value;
                    var value = valueGenerator.Invoke();
                    var result = transformFunction.Invoke(value);
                    if (result.Item1)
                    {
                        return result;
                    }
                }
            }
            return (false, null);
        }

        public object Get(object key)
        {
            return RegistryUtils.Get(this, key);
        }
    }
}
