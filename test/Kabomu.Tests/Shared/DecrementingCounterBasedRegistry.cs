using Kabomu.Mediator.Registry;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Tests.Shared
{
    public class DecrementingCounterBasedRegistry : IRegistry
    {
        public (bool, object) TryGet(object key)
        {
            if (key is int count)
            {
                if (count >= 0)
                {
                    return (true, count);
                }
            }
            return (false, null);
        }

        public object Get(object key)
        {
            if (key is int count)
            {
                if (count >= 0)
                {
                    return count;
                }
            }
            throw new NotInRegistryException($"{key}");
        }

        public (bool, object) TryGetFirst(object key, Func<object, (bool, object)> transformFunction)
        {
            if (key is int count)
            {
                while (count >= 0)
                {
                    var result = transformFunction.Invoke(count);
                    if (result.Item1)
                    {
                        return result;
                    }
                    count--;
                }
            }
            return (false, null);
        }

        public IEnumerable<object> GetAll(object key)
        {
            var values = new List<object>();
            if (key is int count)
            {
                if (count >= 0)
                {
                    for (int i = count; i >= 0; i--)
                    {
                        values.Add(i);
                    }
                }
            }
            return values;
        }
    }
}
