using Kabomu.Common;
using Kabomu.Mediator.Handling;
using Kabomu.Mediator.Path;
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
    /// and so the <see cref="HandlerUtils"/> and <see cref="DefaultPathTemplateGenerator"/> classes exist for this
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
        /// "Shadowable" here means "can be hiddden", ie handlers can insert their own
        /// contextual objects at the top of the context's registry under the keys of
        /// the context objects in this property.
        /// <para>
        /// The following shadowable contextual objects are made available by default if not found
        /// initially inside this property:
        /// (1) an instance of <see cref="DefaultPathTemplateGenerator"/> with no value for its
        /// <see cref="DefaultPathTemplateGenerator.ConstraintFunctions"/> property,
        /// stored under the key <see cref="ContextUtils.RegistryKeyPathTemplateGenerator"/>;
        /// (2) an instance of <see cref="IPathMatchResult"/> stored under the
        /// key <see cref="ContextUtils.RegistryKeyPathMatchResult"/> with its
        /// <see cref="IPathMatchResult.UnboundRequestTarget"/> property set to the
        /// value of the <see cref="IQuasiHttpRequest.Target"/> property for the incoming
        /// request. The <see cref="IPathMatchResult.BoundPath"/> and 
        /// <see cref="IPathMatchResult.PathValues"/> properties will be set with
        /// empty or null values depending on whether the <see cref="IQuasiHttpRequest.Target"/>
        /// value is not null or null respectively.
        /// </para>
        /// </remarks>
        public IRegistry InitialHandlerVariables { get; set; }

        /// <summary>
        /// Gets or sets the "non-shadowable" contextual objects which will be used to populate an
        /// instance of <see cref="IContext"/> class for use by all handlers.
        /// </summary>
        /// <remarks>
        /// "Non-shadowable" here means "cannot be hidden", ie handlers cannot insert their own
        /// contextual objects at the top of the context's registry under the keys of
        /// the context objects in this property (they can try, but it won't take effect).
        /// <para>
        /// The following non-shadowable contextual objects are always exposed
        /// regardless of whether they are found initially inside this property:
        /// (1) an instance of <see cref="IContext"/>
        /// stored under the key <see cref="ContextUtils.RegistryKeyContext"/>,
        /// which is same as the context object that all handlers will receive;
        /// (2) an instance of <see cref="IContextRequest"/>
        /// stored under the key <see cref="ContextUtils.RegistryKeyRequest"/>,
        /// which is same as the <see cref="IContext.Request"/> property of
        /// the context that all handlers will receive;
        /// (3) an instance of <see cref="IContextResponse"/>
        /// stored under the key <see cref="ContextUtils.RegistryKeyResponse"/>,
        /// which is same as the <see cref="IContext.Response"/> property of
        /// the context that all handlers will receive;
        /// </para>
        /// </remarks>
        public IRegistry HandlerConstants { get; set; }

        /// <summary>
        /// Creates an instance of <see cref="IContext"/> class with the properties of this instance, and begins
        /// processing the pipeline of handlers set up in the <see cref="InitialHandlers"/> property.
        /// </summary>
        /// <param name="request">the quasi http response</param>
        /// <returns>a task whose result will be a quasi http response to the request</returns>
        /// <exception cref="MissingDependencyException">The <see cref="InitialHandlers"/> property is null</exception>
        public Task<IQuasiHttpResponse> ProcessRequest(IQuasiHttpRequest request)
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

            context.Start();

            return responseTransmmitter.Task;
        }
    }
}
