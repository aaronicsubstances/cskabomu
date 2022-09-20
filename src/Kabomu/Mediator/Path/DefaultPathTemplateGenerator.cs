using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using static Kabomu.Mediator.Path.DefaultPathTemplateExampleInternal;

namespace Kabomu.Mediator.Path
{
    /// <summary>
    /// Provides the default path template generation algorithm used in the Kabomu.Mediator framework. Inspired by
    /// <a href="https://learn.microsoft.com/en-us/aspnet/core/fundamentals/routing?view=aspnetcore-6.0">Routing in ASP.NET Core</a>,
    /// it contains concepts similar to ASP.NET Core routing concepts of templates, catch-all parameters,
    /// default values, and constraints.
    /// </summary>
    /// <remarks>
    /// This class generates path templates out of CSV specifications.
    /// <br/>
    /// One notable difference between those specs and ASP.NET Core routing templates is that
    /// path segments with default values are treated almost like optional path segments.
    /// They can be only be specified indirectly by
    /// generating all possible routing templates with and without segments which can be optional or have a default values,
    /// from shortest to longest.
    /// Default values are then specified separately for storage in a dictionary.
    /// <para>
    /// <example>
    /// As an example, an ASP.NET Core routing template of "{controller=Home}/{action=Index}/{id?}" corresponds to the CSV below:
    /// <code>
    /// "/,//controller,//controller//action,//controller//action//id\n
    /// defaults:,controller,Home,action,Index"
    /// </code>
    /// </example>
    /// </para>
    /// <example>
    /// Another ASP.NET Core routing template of "blog/{article:minlength(10)}/**path" corresponds to:
    /// <code>
    /// "/blog//article///path\n
    /// check:article,minlength,10"
    /// </code>
    /// </example>
    /// <para>
    /// Another difference is that the CSV specs do not allow for separators between path segment expressions other
    /// than forward slashes. So the ASP.NET Core routing template of "{country}-{region}" does not have a direct translation.
    /// </para>
    /// <para>
    /// Each CSV row is of one of these formats:
    /// <br/>
    /// - an empty row. Useful for visually sectioning parts of a CSV spec.
    /// <br/>
    /// - first column starts with forward slash ("/") or slash for short. each column including the first
    /// must be a path matching expression containing zero or more path variables.
    /// <br/>
    /// - first column starts with "name:". the suffix after "name:" becomes the non-unique label for the remaining columns as a group.
    /// such a group can be targetted if the label is present as a key in any dictionary of match options available.
    /// the second and remaining columns must be path matching expressions.
    /// <br/>
    /// - first column starts with "defaults:". the second and remaining columns are interpreted as a list of alternating 
    /// key value pairs, which will be used to populate a map of default values. the keys correspond to path variables
    /// which may be present in path matching expressions.
    /// <br/>
    /// - first column starts with "check:". then the suffix after "check:" will be taken as a path variable which may
    /// be present in path matching expressions. the second column must be a key mapped to a constraint function in a dictionary of
    /// such functions. the third and remaining columns are interpreted as a list of arguments which should be stored and passed to 
    /// the constraint function every time path matching or interpolation is requested later on.
    /// <br/>
    /// - first column is empty. then current row must not be the first row, and the
    /// previous row must not be an empty row. in this case the first column will be treated as if its value equals that of the
    /// nearest non-empty first column above current row. the rest of the columns will be processed accordingly.
    /// </para>
    /// A path matching expression is either a single slash, or a concatenation of at least one of the following
    /// segment expressions:
    /// <list type="bullet">
    /// <item>single slash followed by one or more non-slash characters. indicates a literal path segment expression</item>
    /// <item>double slash followed by one or more non-slash characters. indicates a single path segment expression.
    /// The non-slash characters become a path variable key</item>
    /// <item>triple slash followed by one or more non-slash characters, and is a wild card segment expression for matching zero
    /// or more path segments. The non-slash characters become a path variable key</item>
    /// </list>
    /// Within a path matching expression,
    /// <list type="bullet">
    /// <item>all non-slash characters for a literal, single or wild card segment expressions will be trimmed of
    /// surrounding whitespace.</item>
    /// <item>path variables must be unique</item>
    /// <item>at most only 1 wild card segment expression may be present</item>
    /// <item>empty path segments are not matched by single path segment expressions by default.
    /// </item>
    /// </list>
    /// Check online references (e.g. project github repo) for examples of using this class to generate path templates,
    /// for further documentation on the CSV specification, and how the generated path templates match and interpolate request paths.
    /// </remarks>
    public class DefaultPathTemplateGenerator : IPathTemplateGenerator
    {
        private static readonly Regex SpecSegmentRegex = new Regex("(/{1,3})([^/]+)");
        private static readonly Regex SpecLeadingWsRegex = new Regex(@"^\s*");
        private static readonly Regex UnnamedSpecRegex = new Regex(@"^\s*/");
        private static readonly Regex DefaultsKeyRegex = new Regex(@"^(?i)\s*defaults\s*:");
        private static readonly Regex ConstraintKeyRegex = new Regex(@"^(?is)\s*check\s*:(.*)$");
        private static readonly Regex SpecNameKeyRegex = new Regex(@"^(?is)\s*name\s*:(.*)$");
        private static readonly Regex RepeatKeyRegex = new Regex(@"^\s*$");

