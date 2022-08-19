using Kabomu.Common;
using Kabomu.Mediator.Handling;
using System;
using System.Collections.Generic;
using System.Linq;
using static Kabomu.Mediator.Path.DefaultPathTemplateExampleInternal;

namespace Kabomu.Mediator.Path
{
    internal class DefaultPathTemplate : IPathTemplate
    {
        public DefaultPathTemplate()
        {
        }

        public IList<DefaultPathTemplateExampleInternal> ParsedExamples { get; set; }
        public IDictionary<string, string> DefaultValues { get; set; }
        public Dictionary<string, IList<(string, string[])>> AllConstraints { get; set; }
        public IDictionary<string, IPathConstraint> ConstraintFunctions { get; set; }

        public string Interpolate(IContext context, IDictionary<string, string> pathValues,
            object opaqueOptionObj)
        {
            var possibilities = InterpolateAll(context, pathValues, opaqueOptionObj);

            // pick shortest string. break ties by ordering of parsed examples.
            // stable sort needed so can't use List.sort
            var shortest = possibilities.OrderBy(x => x.Length).FirstOrDefault();
            if (shortest == null)
            {
                throw new Exception("could not interpolate template with the provided arguments");
            }
            return shortest;
        }

        public IList<string> InterpolateAll(IContext context, IDictionary<string, string> pathValues,
            object opaqueOptionObj)
        {
            var options = (DefaultPathTemplateFormatOptions)opaqueOptionObj;

            var candidates = new List<string>();
            for (int i = 0; i < ParsedExamples.Count; i++)
            {
                var candidate = TryInterpolate(i, context, pathValues, options);
                if (candidate != null)
                {
                    candidates.Add(candidate);
                }
            }
            return candidates;
        }

        private string TryInterpolate(int parsedExampleIndex, IContext context, IDictionary<string, string> pathValues,
            DefaultPathTemplateFormatOptions options)
        {
            var parsedExample = ParsedExamples[parsedExampleIndex];

            // by default apply constraints.
            var applyConstraints = options?.ApplyConstraints ?? true;

            var escapeNonWildCardSegment = PathUtilsInternal.GetEffectiveEscapeNonWildCardSegment(options, parsedExample);

            var segments = new List<string>();
            var tokens = parsedExample.Tokens;
            var wildCardTokenSeen = false;
            foreach (var token in tokens)
            {
                if (token.Type == PathToken.TokenTypeLiteral)
                {
                    segments.Add(token.Value);
                }
                else if (token.Type == PathToken.TokenTypeSegment ||
                    token.Type == PathToken.TokenTypeWildCard)
                {
                    if (token.Type == PathToken.TokenTypeWildCard)
                    {
                        if (wildCardTokenSeen)
                        {
                            throw new ExpectationViolationException("wildCardTokenProcessed is true");
                        }
                        wildCardTokenSeen = true;
                    }
                    var valueKey = token.Value;
                    string pathValue;
                    if (pathValues.ContainsKey(token.Value))
                    {
                        pathValue = pathValues[valueKey];
                        if (applyConstraints && AllConstraints != null &&
                            AllConstraints.ContainsKey(valueKey))
                        {
                            var valueConstraints = AllConstraints[valueKey];
                            var ok = PathUtilsInternal.ApplyValueConstraints(this, context, pathValues, valueKey, valueConstraints,
                                ContextUtils.PathConstraintMatchDirectionFormat);
                            if (!ok)
                            {
                                return null;
                            }
                        }
                    }
                    else
                    {
                        if (DefaultValues != null && DefaultValues.ContainsKey(valueKey))
                        {
                            pathValue = DefaultValues[valueKey];
                        }
                        else
                        {
                            // no path value provided, meaning parsed example is not meant to be used.
                            return null;
                        }
                    }
                    if (token.Type == PathToken.TokenTypeWildCard && pathValue == null)
                    {
                        // no wild card segment needed.
                        continue;
                    }
                    if (pathValue == null)
                    {
                        pathValue = PathUtilsInternal.ConvertPossibleNullToString(pathValue);
                    }
                    if (token.Type == PathToken.TokenTypeSegment && escapeNonWildCardSegment)
                    {
                        pathValue = PathUtilsInternal.EncodeAlmostEveryUriChar(pathValue);
                    }
                    segments.Add(pathValue);
                }
                else
                {
                    throw new ExpectationViolationException("unexpected token type: " +
                        token.Type);
                }
            }

            // check that other unused non-literal tokens are satisfied from default values
            // if specified in path values.
            if (!PathUtilsInternal.AreAllRelevantPathValuesSatisfiedFromDefaultValues(
                pathValues, options?.CaseSensitiveMatchEnabled,
                ParsedExamples, parsedExampleIndex, DefaultValues))
            {
                return null;
            }

            // if our segments are ready then proceed to join them with intervening slashes
            // and carefully surround with sentinel slashes.
            bool applyLeadingSlash = PathUtilsInternal.GetEffectiveApplyLeadingSlash(options, parsedExample);
            bool applyTrailingSlash = PathUtilsInternal.GetEffectiveApplyTrailingSlash(options, parsedExample);
            string path;
            if (tokens.Count == 0)
            {
                if (applyLeadingSlash || applyTrailingSlash)
                {
                    path = "/";
                }
                else
                {
                    path = "";
                }
            }
            else
            {
                string prefix = applyLeadingSlash ? "/" : "";
                string suffix = applyTrailingSlash ? "/" : "";
                path = prefix + string.Join("/", segments) + suffix;
            }
            return path;
        }

