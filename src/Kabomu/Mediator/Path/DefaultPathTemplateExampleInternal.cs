using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Mediator.Path
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// To specify exact matches, specify the exact match as only 1 example,
    /// and set the values of the <see cref="MatchLeadingSlash"/> and <see cref="MatchTrailingSlash"/>
    /// options to non-null values which correspond with the exact match.
    /// </remarks>
    internal class DefaultPathTemplateExampleInternal
    {
        public bool? CaseSensitiveMatchEnabled { get; set; }
        public bool? MatchLeadingSlash { get; set; }
        public bool? MatchTrailingSlash { get; set; }
        public bool? UnescapeNonWildCardSegments { get; set; }
        public IList<PathToken> Tokens { get; set; }

        public class PathToken
        {
            public const int TokenTypeLiteral = 1;

            public const int TokenTypeSegment = 2;

            public const int TokenTypeWildCard = 3;

            public int Type;
            public string Value;
            public bool EmptySegmentAllowed;
        }
    }
}