        private static readonly int ReferenceKeyDefaults = 1;
        private static readonly int ReferenceKeyConstraint = 2;
        private static readonly int ReferenceKeySpecName = 3;

        /// <summary>
        /// Gets or sets a named map of constraint functions which will be used by
        /// generated templates for path matching and interpolation.
        /// </summary>
        public IDictionary<string, IPathConstraint> ConstraintFunctions { get; set; }

        /// <summary>
        /// Creates a path template from a given CSV specification with options, as described in the
        /// documentation of this class.
        /// </summary>
        /// <remarks>
        /// Valid options object which can accompany a CSV specification are
        /// <list type="bullet">
        /// <item>null</item>
        /// <item>an instance of <see cref="DefaultPathTemplateMatchOptions"/></item>
        /// <item>an instance of <see cref="IDictionary{TKey, TValue}"/> class
        /// with string keys and values of type <see cref="DefaultPathTemplateMatchOptions"/>. 
        /// Each key should be the label of a group of request path
        /// mathing expressions in the CSV specification</item>
        /// </list>
        /// </remarks>
        /// <param name="spec">the CSV specification of the path template.</param>
        /// <param name="options">optional object accompanying the CSV specification</param>
        /// <returns>path template instance</returns>
        /// <exception cref="PathTemplateException">If an error occurs due to invalid arguments</exception>
        public IPathTemplate Parse(string spec, object options)
        {
            try
            {
                return ParseInternal(spec, options);
            }
            catch (Exception e)
            {
                if (e is PathTemplateException)
                {
                    throw;
                }
                throw new PathTemplateException(null, e);
            }
        }

