using Kabomu.Mediator.Path;
using Kabomu.Mediator.Registry;
using Kabomu.Mediator.RequestParsing;
using Kabomu.Mediator.ResponseRendering;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Mediator.Handling
{
    /// <summary>
    /// Provides constants and helper methods for working with <see cref="IContext"/> objects.
    /// </summary>
    public static class ContextUtils
    {
        /// <summary>
        /// Key usable for retrieving a context object from its own registry.
        /// </summary>
        public static readonly Type RegistryKeyContext = typeof(IContext);

        /// <summary>
        /// Key usable for retrieving a context's request object from its own registry.
        /// </summary>
        public static readonly Type RegistryKeyRequest = typeof(IContextRequest);

        /// <summary>
        /// Key usable for retrieving a context's response object from its own registry.
        /// </summary>
        public static readonly Type RegistryKeyResponse = typeof(IContextResponse);

        /// <summary>
        /// Key usable for storing and retrieving path template generators, ie instances of
        /// <see cref="IPathTemplateGenerator"/> class. Used by
        /// <see cref="GetPathTemplateGenerator"/> and HandlerUtils.Path* methods.
        /// </summary>
        public static readonly Type RegistryKeyPathTemplateGenerator = typeof(IPathTemplateGenerator);

        /// <summary>
        /// Key usable for storing and retrieving path template math results, ie instances
        /// of <see cref="IPathMatchResult"/> class. Used by
        /// <see cref="GetPathMatchResult"/> and HandlerUtils.Path* methods.
        /// </summary>
        public static readonly Type RegistryKeyPathMatchResult = typeof(IPathMatchResult);

        /// <summary>
        /// Key usable for storing and retrieving request parsers, ie instances of
        /// <see cref="IRequestParser"/> class. Used by <see cref="ContextExtensions.ParseRequest"/> method.
        /// </summary>
        public static readonly Type RegistryKeyRequestParser = typeof(IRequestParser);

        /// <summary>
        /// Key usable for storing and retrieving response renderer, ie instances of
        /// <see cref="IResponseRenderer"/> class. Used by <see cref="ContextExtensions.RenderResponse"/> method.
        /// </summary>
        public static readonly Type RegistryKeyResponseRenderer = typeof(IResponseRenderer);

        /// <summary>
        /// Key usable for storing and retrieving instances of <see cref="IUnexpectedEndHandler"/> class.
        /// Used by <see cref="ContextExtensions.HandleUnexpectedEnd"/> method.
        /// </summary>
        public static readonly Type RegistryKeyUnexpectedEndHandler = typeof(IUnexpectedEndHandler);

        /// <summary>
        /// Key usable for storing and retrieving error handlers, ie instances of <see cref="IServerErrorHandler"/> class.
        /// Used by <see cref="ContextExtensions.HandleError"/> method.
        /// </summary>
        public static readonly Type RegistryKeyServerErrorHandler = typeof(IServerErrorHandler);

        /// <summary>
        /// Used to indicate to instances of <see cref="IPathConstraint"/> classes that
        /// invocation is being done during path matching. It is equal to 1.
        /// </summary>
        public static readonly int PathConstraintMatchDirectionMatch = 1;

        /// <summary>
        /// Used to indicate to instances of <see cref="IPathConstraint"/> classes that
        /// invocation is being done during path interpolation. It is equal to 2.
        /// </summary>
        public static readonly int PathConstraintMatchDirectionFormat = 2;

        /// <summary>
        /// Creates an instance of <see cref="NoSuchParserException"/> class with
        /// error message describing a missing registry key.
        /// </summary>
        /// <param name="key">the key which was not found in a registry</param>
        /// <returns>new instance of <see cref="NoSuchParserException"/> class</returns>
        public static NoSuchParserException CreateNoSuchParserExceptionForKey(object key)
        {
            return new NoSuchParserException($"No appropriate request parser found under registry key: {key}");
        }

        /// <summary>
        /// Creates an instance of <see cref="NoSuchRendererException"/> class with
        /// error message describing a missing registry key.
        /// </summary>
        /// <param name="key">the key which was not found in a registry</param>
        /// <returns>new instance of <see cref="NotInRegistryException"/> class</returns>
        public static NoSuchRendererException CreateNoSuchRendererExceptionForKey(object key)
        {
            return new NoSuchRendererException($"No appropriate response renderer found under registry key: {key}");
        }
    }
}
