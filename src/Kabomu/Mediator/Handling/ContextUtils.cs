using Kabomu.Mediator.Path;
using Kabomu.Mediator.Registry;
using Kabomu.Mediator.RequestParsing;
using Kabomu.Mediator.ResponseRendering;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Mediator.Handling
{
    public static class ContextUtils
    {
        public static readonly TypeBasedRegistryKey TypePatternContext =
            new TypeBasedRegistryKey(typeof(IContext));

        public static readonly TypeBasedRegistryKey TypePatternRequest =
            new TypeBasedRegistryKey(typeof(IContextRequest));

        public static readonly TypeBasedRegistryKey TypePatternResponse =
            new TypeBasedRegistryKey(typeof(IContextResponse));

        public static readonly TypeBasedRegistryKey TypePatternPathTemplateGenerator =
            new TypeBasedRegistryKey(typeof(IPathTemplateGenerator));

        public static readonly TypeBasedRegistryKey TypePatternPathMatchResult =
            new TypeBasedRegistryKey(typeof(IPathMatchResult));

        public static readonly TypeBasedRegistryKey TypePatternRequestParser =
            new TypeBasedRegistryKey(typeof(IRequestParser));

        public static readonly TypeBasedRegistryKey TypePatternResponseRenderer =
            new TypeBasedRegistryKey(typeof(IResponseRenderer));

        public static readonly TypeBasedRegistryKey TypePatternUnexpectedEndHandler =
            new TypeBasedRegistryKey(typeof(IUnexpectedEndHandler));

        public static readonly TypeBasedRegistryKey TypePatternServerErrorHandler =
            new TypeBasedRegistryKey(typeof(IServerErrorHandler));

        public static readonly int PathConstraintMatchDirectionMatch = 1;

        public static readonly int PathConstraintMatchDirectionFormat = 2;
    }
}