        private IPathTemplate ParseInternal(string spec, object options)
        {
            if (spec == null)
            {
                throw new ArgumentNullException(nameof(spec));
            }

            DefaultPathTemplateMatchOptions optionsForAll = null;
            IDictionary<string, DefaultPathTemplateMatchOptions> individualOptions = null;
            if (options != null)
            {
                if (options is IDictionary<string, DefaultPathTemplateMatchOptions>)
                {
                    individualOptions = (IDictionary<string, DefaultPathTemplateMatchOptions>)options;
                }
                else
                {
                    optionsForAll = (DefaultPathTemplateMatchOptions)options;
                }
            }

            var parsedCsv = CsvUtils.Deserialize(spec);

            var parsedExamples = new List<DefaultPathTemplateExampleInternal>();
            var defaultValues = new Dictionary<string, string>();

            var allConstraints = new Dictionary<string, IList<(string, string[])>>();

            // Copy over used constraints to make them available even after 
            // an update to this generator.
            var usedConstraintFunctions = new Dictionary<string, IPathConstraint>();

            int referenceKey = 0;
            string referenceAfterKey = null;

            for (int i = 0; i < parsedCsv.Count; i++)
            {
                int rowNum = i + 1;
                var row = parsedCsv[i];

                string firstColEntry = null;
                if (row.Count > 0)
                {
                    firstColEntry = row[0];
                }

                // identify type of row and process accordingly.
                Match m;
                if (firstColEntry == null || UnnamedSpecRegex.Match(firstColEntry).Success)
                {
                    var parsedRow = ParseExamples(rowNum, row, 0, optionsForAll);
                    parsedExamples.AddRange(parsedRow);

                    // cancel reference points of empty keys.
                    referenceKey = 0;
                    referenceAfterKey = null;
                }
                else if (DefaultsKeyRegex.Match(firstColEntry).Success)
                {
                    referenceKey = ReferenceKeyDefaults;
                    ParseDefaultValues(row, defaultValues);
                }
                else if ((m = ConstraintKeyRegex.Match(firstColEntry)).Success)
                {
                    referenceKey = ReferenceKeyConstraint;
                    string targetValueKey = m.Groups[1].Value;
                    referenceAfterKey = targetValueKey;
                    ParseConstraints(rowNum, row, targetValueKey, allConstraints, usedConstraintFunctions);
                }
                else if ((m = SpecNameKeyRegex.Match(firstColEntry)).Success)
                {
                    referenceKey = ReferenceKeySpecName;
                    string name = m.Groups[1].Value;
                    referenceAfterKey = name;
                    DefaultPathTemplateMatchOptions optionToUse = null;
                    if (individualOptions != null && individualOptions.ContainsKey(name))
                    {
                        optionToUse = individualOptions[name];
                    }
                    var parsedRow = ParseExamples(rowNum, row, 1, optionToUse);
                    parsedExamples.AddRange(parsedRow);
                }
                else if (RepeatKeyRegex.Match(firstColEntry).Success)
                {
                    if (referenceKey == 0)
                    {
                        throw AbortParse(rowNum, 1, "empty key found at the " +
                            "beginning of CSV or just after empty CSV row");
                    }
                    if (referenceKey == ReferenceKeyDefaults)
                    {
                        ParseDefaultValues(row, defaultValues);
                    }
                    else if (referenceKey == ReferenceKeyConstraint)
                    {
                        string targetValueKey = referenceAfterKey;
                        ParseConstraints(rowNum, row, targetValueKey, allConstraints, usedConstraintFunctions);
                    }
                    else if (referenceKey == ReferenceKeySpecName)
                    {
                        string name = referenceAfterKey;
                        DefaultPathTemplateMatchOptions optionToUse = null;
                        if (individualOptions != null && individualOptions.ContainsKey(name))
                        {
                            optionToUse = individualOptions[name];
                        }
                        var parsedRow = ParseExamples(rowNum, row, 1, optionToUse);
                        parsedExamples.AddRange(parsedRow);
                    }
                    else
                    {
                        throw new ExpectationViolationException($"unexpected reference key: {referenceKey}");
                    }
                }
                else
                {
                    if (firstColEntry.Contains("/"))
                    {
                        throw AbortParse(rowNum, 1, "missing leading slash");
                    }
                    else
                    {
                        throw AbortParse(rowNum, 1, $"unknown key: {row[0]}");
                    }
                }
            }

            if (parsedExamples.Count == 0)
            {
                var lastColNum = 0;
                if (parsedCsv.Count > 0)
                {
                    lastColNum = parsedCsv[parsedCsv.Count - 1].Count;
                }
                throw AbortParse(parsedCsv.Count, lastColNum, "no examples specified");
            }

            // remove unnecessary escapes from all literal tokens after just determining
            // which of them are exempt from escaping.
            RemoveUnnecessaryUriEscapes(parsedExamples);

            var pathTemplate = new DefaultPathTemplateInternal
            {
                ParsedExamples = parsedExamples
            };
            if (defaultValues.Count > 0)
            {
                pathTemplate.DefaultValues = defaultValues;
            }
            if (allConstraints.Count > 0)
            {
                pathTemplate.AllConstraints = allConstraints;
                pathTemplate.ConstraintFunctions = usedConstraintFunctions;
            }

            return pathTemplate;
        }

