using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Mediator.Path
{
    public interface IPathTemplateFormatOptions
    {
        bool? ApplyLeadingSlash { get; }
        bool? ApplyTrailingSlash { get; }
        bool? ApplyConstraints { get; }
        bool? EscapeNonWildCardSegments { get; }
    }
}
