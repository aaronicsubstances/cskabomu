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
        public static readonly TypeBasedRegistryKey RegistryKeyContext =
            new TypeBasedRegistryKey(typeof(IContext));

        /// <summary>
        /// Key usable for retrieving a context's request object from its own registry.
        /// </summary>
        public static readonly TypeBasedRegistryKey RegistryKeyRequest =
            new TypeBasedRegistryKey(typeof(IContextRequest));

        /// <summary>
        /// Key usable for retrieving a context's response object from its own registry.
        /// </summary>
        public static readonly TypeBasedRegistryKey RegistryKeyResponse =
            new TypeBasedRegistryKey(typeof(IContextResponse));

        /// <summary>
        /// Key usable for storing and retrieving path template generators, ie instances of
        /// <see cref="IPathTemplateGenerator"/> class. Used by
        /// <see cref="GetPathTemplateGenerator"/> and HandlerUtils.Path* methods.
        /// </summary>
        public static readonly TypeBasedRegistryKey RegistryKeyPathTemplateGenerator =
            new TypeBasedRegistryKey(typeof(IPathTemplateGenerator));

        /// <summary>
        /// Key usable for storing and retrieving path template math results, ie instances
        /// of <see cref="IPathMatchResult"/> class. Used by
        /// <see cref="GetPathMatchResult"/> and HandlerUtils.Path* methods.
        /// </summary>
        public static readonly TypeBasedRegistryKey RegistryKeyPathMatchResult =
            new TypeBasedRegistryKey(typeof(IPathMatchResult));

        /// <summary>
        /// Key usable for storing and retrieving request parsers, ie instances of
        /// <see cref="IRequestParser"/> class. Used by <see cref="ContextExtensions.ParseRequest"/> method.
        /// </summary>
        public static readonly TypeBasedRegistryKey RegistryKeyRequestParser =
            new TypeBasedRegistryKey(typeof(IRequestParser));

        /// <summary>
        /// Key usable for storing and retrieving response renderer, ie instances of
        /// <see cref="IResponseRenderer"/> class. Used by <see cref="ContextExtensions.RenderResponse"/> method.
        /// </summary>
        public static readonly TypeBasedRegistryKey RegistryKeyResponseRenderer =
            new TypeBasedRegistryKey(typeof(IResponseRenderer));

        /// <summary>
        /// Key usable for storing and retrieving instances of <see cref="IUnexpectedEndHandler"/> class.
        /// Used by <see cref="ContextExtensions.HandleUnexpectedEnd"/> method.
        /// </summary>
        public static readonly TypeBasedRegistryKey RegistryKeyUnexpectedEndHandler =
            new TypeBasedRegistryKey(typeof(IUnexpectedEndHandler));

        /// <summary>
        /// Key usable for storing and retrieving error handlers, ie instances of <see cref="IServerErrorHandler"/> class.
        /// Used by <see cref="ContextExtensions.HandleError"/> method.
        /// </summary>
        public static readonly TypeBasedRegistryKey RegistryKeyServerErrorHandler =
            new TypeBasedRegistryKey(typeof(IServerErrorHandler));

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
        /// Gets the most current instance of <see cref="IPathMatchResult"/> class from a given registry using
        /// the key of <see cref="RegistryKeyPathMatchResult"/>. Key must be found or else exception will be thrown.
        /// </summary>
        /// <param name="registry">registry to retrieve from</param>
        /// <returns>most current path match result</returns>
        /// <exception cref="NotInRegistryException">If key of <see cref="RegistryKeyPathMatchResult"/>
        /// was not found.</exception>
        public static IPathMatchResult GetPathMatchResult(IRegistry registry)
        {
            return RegistryExtensions.Get<IPathMatchResult>(registry, RegistryKeyPathMatchResult);
        }

        /// <summary>
        /// Gets the most current instance of <see cref="IPathTemplateGenerator"/> class from a given registry using
        /// the key of <see cref="RegistryKeyPathTemplateGenerator"/>. Key must be found or else exception will be thrown.
        /// </summary>
        /// <param name="registry">registry to retrieve from</param>
        /// <returns>most current path template generator</returns>
        /// <exception cref="NotInRegistryException">If key of <see cref="RegistryKeyPathTemplateGenerator"/>
        /// was not found.</exception>
        public static IPathTemplateGenerator GetPathTemplateGenerator(IRegistry registry)
        {
            return RegistryExtensions.Get<IPathTemplateGenerator>(registry, RegistryKeyPathTemplateGenerator);
        }

        /// <summary>
        /// Generates a path template from a string specification and compatible options, using the most current instance of 
        /// <see cref="IPathTemplateGenerator"/> stored in a given registry under the key of
        /// <see cref="RegistryKeyPathTemplateGenerator"/>. Key must be found or else exception will be thrown.
        /// </summary>
        /// <param name="registry">registry to retrieve from</param>
        /// <param name="spec">string specification</param>
        /// <param name="options">options accompanying string spec</param>
        /// <returns>path template generated from spec with most current path template generator</returns>
        /// <exception cref="NotInRegistryException">If key of <see cref="RegistryKeyPathTemplateGenerator"/>
        /// was not found.</exception>
        public static IPathTemplate GeneratePathTemplate(IRegistry registry, string spec, object options)
        {
            var pathTemplateGenerator = GetPathTemplateGenerator(registry);
            IPathTemplate pathTemplate = pathTemplateGenerator.Parse(spec, options);
            return pathTemplate;
        }

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