        private Exception AbortParse(int rowNum, int colNum, string msg)
        {
            throw new PathTemplateException($"parse error in CSV at row {rowNum} column {colNum}: {msg}");
        }

        private IList<DefaultPathTemplateExampleInternal> ParseExamples(int rowNum, IList<string> row,
            int startColIndex, DefaultPathTemplateMatchOptions options)
        {
            var parsedExamples = new List<DefaultPathTemplateExampleInternal>();
            for (int i = startColIndex; i < row.Count; i++)
            {
                var src = row[i];
                var tokens = ParseExample(rowNum, i + 1, src);
                var parsedExample = new DefaultPathTemplateExampleInternal
                {
                    Tokens = tokens
                };
                parsedExamples.Add(parsedExample);
                if (options != null)
                {
                    SetParsedExampleOptions(parsedExample, options);
                }
            }
            return parsedExamples;
        }

        private IList<PathToken> ParseExample(int rowNum, int colNum, string src)
        {
            // Interpret path spec as either 
            //  1. a single slash, or
            //  2. one or more concatenations of /literal or //segment or ///wildcard.
            // where literal, segment or wildcard have the ff x'tics:
            //  a. cannot be empty
            //  b. cannot contain slashes
            //  c. surrounding whitespace will be trimmed off.
            //  d. a non wild card segment surrounded by whitespace will be interpreted 
            //     to mean it allows for empty values (wild card matches can always match empty segments).

            int wildCardChPos = -1;
            var nonLiteralNames = new HashSet<string>();
            var tokens = new List<PathToken>();

            // remove leading whitespace, but keep track of starting position
            // of first non whitespace char for error reporting purposes.
            Match m = SpecLeadingWsRegex.Match(src);
            if (!m.Success)
            {
                throw new ExpectationViolationException("SpecLeadingWsRegex match failed");
            }
            if (m.Index != 0)
            {
                throw new ExpectationViolationException($"SpecLeadingWsRegex match index {m.Index} != 0");
            }
            int startIndex = m.Length;

            // deal specially with '/' which is allowed to yield empty set of tokens.
            if (startIndex == src.Length - 1 && src[startIndex] == '/')
            {
                // return empty tokens
                return tokens;
            }
            while (startIndex < src.Length)
            {
                int startChPos = startIndex + 1;
                m = SpecSegmentRegex.Match(src, startIndex);
                if (!m.Success || m.Index != startIndex)
                {
                    throw AbortParse(rowNum, colNum, $"invalid spec seen at char pos {startChPos}");
                }
                string tokenTypeIndicator = m.Groups[1].Value;
                int tokenType;
                switch (tokenTypeIndicator)
                {
                    case "/":
                        tokenType = PathToken.TokenTypeLiteral;
                        break;
                    case "//":
                        tokenType = PathToken.TokenTypeSegment;
                        break;
                    case "///":
                        tokenType = PathToken.TokenTypeWildCard;
                        break;
                    default:
                        throw new ExpectationViolationException("unexpected token type indicator: " +
                            tokenTypeIndicator);
                }

                string tokenValueIndicator = m.Groups[2].Value;
                string tokenValue = tokenValueIndicator.Trim();
                bool emptyValueAllowed = tokenValueIndicator != tokenValue;

                if (tokenType != PathToken.TokenTypeLiteral)
                {
                    if (nonLiteralNames.Contains(tokenValue))
                    {
                        throw AbortParse(rowNum, colNum, $"duplicate use of segment name at char pos {startChPos}");
                    }
                    nonLiteralNames.Add(tokenValue);
                }
                if (tokenType == PathToken.TokenTypeWildCard)
                {
                    if (wildCardChPos != -1)
                    {
                        throw AbortParse(rowNum, colNum, "duplicate specification of wild card segment " +
                            $"at char pos {startChPos} " +
                            $"(wild card segment already specified at {wildCardChPos})");
                    }
                    wildCardChPos = startChPos;
                }

                // add new token
                var token = new PathToken
                {
                    Type = tokenType,
                    Value = tokenValue,
                    EmptySegmentAllowed = emptyValueAllowed
                };
                tokens.Add(token);

                // advance loop
                startIndex += m.Length;
            }

            if (tokens.Count == 0)
            {
                throw AbortParse(rowNum, colNum, "encountered blank string spec");
            }

            return tokens;
        }

