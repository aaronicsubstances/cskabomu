using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Mediator.Path
{
    public class DefaultPathTemplateFormatOptions
    {
        public bool? ApplyLeadingSlash { get; set; }
        public bool? ApplyTrailingSlash { get; set; }
        public bool? ApplyConstraints { get; set; }
        public bool? CaseSensitiveMatchEnabled { get; set; }
        public bool? EscapeNonWildCardSegments { get; set; }
    }
}
