using Kabomu.Common;
using Kabomu.Concurrency;
using Kabomu.Mediator.Handling;
using Kabomu.Mediator.Registry;
using Kabomu.QuasiHttp;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Mediator
{
    /// <summary>
    /// Gateway to the Kabomu.Mediator quasi web framework based on a handler pipeline architecture similar to
    /// popular actual web frameworks.
    /// </summary>
    /// <remarks>
    /// The key concepts of this quasi web framework are based on the <see cref="IContext"/>,
    /// <see cref="IRegistry"/> and <see cref="Handler"/> types.
    /// <para></para>
    /// An instance of this class denotes a handler pipeline, and so at a minimum, must be supplied with
    /// an instance of <see cref="Handler"/> type through its <see cref="InitialHandlers"/> property.
    /// <para></para>
    /// By employing the delegation abilities of the <see cref="IContext"/> argument of the
    /// <see cref="Handler"/> type, more instances <see cref="Handler"/> can be created and added to form a quasi http
    /// request pipeline.
    /// <para></para>
    /// Generally, the simplest of handlers will parse the context request body, process the deserialized body to
    /// obtain a body intended as a response, serialize it into the context response, and commit the response by
    /// sending it as the asynchronous result of the external call to the <see cref="ProcessRequest"/> method.
    /// All these steps are made available by the members of the <see cref="IContext"/> and
    /// <see cref="ContextExtensions"/> classes.
    /// <para></para>
    /// Handlers however can need more sophisticated setups to deal with request method and path matching,
    /// and so the <see cref="HandlerUtils"/> and <see cref="Path.DefaultPathTemplateGenerator"/> classes exist for this
    /// purpose as well.
    /// </remarks>
    public class MediatorQuasiWebApplication : IQuasiHttpApplication
    {
        /// <summary>
        /// Gets or sets the list of instances of the <see cref="Handler"/> type which
        /// will be used to begin quasi http request processing.
        /// </summary>
        public IList<Handler> InitialHandlers { get; set; }

        /// <summary>
        /// Gets or sets the "shadowable" contextual objects which will be used to populate an
        /// instance of <see cref="IContext"/> class for use by all handlers.
        /// </summary>
        /// <remarks>
        /// "Shadowable" here means that handlers can use delegation to insert their own
        /// contextual objects at the top of the context's registry under the keys of
        /// the context objects in this property.
        /// </remarks>
        public IRegistry InitialHandlerVariables { get; set; }

        /// <summary>
        /// Gets or sets the "non-shadowable" contextual objects which will be used to populate an
        /// instance of <see cref="IContext"/> class for use by all handlers.
        /// </summary>
        /// <remarks>
        /// "Non-shadowable" here means that handlers cannot use delegation to insert their own
        /// contextual objects at the top of the context's registry under the keys of
        /// the context objects in this property (they can try, but it won't take effect).
        /// </remarks>
        public IRegistry HandlerConstants { get; set; }

        /// <summary>
        /// Gets or sets a factory for creating "async locks" one for each instance of 
        /// <see cref="IContext"/> class that will be created.
        /// </summary>
        /// <remarks>
        /// Such locks will be used for internal sychronization within <see cref="IContext"/> and other
        /// framework classes. They can also be used for external synchronization by handlers.
        /// <para></para>
        /// Can be null, which is the same as the default value, which will lead to
        /// no synchronization. Clients will then have to exclude concurrent non thread safe
        /// accesses to context objects and their responses.
        /// </remarks>
        public IMutexApiFactory MutexApiFactory { get; set; }

        /// <summary>
        /// Creates an instance of <see cref="IContext"/> class with the properties of this instance, and begins
        /// processing the pipeline of handlers set up in the <see cref="InitialHandlers"/> property.
        /// </summary>
        /// <param name="request">the quasi http response</param>
        /// <param name="requestEnvironment">request environment variables</param>
        /// <returns>a task whose result will be a quasi http response</returns>
        /// <exception cref="MissingDependencyException">The <see cref="InitialHandlers"/> property is null</exception>
        public async Task<IQuasiHttpResponse> ProcessRequest(IQuasiHttpRequest request)
        {
            var contextRequest = new DefaultContextRequestInternal(request);
            var responseTransmmitter = new TaskCompletionSource<IQuasiHttpResponse>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var contextResponse = new DefaultContextResponseInternal(new DefaultQuasiHttpResponse(), responseTransmmitter);
            var context = new DefaultContextInternal
            {
                Request = contextRequest,
                Response = contextResponse,
                InitialHandlers = InitialHandlers,
                InitialHandlerVariables = InitialHandlerVariables,
                HandlerConstants = HandlerConstants,
            };

            var mutexApiTask = MutexApiFactory?.Create();
            if (mutexApiTask != null)
            {
                context.MutexApi = await mutexApiTask;
            }

            context.Start();

            return await responseTransmmitter.Task;
        }
    }
}
