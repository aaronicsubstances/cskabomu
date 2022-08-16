﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Mediator.Path
{
    public interface IPathMatchResult
    {
        IDictionary<string, string> PathValues { get; }
        string BoundPathPortion { get; }
        string UnboundPathPortion { get; }
    }
}