        public IPathMatchResult Match(IContext context, string requestTarget)
        {
            var path = PathUtilsInternal.ExtractPath(requestTarget);

            var segments = PathUtilsInternal.NormalizeAndSplitPath(path);

            var pathValues = new Dictionary<string, string>();
            DefaultPathTemplateExampleInternal matchingExample = null;
            string wildCardMatch = null;
            foreach (var parsedExample in ParsedExamples)
            {
                // deal with requirements of matching surrounding slashes while
                // treating single slash path ('/') specially.
                if (path == "/")
                {
                    // we choose to always make '/' match an empty set of tokens if MatchLeadingSlash and
                    // MatchTrailingSlash are not both false at the same time, by interpreting '/' as
                    //  1. both a leading and trailing slash, if both MatchLeadingSlash and MatchLeadingSlash are true
                    //  2. trailing but not leading slash, if MatchLeadingSlash is not true and MatchTrailingSlash is true
                    //  3. leading but not trailing slash, if MatchTrailingSlash is not true and MatchLeadingSlash is true
                    if (parsedExample.MatchLeadingSlash == false && parsedExample.MatchTrailingSlash == false)
                    {
                        continue;
                    }
                }
                else
                {
                    if (parsedExample.MatchLeadingSlash.HasValue &&
                        path.StartsWith("/") != parsedExample.MatchLeadingSlash.Value)
                    {
                        continue;
                    }
                    if (parsedExample.MatchTrailingSlash.HasValue &&
                        path.EndsWith("/") != parsedExample.MatchTrailingSlash.Value)
                    {
                        continue;
                    }
                }

                var matchAttempt = TryMatch(path, segments, parsedExample, pathValues);
                if (matchAttempt.Item1)
                {
                    matchingExample = parsedExample;
                    wildCardMatch = matchAttempt.Item2;
                    break;
                }
            }
            if (matchingExample == null)
            {
                return null;
            }

            // apply constraint functions.
            if (AllConstraints != null)
            {
                foreach (var e in AllConstraints)
                {
                    if (!pathValues.ContainsKey(e.Key))
                    {
                        continue;
                    }
                    bool ok = PathUtilsInternal.ApplyValueConstraints(this, context, pathValues,
                        e.Key, e.Value, ContextUtils.PathConstraintMatchDirectionMatch);
                    if (!ok)
                    {
                        return null;
                    }
                }
            }

            // add default values.
            if (DefaultValues != null)
            {
                foreach (var e in DefaultValues)
                {
                    if (!pathValues.ContainsKey(e.Key))
                    {
                        pathValues.Add(e.Key, e.Value);
                    }
                }
            }

            string boundPathPortion = path, unboundPathPortion = "";
            if (matchingExample.Tokens.Count > 0 &&
                matchingExample.Tokens[matchingExample.Tokens.Count - 1].Type == PathToken.TokenTypeWildCard &&
                wildCardMatch != null)
            {
                unboundPathPortion = wildCardMatch;
                if (!path.EndsWith(unboundPathPortion))
                {
                    throw new ExpectationViolationException($"{path} does not end with {unboundPathPortion}");
                }
                boundPathPortion = path.Substring(0, path.Length - unboundPathPortion.Length);
            }

            var result = new DefaultPathMatchResult
            {
                PathValues = pathValues,
                BoundPathPortion = boundPathPortion,
                UnboundPathPortion = unboundPathPortion
            };
            return result;
        }

