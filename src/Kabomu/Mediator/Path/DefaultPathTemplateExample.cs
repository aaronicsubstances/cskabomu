using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Mediator.Path
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// To specify exact matches, specify the exact match in only 1 sample,
    /// and set the values of the <see cref="MatchLeadingSlash"/> and <see cref="MatchTrailingSlash"/>
    /// properties to non-null values which correspond with the exact match.
    /// </remarks>
    public class DefaultPathTemplateExample
    {
        public bool? CaseSensitiveMatchEnabled { get; set; }
        public bool? MatchLeadingSlash { get; set; }
        public bool? MatchTrailingSlash { get; set; }
        public bool? UnescapeNonWildCardSegments { get; set; }
        public IList<string> Samples { get; set; }
        internal IList<DefaultPathToken> ParsedSamples { get; set; }
    }
}
