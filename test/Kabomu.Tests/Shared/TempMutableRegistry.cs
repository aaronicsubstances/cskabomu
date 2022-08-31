using Kabomu.Mediator.Registry;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Tests.Shared
{
    public class TempMutableRegistry : IMutableRegistry
    {
        public object ActualKeyAdded { get; set; }
        public Func<object> ActualValueGeneratorAdded { get; set; }

        public IMutableRegistry Add(object key, object value)
        {
            throw new NotImplementedException();
        }

        public IMutableRegistry AddGenerator(object key, Func<object> valueGenerator)
        {
            ActualKeyAdded = key;
            ActualValueGeneratorAdded = valueGenerator;
            return this;
        }

        public object Get(object key)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<object> GetAll(object key)
        {
            throw new NotImplementedException();
        }

        public IMutableRegistry Remove(object key)
        {
            throw new NotImplementedException();
        }

        public (bool, object) TryGet(object key)
        {
            throw new NotImplementedException();
        }

        public (bool, object) TryGetFirst(object key, Func<object, (bool, object)> transformFunction)
        {
            throw new NotImplementedException();
        }
    }
}
