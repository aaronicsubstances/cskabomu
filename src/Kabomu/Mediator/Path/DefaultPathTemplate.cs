using Kabomu.Common;
using Kabomu.Mediator.Handling;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Kabomu.Mediator.Path
{
    internal class DefaultPathTemplate : IPathTemplate
    {
        public DefaultPathTemplate()
        {
        }

        public bool CaseSensitiveMatchEnabled { get; set; }
        public bool? MatchTrailingSlash { get; set; }
        public IDictionary<string, string> DefaultValues { get; set; }
        public IList<IList<DefaultPathToken>> ParsedSampleSets { get; set; }
        public IDictionary<string, IList<IList<string>>> ParsedConstraintSpecs { get; set; }
        public IDictionary<string, IPathConstraint> PathConstraints { get; set; }

        public IPathMatchResult Match(IContext context, string requestTarget)
        {
            var path = DefaultPathTemplateGenerator.ExtractPath(requestTarget);

            if (MatchTrailingSlash.HasValue)
            {
                if (path.EndsWith("/") != MatchTrailingSlash.Value)
                {
                    return null;
                }
            }

            var segments = DefaultPathTemplateGenerator.SplitPath(path);

            var pathValues = new Dictionary<string, string>();
            IList<DefaultPathToken> matchingSampleSet = null;
            string wildCardMatch = null;
            foreach (var sampleSet in ParsedSampleSets)
            {
                var matchAttempt = TryMatch(path, segments, sampleSet, pathValues);
                if (matchAttempt.Item1)
                {
                    matchingSampleSet = sampleSet;
                    wildCardMatch = matchAttempt.Item2;
                    break;
                }
            }
            if (matchingSampleSet == null)
            {
                return null;
            }

            string boundPathPortion = path, unboundPathPortion = "";
            if (matchingSampleSet.Count > 0 &&
                matchingSampleSet[matchingSampleSet.Count - 1].Type == DefaultPathToken.TokenTypeWildCard &&
                wildCardMatch != null)
            {
                unboundPathPortion = wildCardMatch;
                if (!path.EndsWith(unboundPathPortion))
                {
                    throw new ExpectationViolationException("bug found in computing unbound path portion: " +
                        $"{path} does not end with {unboundPathPortion}");
                }
                boundPathPortion = path.Substring(0, path.Length - unboundPathPortion.Length);
            }

            // augment with default values.
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

            // run through path constraints.
            if (ParsedConstraintSpecs != null)
            {
                foreach (var e in ParsedConstraintSpecs)
                {
                    foreach (var row in e.Value)
                    {
                        if (!pathValues.ContainsKey(e.Key))
                        {
                            continue;
                        }
                        if (row.Count == 0)
                        {
                            continue;
                        }
                        var constraintFxn = PathConstraints[row[0]];
                        string[] args = row.Skip(1).ToArray();
                        bool ok = constraintFxn.Match(context, this, pathValues, e.Key,
                            args, 0);
                        if (!ok)
                        {
                            return null;
                        }
                    }
                }
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
            IList<DefaultPathToken> tokens, IDictionary<string, string> pathValues)
        {
            int wildCardTokenIndex = -1;
            for (int i = 0; i < tokens.Count; i++)
            {
                if (tokens[i].Type == DefaultPathToken.TokenTypeWildCard)
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
                if (token.Type == DefaultPathToken.TokenTypeLiteral)
                {
                    // accept empty segments
                    var comparisonType = CaseSensitiveMatchEnabled ? StringComparison.Ordinal :
                        StringComparison.OrdinalIgnoreCase;
                    if (!token.Value.Equals(segment, comparisonType))
                    {
                        return (false, null);
                    }
                }
                else if (token.Type == DefaultPathToken.TokenTypeSegment)
                {
                    // reject empty segments by default.
                    if (segment.Length == 0 && !token.EmptySegmentAllowed)
                    {
                        return (false, null);
                    }
                    var dataItem = Uri.UnescapeDataString(segment);
                    pathValues.Add(token.Value, dataItem);
                }
                else if (token.Type == DefaultPathToken.TokenTypeWildCard)
                {
                    if (i != wildCardTokenIndex)
                    {
                        throw new ExpectationViolationException($"multiple wild card tokens found: " +
                            $"{i} != {wildCardTokenIndex}");
                    }

                    if (segment == null)
                    {
                        continue;
                    }

                    // construct wild card match and don't escape it.
                    wildCardMatch = string.Join("/", segments.Skip(wildCardTokenIndex)
                        .Take(segments.Count - tokens.Count + 1));
                    // ensure wild card match at beginning and/or ending of tokens
                    // correspond to prefix and/or suffix of path respectively.
                    if (wildCardTokenIndex == 0)
                    {
                        int index = path.IndexOf(wildCardMatch); // must not be -1
                        if (index != 0)
                        {
                            wildCardMatch = path.Substring(0, index) + wildCardMatch;
                        }
                    }
                    if (wildCardTokenIndex == tokens.Count - 1)
                    {
                        int index = path.LastIndexOf(wildCardMatch); // must not be -1
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

                    // insert into path values if needed.
                    var wildCardKey = tokens[wildCardTokenIndex].Value;
                    if (wildCardKey.Length > 0)
                    {
                        pathValues.Add(wildCardKey, wildCardMatch);
                    }
                }
                else
                {
                    throw new ExpectationViolationException("unexpected token type: " +
                        token.Type);
                }
            }
            return (true, wildCardMatch);
        }
    }
}