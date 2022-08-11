using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kabomu.Mediator.Registry
{
    public class DefaultMutableRegistry : IMutableRegistry
    {
        private readonly LinkedList<IRegisterValue> _typeKeyEntries; // serves as deque data structure.
        private readonly Dictionary<string, Stack<IRegisterValue>> _stringKeyEntries;

        public DefaultMutableRegistry()
        {
            _typeKeyEntries = new LinkedList<IRegisterValue>();
            _stringKeyEntries = new Dictionary<string, Stack<IRegisterValue>>();
        }

        public IMutableRegistry Add(object key, object value)
        {
            if (key is string stringKey)
            {
                Stack<IRegisterValue> registerValues;
                if (_stringKeyEntries.ContainsKey(stringKey))
                {
                    registerValues = _stringKeyEntries[stringKey];
                }
                else
                {
                    registerValues = new Stack<IRegisterValue>();
                    _stringKeyEntries.Add(stringKey, registerValues);
                }
                registerValues.Push(new EagerValue(null, value));
            }
            else if (key is Type typeKey)
            {
                _typeKeyEntries.AddFirst(new EagerValue(typeKey, value));
            }
            else
            {
                throw new ArgumentException("key must be a string or Type object", nameof(key));
            }
            return this;
        }

        public IMutableRegistry AddLazy(object key, Func<object> valueGenerator)
        {
            if (key is string stringKey)
            {
                Stack<IRegisterValue> registerValues;
                if (_stringKeyEntries.ContainsKey(stringKey))
                {
                    registerValues = _stringKeyEntries[stringKey];
                }
                else
                {
                    registerValues = new Stack<IRegisterValue>();
                    _stringKeyEntries.Add(stringKey, registerValues);
                }
                registerValues.Push(new LazyValue(null, valueGenerator));
            }
            else if (key is Type typeKey)
            {
                _typeKeyEntries.AddFirst(new LazyValue(typeKey, valueGenerator));
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
                    if (typeKey.IsAssignableFrom(node.Value.Key))
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
                    var valueToUse = _stringKeyEntries[stringKey].Peek();
                    return (true, valueToUse);
                }
            }
            else if (key is Type typeKey)
            {
                foreach (var entry in _typeKeyEntries)
                {
                    if (typeKey.IsAssignableFrom(entry.Key))
                    {
                        return (true, entry.Value);
                    }
                }
            }
            return (false, null);
        }

        public IEnumerable<object> GetAll(object key)
        {
            if (key is string stringKey)
            {
                if (_stringKeyEntries.ContainsKey(stringKey))
                {
                    return _stringKeyEntries[stringKey];
                }
            }
            else if (key is Type typeKey)
            {
                var selected = new List<object>();
                foreach (var entry in _typeKeyEntries)
                {
                    if (typeKey.IsAssignableFrom(entry.Key))
                    {
                        selected.Add(entry.Value);
                    }
                }
                return selected;
            }
            return Enumerable.Empty<object>();
        }

        public object Get(object key)
        {
            return RegistryUtils.Get(this, key);
        }

        public (bool, object) TryGetFirst(object key, Func<object, (bool, object)> transformFunction)
        {
            return RegistryUtils.TryGetFirst(this, key, transformFunction);
        }

        private interface IRegisterValue
        {
            Type Key { get; }
            object Value { get; }
        }

        private class EagerValue : IRegisterValue
        {
            public EagerValue(Type key, object value)
            {
                Key = key;
                Value = value;
            }

            public Type Key { get; }

            public object Value { get; }
        }

        private class LazyValue : IRegisterValue
        {
            private readonly object _lock = new object();
            private readonly Func<object> _valueGenerator;
            private bool _valueSet;
            private object _value;

            public LazyValue(Type key, Func<object> valueGenerator)
            {
                Key = key;
                _valueGenerator = valueGenerator;
            }

            public Type Key { get; }

            public object Value
            {
                get
                {
                    // use recommended double condition check strategy to implement lazy loading pattern
                    if (!_valueSet)
                    {
                        lock (_lock)
                        {
                            if (!_valueSet)
                            {
                                _value = _valueGenerator.Invoke();
                                _valueSet = true;
                            }
                        }
                    }
                    return _value;
                }
            }
        }
    }
}
