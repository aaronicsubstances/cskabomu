using System;

namespace Kabomu.Mediator.Path
{
    public class DefaultPathTemplateMatchOptions
    {
        public bool? MatchLeadingSlash { get; set; }
        public bool? MatchTrailingSlash { get; set; }
        public bool? CaseSensitiveMatchEnabled { get; set; }
        public bool? UnescapeNonWildCardSegments { get; set; }
    }
}