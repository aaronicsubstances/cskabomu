using Kabomu.Mediator.Registry;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kabomu.Mediator.Handling
{
    /// <summary>
    /// Represents some context for <see cref="Handler"/> instances to process quasi http requests and generate quasi http
    /// responses.
    /// </summary>
    /// <remarks>
    /// This interface provides handlers with
    /// <list type="bullet">
    /// <item>access to instances of <see cref="QuasiHttp.IQuasiHttpRequest"/> and <see cref="QuasiHttp.IQuasiHttpResponse"/>
    /// classes</item>
    /// <item>delegation to other handlers via the Next* and Insert* methods.</item>
    /// <item>access to contextual objects by acting as a registry onto which arbitrary objects can be "pushed"
    /// during delegation, for downstream handlers to consume.</item>
    /// </list>
    /// <para>
    /// Due to delegation, at any point in time the handlers of an instance of <see cref="IContext"/> class
    /// are arranged as a stack, in which each item of the stack is a chain of handlers processed in order of
    /// insertion.
    /// </para>
    /// By default the provided contextual objects are
    /// <list type="bullet">
    /// <item>an instance of <see cref="Path.DefaultPathTemplateGenerator"/> class through the key of
    /// <see cref="ContextUtils.RegistryKeyPathTemplateGenerator"/></item>
    /// <item>an instance of <see cref="Path.IPathMatchResult"/> class through the key of
    /// <see cref="ContextUtils.RegistryKeyPathMatchResult"/>. This instance serves as the root path match result,
    /// in which its UnboundRequestTarget property is set to whatever the request target is.</item>
    /// </list>
    /// Any additional contextual objects can be present from <see cref="MediatorQuasiWebApplication.InitialHandlerVariables"/>
    /// and <see cref="MediatorQuasiWebApplication.HandlerConstants"/> properties. Delegation can be used to "push"
    /// contextual objects on top of those from the <see cref="MediatorQuasiWebApplication.InitialHandlerVariables"/> property. Those
    /// from <see cref="MediatorQuasiWebApplication.HandlerConstants"/> property however, will always remain at the top throughout
    /// the lifetime of an instance of this class.
    /// <para></para>
    /// 
    /// Instances of this class are not threadsafe without external synchronization. The <see cref="MutexApi"/> property is used
    /// internally for all synchronization needs, and is available to handlers for the same purpose. The
    /// <see cref="MediatorQuasiWebApplication.MutexApiFactory"/> property is used to generate one instance each for every instance
    /// of the <see cref="IContext"/> class.
    /// </remarks>
    public interface IContext : IRegistry
    {
        /// <summary>
        /// Gets a mutual exclusion async lock which can be used to synchronize access to instances of this object.
        /// </summary>
        object Mutex { get; }

        /// <summary>
        /// Gets the wrapper through which the quasi http request can be accessed. The wrapper also
        /// acts as a mutable store of contextual objects, which handlers can use.
        /// </summary>
        IContextRequest Request { get; }

        /// <summary>
        /// Gets the quasi http response wrapper by which handlers can generate a quasi http response
        /// for an instance of the <see cref="MediatorQuasiWebApplication"/> class.
        /// </summary>
        IContextResponse Response { get; }

        /// <summary>
        /// Pushes a new chain of handlers into the stack, then delegates to the first of them.
        /// </summary>
        /// <remarks>
        /// The request and response of this object should not be accessed after this method is called.
        /// </remarks>
        /// <param name="handlers">The handlers to insert. null items will be ignored. if empty, then the entire call will
        /// be equivalent to a call to Next*</param>
        void Insert​(IList<Handler> handlers);

        /// <summary>
        /// Pushes a new chain of handlers into the stack, with the given registry, then delegates to the first of them.
        /// </summary>
        /// <remarks>
        /// The given registry will be applicable to the inserted handlers, and  handlers in all chains which
        /// will be pushed on top of the newly created chain.
        /// </remarks>
        /// <param name="handlers">The handlers to insert. null items will be ignored. Also if empty, then the entire call will
        /// be equivalent to a call to Next*</param>
        /// <param name="registry">The registry for the inserted handlers</param>
        void Insert​(IList<Handler> handlers, IRegistry registry);

        /// <summary>
        /// Pops off the current chain, abandons all handlers in the chain yet to be invoked,
        /// and delegates to the first handler in the new current chain.
        /// </summary>
        /// <remarks>
        /// If all handlers in the current chain have been invoked, then the entire method call will
        /// be equivalent to a call to Next*
        /// </remarks>
        void SkipInsert();

        /// <summary>
        /// Delegates handling to the next handler in line.
        /// </summary>
        /// <remarks>
        /// The next handler in line is the next handler in the current chain. Unless the current chain is exhausted,
        /// in which case the current chain is popped off the stack and the next handler becomes the first handler in the
        /// new current chain.
        ///  <para></para>
        /// The request and response of this object should not be accessed after this method is called.
        /// </remarks>
        void Next();

        /// <summary>
        /// Invokes the next handler in line, after adding the given registry.
        /// </summary>
        /// <remarks>
        /// The next handler in line is the next handler in the current chain. Unless the current chain is exhausted,
        /// in which case the current chain is popped off the stack and the next handler becomes the first handler in the
        /// new current chain.
        ///  <para></para>
        /// The given registry will be applicable to all subsequent handlers in the current chain, and
        /// handlers in all chains which will subsequently be pushed on top of current chain.
        /// <para>
        /// If there are no subsequent handlers in the current chain, then the given registry will not have any effect.
        /// </para>
        /// </remarks>
        /// <param name="registry">The registry to make available for subsequent handlers. can be null, in which 
        /// case it will be ignored.</param>
        void Next​(IRegistry registry);
    }
}