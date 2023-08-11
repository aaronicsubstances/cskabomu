using Kabomu.Mediator.Handling;
using Kabomu.Mediator.Registry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Tests.Shared.Mediator
{
    public class TempContext : IContext
    {
        public IContextRequest Request { get; set; }
        public IContextResponse Response { get; set; }
        public IList<Handler> HandlersSeen { get; set; }
        public IRegistry RegistrySeen { get; set; }
        public int InsertCallCount { get; set; }
        public int SkipInsertCallCount { get; set; }
        public int NextCallCount { get; set; }
        public IRegistry DelegateRegistry { get; set; }

        public void Insert(IList<Handler> handlers)
        {
            Insert(handlers, null);
        }

        public void Insert(IList<Handler> handlers, IRegistry registry)
        {
            InsertCallCount++;
            HandlersSeen = handlers;
            RegistrySeen = registry;
        }

        public void SkipInsert()
        {
            SkipInsertCallCount++;
        }

        public void Next()
        {
            Next(null);
        }

        public void Next(IRegistry registry)
        {
            NextCallCount++;
            RegistrySeen = registry;
        }

        public object Get(object key)
        {
            return DelegateRegistry.Get(key);
        }

        public IEnumerable<object> GetAll(object key)
        {
            return DelegateRegistry.GetAll(key);
        }

        public (bool, object) TryGet(object key)
        {
            return DelegateRegistry.TryGet(key);
        }

        public (bool, object) TryGetFirst(object key, Func<object, (bool, object)> transformFunction)
        {
            return DelegateRegistry.TryGetFirst(key, transformFunction);
        }
    }
}
