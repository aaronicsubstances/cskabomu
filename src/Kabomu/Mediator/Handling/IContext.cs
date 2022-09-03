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

        Task Insert​(IList<Handler> handlers);
        Task Insert​(IList<Handler> handlers, IRegistry registry);
        Task SkipInsert();
        Task Next();
        Task Next​(IRegistry registry);
    }
}