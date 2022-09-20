﻿using System;
using System.Threading.Tasks;

namespace Kabomu.Mediator.Handling
{
    /// <summary>
    /// Represents a procedure that can process quasi http requests. All handlers are arranged in a stack of chains, and
    /// processed first to last within a chain, and top to bottom across the stack.
    /// </summary>
    /// <remarks>
    /// In Kabomu.Mediator, stack of chains of handlers are expected to be built incrementally at request time, rather than all at once
    /// before any request.
    /// <para></para>
    /// A handler is expected to do exactly one of following to build the stack:
    /// <list type="bullet">
    /// <item>push a new chain of handlers onto the stack, and call the first of them</item>
    /// <item>call the next handler in the top chain, or pop the stack and call
    /// the first handler of the new top chain</item>
    /// <item>skip the remaining handlers in the top chain by popping the stack, and call
    /// the first handler of the new top chain</item>
    /// <item>do not touch the stack at all, and instead commit quasi
    /// http response, effectively ending the entire request processing</item>
    /// </list>
    /// </remarks>
    /// <param name="context">quasi http context</param>
    /// <returns>task representing asynchronus operation.</returns>
    public delegate Task Handler(IContext context);

    /// <summary>
    /// Counterpart to <see cref="Handler"/> delegate.
    /// </summary>
    public interface IHandler
    {
        /// <summary>
        /// Processes a quasi http context.
        /// </summary>
        /// <param name="context">quasi http context</param>
        /// <returns>task representing asynchronous operation</returns>
        Task Handle(IContext context);
    }
}