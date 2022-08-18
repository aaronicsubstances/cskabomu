using Kabomu.Concurrency;
using Kabomu.Mediator.Path;
using Kabomu.Mediator.Registry;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kabomu.Mediator.Handling
{
    public interface IContext : IRegistry
    {
        IContextRequest Request { get; }
        IPathMatchResult PathMatchResult { get; }
        IContextResponse Response { get; }
        IMutexApi MutexApi { get; set; }
        Task Insert​(IList<Handler> handlers);
        Task Insert​(IList<Handler> handlers, IRegistry registry);
        Task SkipInsert();
        Task Next();
        Task Next​(IRegistry registry);
        Task<T> ParseRequest<T>(object parseOpts);
        Task RenderResponse(object body);
        Task HandleError​(Exception error);
        Task HandleUnexpectedEnd();
    }
}