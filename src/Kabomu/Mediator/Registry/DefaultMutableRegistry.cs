using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Mediator.Registry
{
    public class DefaultMutableRegistry : IMutableRegistry
    {
        private readonly IDictionary<object, LinkedList<Func<object>>> _entries;
        private readonly LinkedList<object> _entryKeys; // used to maintain key insertion order in FIFO order

        public DefaultMutableRegistry()
        {
            _entries = new Dictionary<object, LinkedList<Func<object>>>();
            _entryKeys = new LinkedList<object>();
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
            // disallow duplicate entry keys.
            var keyPresent = false;
            foreach (var k in _entryKeys)
            {
                // use the same equality comparison mechanism as the one
                // used to compare keys in dictionary.
                if (EqualityComparer<object>.Default.Equals(k, key))
                {
                    keyPresent = true;
                    break;
                }
            }
            if (!keyPresent)
            {
                // insert in FIFO order.
                _entryKeys.AddFirst(key);
            }
            return this;
        }

        public IMutableRegistry Remove(object key)
        {
            _entries.Remove(key);
            // remove all entry keys and re-add them in LIFO order, except for
            // keys which match the supplied argument.
            var keyCount = _entryKeys.Count;
            for (int i = 0; i < keyCount; i++)
            {
                var curr = _entryKeys.First;
                _entryKeys.RemoveFirst();
                // use the same equality comparison mechanism as the one
                // used to compare keys in dictionary.
                if (!EqualityComparer<object>.Default.Equals(curr.Value, key))
                {
                    _entryKeys.AddLast(curr);
                }
            }
            return this;
        }

        public (bool, object) TryGet(object key)
        {
            if (key is IRegistryKeyPattern keyPattern)
            {
                foreach (var k in _entryKeys)
                {
                    if (keyPattern.IsMatch(k))
                    {
                        var valueGenerator = _entries[k].First.Value;
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
                foreach (var k in _entryKeys)
                {
                    if (keyPattern.IsMatch(k))
                    {
                        foreach (var valueGenerator in _entries[k])
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
                foreach (var k in _entryKeys)
                {
                    if (keyPattern.IsMatch(k))
                    {
                        var valueGenerator = _entries[k].First.Value;
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
