using Kabomu.Mediator.Registry;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Tests.Shared
{
    public class IndexedArrayBasedRegistry : IRegistry
    {
        private readonly object[] _array;

        public IndexedArrayBasedRegistry(object[] array)
        {
            _array = array;
        }

        public (bool, object) TryGet(object key)
        {
            if (key is null)
            {
                if (_array != null && _array.Length > 0)
                {
                    return (true, _array[0]);
                }
            }
            else if (key is int index)
            {
                if (_array != null && index >= 0 && index < _array.Length)
                {
                    return (true, _array[index]);
                }
            }
            return (false, null);
        }

        public object Get(object key)
        {
            if (key is null)
            {
                if (_array != null && _array.Length > 0)
                {
                    return _array[0];
                }
            }
            else if (key is int index)
            {
                if (_array != null && index >= 0 && index < _array.Length)
                {
                    return _array[index];
                }
            }
            throw new NotInRegistryException($"{key}");
        }

        public (bool, object) TryGetFirst(object key, Func<object, (bool, object)> transformFunction)
        {
            if (key is null)
            {
                if (_array != null)
                {
                    foreach (var item in _array)
                    {
                        var result = transformFunction.Invoke(item);
                        if (result.Item1)
                        {
                            return result;
                        }
                    }
                }
            }
            else if (key is int index)
            {
                if (_array != null && index >= 0 && index < _array.Length)
                {
                    var result = transformFunction.Invoke(_array[index]);
                    if (result.Item1)
                    {
                        return result;
                    }
                }
            }
            return (false, null);
        }

        public IEnumerable<object> GetAll(object key)
        {
            if (key is null)
            {
                if (_array != null)
                {
                    return _array;
                }
            }
            else
            {
                if (key is int index)
                {
                    if (_array != null && index >= 0 && index < _array.Length)
                    {
                        return new object[] { _array[index] };
                    }
                }
            }
            return new object[0];
        }
    }
}
