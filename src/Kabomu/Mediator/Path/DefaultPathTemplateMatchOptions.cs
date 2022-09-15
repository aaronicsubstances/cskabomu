using System;

namespace Kabomu.Mediator.Path
{
    /// <summary>
    /// Path match options for <see cref="IPathTemplate"/> instances created by
    /// <see cref="DefaultPathTemplateGenerator"/> instances.
    /// </summary>
    public class DefaultPathTemplateMatchOptions
    {
        /// <summary>
        /// Gets or sets a value which indicates whether a path must have a leading slash for
        /// it to successfully match.
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        ///     <item>A value of true means that a path to be matched must a leading slash.
        /// </item>
        ///     <item>A value of false means a path to be matched must not have a leading slash. </item>
        ///     <item>A value of null means it does
        /// not matter whether or not a path to be matched has a leading slash.</item>
        /// </list>
        /// </remarks>
        public bool? MatchLeadingSlash { get; set; }

        /// <summary>
        /// Gets or sets a value which indicates whether a path must have a trailing slash for
        /// it to successfully match.
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        ///     <item>A value of true means that a path to be matched must a trailing slash.</item>
        ///     <item>A value of false means a path to be matched must not have a trailing slash. </item>
        ///     <item>A value of null means it does
        /// not matter whether or not a path to be matched has a trailing slash.</item>
        /// </list>
        /// </remarks>
        public bool? MatchTrailingSlash { get; set; }

        /// <summary>
        /// Gets or sets a value which indicates how matching template literal segments path should consider case of characters.
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        ///   <item>A value of true means that a path to be matched should match case of literal segments in templates.</item>
        ///   <item>A value of false or null means a path to be matched should ignore case of literal segments in templates.</item>
        /// </list>
        /// </remarks>
        public bool? CaseSensitiveMatchEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value which indicates whether captured non literal (but non wilcard) path segments 
        /// should be unescaped of URL percent encodings. Wild card segments are never unescaped.
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        ///   <item>A value of false means that captured non literal (but non wilcard) path segments should not
        ///   be unescaped.</item>
        ///   <item>A value of true or null means that captured non literal (but non wilcard) path segments should
        ///   be unescaped.</item>
        /// </list>
        /// </remarks>
        public bool? UnescapeNonWildCardSegments { get; set; }
    }
}