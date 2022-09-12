using System;
using System.Threading.Tasks;

namespace Kabomu.Mediator.Handling
{
    /// <summary>
    /// Represents a function that can process quasi http requests. All handlers are arranged in chain.
    /// </summary>
    /// <remarks>
    /// A handler is expected to do at most one of 4 things:
    /// <list type="bullet">
    /// <item>call the next handler in line</item>
    /// <item>insert new handlers and call the first of them</item>
    /// <item>skip insert handler</item>
    /// <item>call Send* methods to commit quasi http response</item>
    /// </list>
    /// </remarks>
    /// <param name="context">quasi http context</param>
    /// <returns>task representing asynchronus operation.</returns>
    public delegate Task Handler(IContext context);

    /// <summary>
    /// Handler interface for use with dependency injection.
    /// </summary>
    public interface IHandler
    {
        Task Handle(IContext context);
    }
}