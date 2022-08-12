using System;
using System.Threading.Tasks;

namespace Kabomu.Mediator.Handling
{
    public delegate Task Handler(IContext context);

    /// <summary>
    /// Handler interface for use with dependency injection.
    /// </summary>
    public interface IHandler
    {
        Task Handle(IContext context);
    }
}