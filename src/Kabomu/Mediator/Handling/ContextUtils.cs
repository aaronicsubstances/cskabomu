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
        public static readonly IRegistryKeyPattern TypePatternContext =
            new TypeRegistryKeyPattern(typeof(IContext));

        public static readonly IRegistryKeyPattern TypePatternRequest =
            new TypeRegistryKeyPattern(typeof(IContextRequest));

        public static readonly IRegistryKeyPattern TypePatternResponse =
            new TypeRegistryKeyPattern(typeof(IContextResponse));

        public static readonly IRegistryKeyPattern TypePatternPathMatchResult =
            new TypeRegistryKeyPattern(typeof(IPathMatchResult));

        public static readonly IRegistryKeyPattern TypePatternRequestParser =
            new TypeRegistryKeyPattern(typeof(IRequestParser));

        public static readonly IRegistryKeyPattern TypePatternResponseRenderer =
            new TypeRegistryKeyPattern(typeof(IResponseRenderer));

        public static readonly IRegistryKeyPattern TypePatternUnexpectedEndHandler =
            new TypeRegistryKeyPattern(typeof(IUnexpectedEndHandler));

        public static readonly IRegistryKeyPattern TypePatternServerErrorHandler =
            new TypeRegistryKeyPattern(typeof(IServerErrorHandler));

        public static readonly int PathConstraintMatchDirectionMatch = 1;

        public static readonly int PathConstraintMatchDirectionFormat = 2;
    }
}
