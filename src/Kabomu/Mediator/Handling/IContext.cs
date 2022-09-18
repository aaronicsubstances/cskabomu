using Kabomu.Concurrency;
using Kabomu.Mediator.Registry;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kabomu.Mediator.Handling
{
    public interface IContext : IRegistry
    {
        IMutexApi MutexApi { get; set; }
        IContextRequest Request { get; }
        IContextResponse Response { get; }

        void Insert​(IList<Handler> handlers);
        void Insert​(IList<Handler> handlers, IRegistry registry);
        void SkipInsert();
        void Next();
        void Next​(IRegistry registry);
    }
}