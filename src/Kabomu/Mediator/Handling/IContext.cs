using Kabomu.Mediator.Registry;
using Kabomu.Mediator.RequestParsing;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kabomu.Mediator.Handling
{
    public interface IContext : IRegistry
    {
        IRequest Request { get; }
        IPathBinding PathBinding { get; }
        IResponse Response { get; }
        Task Insert​(IList<Handler> handlers);
        Task Insert​(IRegistry registry, IList<Handler> handlers);
        Task Next();
        Task Next​(IRegistry registry);
        Task<T> ParseRequest<T>(object parseOpts);
        Task RenderResponse(object body);
        Task HandleError​(Exception error);
    }
}