        private (bool, string) TryMatch(string path, IList<string> segments,
            DefaultPathTemplateExampleInternal parsedExample, IDictionary<string, string> pathValues)
        {
            IList<PathToken> tokens = parsedExample.Tokens;

            int wildCardTokenIndex = -1;
            for (int i = 0; i < tokens.Count; i++)
            {
                if (tokens[i].Type == PathToken.TokenTypeWildCard)
                {
                    wildCardTokenIndex = i;
                    break;
                }
            }
            if (wildCardTokenIndex == -1)
            {
                if (segments.Count != tokens.Count)
                {
                    return (false, null);
                }
            }
            else
            {
                if (segments.Count < tokens.Count - 1)
                {
                    return (false, null);
                }
            }

            string wildCardMatch = null;
            for (int i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];
                string segment;
                if (wildCardTokenIndex == -1 || i <= wildCardTokenIndex)
                {
                    if (i == wildCardTokenIndex && segments.Count < tokens.Count)
                    {
                        // no wild card segment present.
                        segment = null;
                    }
                    else
                    {
                        segment = segments[i];
                    }
                }
                else
                {
                    // skip wild card segments.
                    segment = segments[i + segments.Count - tokens.Count];
                }
                if (token.Type == PathToken.TokenTypeLiteral)
                {
                    // accept empty segments
                    var comparisonType = parsedExample.CaseSensitiveMatchEnabled == true ?
                        StringComparison.Ordinal :
                        StringComparison.OrdinalIgnoreCase;
                    // partially unescape literals by default before comparison.
                    var unescaped = parsedExample.UnescapeNonWildCardSegments == false ?
                        segment : PathUtilsInternal.ReverseUnnecessaryUriEscapes(segment);
                    if (!token.Value.Equals(unescaped, comparisonType))
                    {
                        return (false, null);
                    }
                }
                else if (token.Type == PathToken.TokenTypeSegment)
                {
                    // reject empty segments by default.
                    if (segment.Length == 0 && !token.EmptySegmentAllowed)
                    {
                        return (false, null);
                    }
                    var valueKey = token.Value;
                    // fully unescape segments by default.
                    var unescaped = parsedExample.UnescapeNonWildCardSegments == false ?
                        segment : Uri.UnescapeDataString(segment);
                    pathValues.Add(valueKey, unescaped);
                }
                else if (token.Type == PathToken.TokenTypeWildCard)
                {
                    if (i != wildCardTokenIndex)
                    {
                        throw new ExpectationViolationException($"{i} != {wildCardTokenIndex}");
                    }

                    if (segment == null)
                    {
                        continue;
                    }

                    // construct wild card match.
                    wildCardMatch = string.Join("/", segments.Skip(wildCardTokenIndex)
                        .Take(segments.Count - tokens.Count + 1));
                    // ensure wild card match at beginning and/or ending of tokens
                    // correspond to prefix and/or suffix of path respectively.
                    if (wildCardTokenIndex == 0)
                    {
                        int index = path.IndexOf(wildCardMatch); // must not be -1
                        if (index == -1)
                        {
                            throw new ExpectationViolationException($"{index} == -1");
                        }
                        if (index != 0)
                        {
                            wildCardMatch = path.Substring(0, index) + wildCardMatch;
                        }
                    }
                    if (wildCardTokenIndex == tokens.Count - 1)
                    {
                        int index = path.LastIndexOf(wildCardMatch); // must not be -1
                        if (index == -1)
                        {
                            throw new ExpectationViolationException($"{index} == -1");
                        }
                        if (index + wildCardMatch.Length != path.Length)
                        {
                            wildCardMatch += path.Substring(index + wildCardMatch.Length);
                        }
                    }

                    // ensure starting slash for "middle" wild card matches
                    if (wildCardTokenIndex != 0 && wildCardTokenIndex != tokens.Count - 1)
                    {
                        wildCardMatch = '/' + wildCardMatch;
                    }
                }
                else
                {
                    throw new ExpectationViolationException("unexpected token type: " +
                        token.Type);
                }
            }

            // ensure wild card entry is made once it is part of tokens
            if (wildCardTokenIndex != -1)
            {
                var wildCardKey = tokens[wildCardTokenIndex].Value;
                // insert into path values without escaping, and even if it is null.
                pathValues.Add(wildCardKey, wildCardMatch);
            }

            return (true, wildCardMatch);
        }
    }
}