using System;
using System.Threading.Tasks;

namespace Kabomu.Mediator
{
    public interface IContext : IRegistry
    {
        IRequest Request { get; }
        IResponse Response { get; }
        Task Insert​(params Handler[] handlers);
        Task Insert​(IRegistry registry, params Handler[] handlers);
        Task Next();
        Task Next​(IRegistry registry);
        Task<T> ParseRequest<T>(object parseOpts);
        Task RenderResponse(object body);
        Task HandleError​(Exception error);
    }
}