using Kabomu.Common;
using Kabomu.Mediator.Handling;
using System;
using System.Collections.Generic;
using System.Linq;
using static Kabomu.Mediator.Path.DefaultPathTemplateExampleInternal;

namespace Kabomu.Mediator.Path
{
    internal class DefaultPathTemplateInternal : IPathTemplate
    {
        public DefaultPathTemplateInternal()
        {
        }

        public IList<DefaultPathTemplateExampleInternal> ParsedExamples { get; set; }
        public IDictionary<string, string> DefaultValues { get; set; }
        public IDictionary<string, IList<(string, string[])>> AllConstraints { get; set; }
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
                            var (ok, _) = PathUtilsInternal.ApplyValueConstraints(this, 
                                context, pathValues, valueKey, valueConstraints,
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
                pathValues, options, ParsedExamples, parsedExampleIndex, DefaultValues))
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
            if (requestTarget == null)
            {
                return null;
            }
            var pathAndAftermath = PathUtilsInternal.SplitRequestTarget(requestTarget);

            var path = pathAndAftermath[0];
            var segments = PathUtilsInternal.NormalizeAndSplitPath(path);

            IDictionary<string, string> pathValues = null;
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

                var matchAttempt = TryMatch(path, segments, parsedExample);
                if (matchAttempt == null)
                {
                    continue;
                }

                // apply constraint functions.
                bool constraintViolationDetected = false;
                if (AllConstraints != null)
                {
                    foreach (var e in AllConstraints)
                    {
                        if (!matchAttempt.PathValues.ContainsKey(e.Key))
                        {
                            continue;
                        }
                        var (ok, _) = PathUtilsInternal.ApplyValueConstraints(this, context, matchAttempt.PathValues,
                            e.Key, e.Value, ContextUtils.PathConstraintMatchDirectionMatch);
                        if (!ok)
                        {
                            constraintViolationDetected = true;
                            break;
                        }
                    }
                }

                if (constraintViolationDetected)
                {
                    continue;
                }

                matchingExample = parsedExample;
                pathValues = matchAttempt.PathValues;
                wildCardMatch = matchAttempt.WildCardMatch;
                break;
            }

            if (matchingExample == null)
            {
                return null;
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

            string boundPath = path, unboundRequestTarget = "";
            if (matchingExample.Tokens.Count > 0 &&
                matchingExample.Tokens[matchingExample.Tokens.Count - 1].Type == PathToken.TokenTypeWildCard &&
                wildCardMatch != null)
            {
                unboundRequestTarget = wildCardMatch;
                if (!path.EndsWith(unboundRequestTarget))
                {
                    throw new ExpectationViolationException($"{path} does not end with {unboundRequestTarget}");
                }
                boundPath = path.Substring(0, path.Length - unboundRequestTarget.Length);
            }
            unboundRequestTarget += pathAndAftermath[1];

            var result = new DefaultPathMatchResultInternal
            {
                PathValues = pathValues,
                BoundPath = boundPath,
                UnboundRequestTarget = unboundRequestTarget
            };
            return result;
        }

        private MatchAttemptResult TryMatch(string path, IList<string> segments,
            DefaultPathTemplateExampleInternal parsedExample)
        {
            var pathValues = new Dictionary<string, string>();
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
                    return null;
                }
            }
            else
            {
                if (segments.Count < tokens.Count - 1)
                {
                    return null;
                }
            }

            string wildCardMatch = null;
            for (int i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];
                string segment;
                if (wildCardTokenIndex == -1 || i < wildCardTokenIndex)
                {
                    segment = segments[i];
                }
                else if (i == wildCardTokenIndex)
                {
                    if (tokens.Count == 1)
                    {
                        // always ensure that whenever a template is just a wild card match,
                        // it matches the whole of path regardless of the segmentation of the path.
                        segment = path;
                    }
                    else if (segments.Count < tokens.Count)
                    {
                        // no wild card segment present.
                        segment = null;
                    }
                    else
                    {
                        segment = string.Join("/", segments.Skip(wildCardTokenIndex)
                            .Take(segments.Count - tokens.Count + 1));
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
                        return null;
                    }
                }
                else if (token.Type == PathToken.TokenTypeSegment)
                {
                    // reject empty segments by default.
                    if (segment.Length == 0 && !token.EmptySegmentAllowed)
                    {
                        return null;
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

                    // accept empty segments and
                    // construct wild card match.
                    wildCardMatch = segment;

                    // ensure wild card match at beginning and/or ending of tokens
                    // correspond to prefix and/or suffix of path respectively.
                    if (tokens.Count > 1)
                    {
                        if (wildCardTokenIndex == 0)
                        {
                            int index = path.IndexOf(wildCardMatch); // must not be -1
                            if (index == -1)
                            {
                                throw new ExpectationViolationException($"{index} == -1");
                            }
                            if (index != 0)
                            {
                                wildCardMatch = '/' + wildCardMatch;
                                //wildCardMatch = path.Substring(0, index) + wildCardMatch;
                            }
                        }
                        else
                        {
                            // ensure starting slash for all but prefix wild card matches
                            wildCardMatch = '/' + wildCardMatch;
                            if (wildCardTokenIndex == tokens.Count - 1)
                            {
                                int index = path.LastIndexOf(wildCardMatch); // must not be -1
                                if (index == -1)
                                {
                                    throw new ExpectationViolationException($"{index} == -1");
                                }
                                if (index + wildCardMatch.Length != path.Length)
                                {
                                    wildCardMatch = wildCardMatch + '/';
                                    //wildCardMatch += path.Substring(index + wildCardMatch.Length);
                                }
                            }
                        }
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

            var result = new MatchAttemptResult
            {
                PathValues = pathValues,
                WildCardMatch = wildCardMatch
            };
            return result;
        }

        class MatchAttemptResult
        {
            public string WildCardMatch { get; set; }
            public IDictionary<string, string> PathValues { get; set; }
        }
    }
}