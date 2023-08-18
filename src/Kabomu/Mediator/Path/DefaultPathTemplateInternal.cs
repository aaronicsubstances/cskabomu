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

        public string Interpolate(IContext context, IDictionary<string, object> pathValues,
            object opaqueOptionObj)
        {
            var candidates = InterpolateAll(context, pathValues, opaqueOptionObj);

            // pick shortest string. break ties by ordering of parsed examples.
            // stable sort needed so can't use List.sort
            var shortest = candidates.OrderBy(x => x.Length).FirstOrDefault();
            if (shortest == null)
            {
                throw new PathTemplateException("could not interpolate template with the provided arguments");
            }
            return shortest;
        }

        public IList<string> InterpolateAll(IContext context, IDictionary<string, object> pathValues,
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

        private string TryInterpolate(int parsedExampleIndex, IContext context, IDictionary<string, object> pathValues,
            DefaultPathTemplateFormatOptions options)
        {
            var parsedExample = ParsedExamples[parsedExampleIndex];
            var escapeNonWildCardSegment = PathUtilsInternal.GetEffectiveEscapeNonWildCardSegment(options, parsedExample);

            var segments = new List<string>();
            var tokens = parsedExample.Tokens;
            var wildCardTokenIndex = -1;
            for (int i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];
                if (token.Type == PathToken.TokenTypeLiteral)
                {
                    segments.Add(token.Value);
                }
                else
                {
                    var valueKey = token.Value;
                    if (pathValues == null || !pathValues.ContainsKey(valueKey))
                    {
                        // no path value provided, meaning parsed example is not meant to be used.
                        return null;
                    }
                    var pathValue = pathValues[valueKey]?.ToString();
                    if (token.Type == PathToken.TokenTypeSegment)
                    {
                        if (!token.EmptySegmentAllowed && (pathValue == "" || pathValue == null))
                        {
                            return null;
                        }
                        if (pathValue == null)
                        {
                            pathValue = "";
                        }
                        if (escapeNonWildCardSegment)
                        {
                            pathValue = PathUtilsInternal.EncodeAlmostEveryUriChar(pathValue);
                        }
                        segments.Add(pathValue);
                    }
                    else if (token.Type == PathToken.TokenTypeWildCard)
                    {
                        if (wildCardTokenIndex != -1)
                        {
                            throw new ExpectationViolationException($"{wildCardTokenIndex} != -1");
                        }
                        wildCardTokenIndex = i;
                        if (pathValue == null)
                        {
                            // no wild card segment needed.
                            continue;
                        }
                        // remove slashes as necessary to result in preservation of
                        // segments during joining.
                        if (tokens.Count > 1)
                        {
                            if (wildCardTokenIndex > 0)
                            {
                                if (pathValue.StartsWith("/"))
                                {
                                    pathValue = pathValue.Substring(1);
                                }
                            }
                            if (wildCardTokenIndex < tokens.Count - 1)
                            {
                                if (pathValue.EndsWith("/"))
                                {
                                    pathValue = pathValue.Substring(0, pathValue.Length - 1);
                                }
                            }
                        }
                        segments.Add(pathValue);
                    }
                    else
                    {
                        throw new ExpectationViolationException("unexpected token type: " +
                            token.Type);
                    }
                }
            }

            // check that other unused non-literal tokens are satisfied from default values
            // if specified in path values.
            if (ParsedExamples.Count > 1)
            {
                if (!PathUtilsInternal.AreAllRelevantPathValuesSatisfiedFromDefaultValues(
                    pathValues, options, ParsedExamples, parsedExampleIndex, DefaultValues))
                {
                    return null;
                }
            }

            // if our segments are ready then proceed to join them with intervening slashes
            // and carefully surround with sentinel slashes.
            var wildCardValuePresent = wildCardTokenIndex != -1 &&
                pathValues[tokens[wildCardTokenIndex].Value] != null;
            bool applyLeadingSlash = PathUtilsInternal.GetEffectiveApplyLeadingSlash(options,
                wildCardValuePresent && wildCardTokenIndex == 0, parsedExample);
            bool applyTrailingSlash = PathUtilsInternal.GetEffectiveApplyTrailingSlash(options,
                wildCardValuePresent && wildCardTokenIndex == tokens.Count - 1, parsedExample);
            string path;
            if (segments.Count == 0)
            {
                // NB: this if-branch applies to 2 cases where
                // either tokens is empty or single wildcard token has null value.
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

            IDictionary<string, object> pathValues = null;
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

                // add default values.
                var candidatePathValues = matchAttempt.PathValues;
                if (DefaultValues != null)
                {
                    foreach (var e in DefaultValues)
                    {
                        if (!candidatePathValues.ContainsKey(e.Key))
                        {
                            candidatePathValues.Add(e.Key, e.Value);
                        }
                    }
                }

                // apply constraint functions.
                bool constraintViolationDetected = false;
                if (AllConstraints != null)
                {
                    foreach (var e in AllConstraints)
                    {
                        if (!candidatePathValues.ContainsKey(e.Key))
                        {
                            continue;
                        }
                        var (ok, _) = PathUtilsInternal.ApplyValueConstraints(this, context, candidatePathValues,
                            e.Key, e.Value);
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
                pathValues = candidatePathValues;
                wildCardMatch = matchAttempt.WildCardMatch;
                break;
            }

            if (matchingExample == null)
            {
                return null;
            }

            string boundPath = path, unboundRequestTarget = pathAndAftermath[1];
            if (matchingExample.Tokens.Count > 0 &&
                matchingExample.Tokens[matchingExample.Tokens.Count - 1].Type == PathToken.TokenTypeWildCard &&
                wildCardMatch != null)
            {
                boundPath = path.Substring(0, path.Length - wildCardMatch.Length);
                unboundRequestTarget = wildCardMatch + unboundRequestTarget;
            }

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
            // use mutable dictionary to make it possible for
            // the addition of default values, and mutation by
            // constraint functions.
            var pathValues = new Dictionary<string, object>();
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

                    // ensure wild card match prefix and suffix
                    // correspond to prefix and suffix of path respectively.
                    if (tokens.Count > 1)
                    {
                        string prefix = path.StartsWith("/") ? "/" : "";
                        string suffix = path.EndsWith("/") ? "/" : "";
                        wildCardMatch = prefix + wildCardMatch + suffix;
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
            public IDictionary<string, object> PathValues { get; set; }
        }
    }
}