        private static void SetParsedExampleOptions(DefaultPathTemplateExampleInternal parsedExample,
            DefaultPathTemplateMatchOptions option)
        {
            parsedExample.UnescapeNonWildCardSegments = option.UnescapeNonWildCardSegments;
            parsedExample.CaseSensitiveMatchEnabled = option.CaseSensitiveMatchEnabled;
            parsedExample.MatchLeadingSlash = option.MatchLeadingSlash;
            parsedExample.MatchTrailingSlash = option.MatchTrailingSlash;
        }

        private void ParseDefaultValues(IList<string> row, Dictionary<string, string> defaultValues)
        {
            for (int i = 1; i < row.Count; i += 2)
            {
                var defaultKey = row[i];
                string defaultValue = null;
                if (i + 1 < row.Count)
                {
                    defaultValue = row[i + 1];
                }
                if (defaultValues.ContainsKey(defaultKey))
                {
                    defaultValues[defaultKey] = defaultValue;
                }
                else
                {
                    defaultValues.Add(defaultKey, defaultValue);
                }
            }
        }

        private void ParseConstraints(int rowNum, IList<string> row, string targetValueKey,
            IDictionary<string, IList<(string, string[])>> allConstraints,
            IDictionary<string, IPathConstraint> usedConstraintFunctions)
        {
            if (row.Count <= 1)
            {
                return;
            }
            string constraintFunctionId = row[1];

            if (ConstraintFunctions == null || !ConstraintFunctions.ContainsKey(constraintFunctionId))
            {
                throw AbortParse(rowNum, 2, $"constraint function '{constraintFunctionId}' not found");
            }
            if (!usedConstraintFunctions.ContainsKey(constraintFunctionId))
            {
                var constraintFunction = ConstraintFunctions[constraintFunctionId];
                if (constraintFunction == null)
                {
                    throw AbortParse(rowNum, 2, "null constraint function found");
                }
                usedConstraintFunctions.Add(constraintFunctionId, constraintFunction);
            }
            IList<(string, string[])> targetValueConstraints;
            if (allConstraints.ContainsKey(targetValueKey))
            {
                targetValueConstraints = allConstraints[targetValueKey];
            }
            else
            {
                targetValueConstraints = new List<(string, string[])>();
                allConstraints.Add(targetValueKey, targetValueConstraints);
            }
            var constraintFunctionArgs = row.Skip(2).ToArray();
            targetValueConstraints.Add(ValueTuple.Create(constraintFunctionId, constraintFunctionArgs));
        }

        private static void RemoveUnnecessaryUriEscapes(IList<DefaultPathTemplateExampleInternal> parsedExamples)
        {
            foreach (var parsedExample in parsedExamples)
            {
                if (parsedExample.UnescapeNonWildCardSegments == false)
                {
                    continue;
                }
                foreach (var token in parsedExample.Tokens)
                {
                    if (token.Type == PathToken.TokenTypeLiteral)
                    {
                        token.Value = PathUtilsInternal.ReverseUnnecessaryUriEscapes(token.Value);
                    }
                }
            }
        }
    }
}
