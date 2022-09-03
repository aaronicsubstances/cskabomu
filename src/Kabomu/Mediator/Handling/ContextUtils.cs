﻿using Kabomu.Mediator.Path;
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
        public static readonly TypeBasedRegistryKey RegistryKeyContext =
            new TypeBasedRegistryKey(typeof(IContext));

        public static readonly TypeBasedRegistryKey RegistryKeyRequest =
            new TypeBasedRegistryKey(typeof(IContextRequest));

        public static readonly TypeBasedRegistryKey RegistryKeyResponse =
            new TypeBasedRegistryKey(typeof(IContextResponse));

        public static readonly TypeBasedRegistryKey RegistryKeyPathTemplateGenerator =
            new TypeBasedRegistryKey(typeof(IPathTemplateGenerator));

        public static readonly TypeBasedRegistryKey RegistryKeyPathMatchResult =
            new TypeBasedRegistryKey(typeof(IPathMatchResult));

        public static readonly TypeBasedRegistryKey RegistryKeyRequestParser =
            new TypeBasedRegistryKey(typeof(IRequestParser));

        public static readonly TypeBasedRegistryKey RegistryKeyResponseRenderer =
            new TypeBasedRegistryKey(typeof(IResponseRenderer));

        public static readonly TypeBasedRegistryKey RegistryKeyUnexpectedEndHandler =
            new TypeBasedRegistryKey(typeof(IUnexpectedEndHandler));

        public static readonly TypeBasedRegistryKey RegistryKeyServerErrorHandler =
            new TypeBasedRegistryKey(typeof(IServerErrorHandler));

        public static readonly int PathConstraintMatchDirectionMatch = 1;

        public static readonly int PathConstraintMatchDirectionFormat = 2;
    }
}
