using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kabomu.Mediator.Path
{
    public class DefaultPathTemplateGenerator
    {
        public DefaultPathTemplateGenerator()
        {
            PathConstraints = new Dictionary<string, IPathConstraint>();
        }

        public IDictionary<string, IPathConstraint> PathConstraints { get; }

        public IPathTemplate Generate(DefaultPathTemplateSpecification pathTemplateSpec)
        {
            var pathTemplate = new DefaultPathTemplate
            {
                DefaultValues = pathTemplateSpec.DefaultValues
            };

            // Parse examples for each set.
            pathTemplate.ParsedSampleSets = new List<DefaultPathTemplateExample>();
            int setNum = 0;
            foreach (var sampleSet in pathTemplateSpec.SampleSets)
            {
                setNum++;
                var tokens = TokenizeSampleSet(setNum, sampleSet);
                var parsedSample = new DefaultPathTemplateExample
                {
                    CaseSensitiveMatchEnabled = sampleSet.CaseSensitiveMatchEnabled,
                    MatchLeadingSlash = sampleSet.MatchLeadingSlash,
                    MatchTrailingSlash = sampleSet.MatchTrailingSlash,
                    ParsedSamples = tokens
                };
                pathTemplate.ParsedSampleSets.Add(parsedSample);
            }

            // Validate that each constraint spec is valid CSV and that
            // all given constraint ids are present.
            // Save parsed CSV results.
            pathTemplate.ParsedConstraintSpecs = new Dictionary<string, IList<IList<string>>>();
            HashSet<string> constraintIds = new HashSet<string>();
            if (pathTemplateSpec.ConstraintSpecs != null)
            {
                foreach (var e in pathTemplateSpec.ConstraintSpecs)
                {
                    var parsed = CsvUtils.Deserialize(e.Value);
                    foreach (var row in parsed)
                    {
                        if (row.Count == 0)
                        {
                            continue;
                        }
                        var constraintId = row[0];
                        if (!PathConstraints.ContainsKey(constraintId))
                        {
                            throw new Exception($"constraint {constraintId} not found");
                        }
                        constraintIds.Add(constraintId);
                    }
                    pathTemplate.ParsedConstraintSpecs.Add(e.Key, parsed);
                }
            }

            // Copy over path constraints to make them available even after 
            // an update to this generator.
            pathTemplate.PathConstraints = new Dictionary<string, IPathConstraint>();
            foreach (var constraintId in constraintIds)
            {
                pathTemplate.PathConstraints.Add(constraintId, PathConstraints[constraintId]);
            }

            // remove unnecessary escapes from all literal tokens
            foreach (var sampleSet in pathTemplate.ParsedSampleSets)
            {
                if (sampleSet.UnescapeNonWildCardSegments == false)
                {
                    continue;
                }
                foreach (var token in sampleSet.ParsedSamples)
                {
                    if (token.Type == DefaultPathToken.TokenTypeLiteral)
                    {
                        token.Value = PathUtilsInternal.ReverseUnnecessaryUriEscapes(token.Value);
                    }
                }
            }

            return pathTemplate;
        }

        internal static IList<DefaultPathToken> TokenizeSampleSet(int setNum, DefaultPathTemplateExample sampleSet)
        {
            // sort by segment count. preserve original submission order for ties.
            var parsedSamples = new List<IList<string>>();
            foreach (var sample in sampleSet.Samples)
            {
                var segments = PathUtilsInternal.NormalizeAndSplitPath(sample);
                parsedSamples.Add(segments);
            }

            // NB: stable sort needed, hence cannot use List.Sort
            var sortedSamples = parsedSamples.Select((x, i) => (i, x))
                .OrderBy(x => x.Item2.Count)
                .ToList();

            // if first time, mark all as literals.
            // else it is a subsequent one.

            // if same count as latest one, convert some literals before and after wild card
            // to single segments. 
            // Allow both new and existing ones to be empty.
            // else must have more;

            // if wild card has already been determined, then just validate that
            // it matches expected literals in prefixes and suffixes.

            // else look for wild card location, ie which split succeeds.

            var tokens = new List<DefaultPathToken>();
            var ignoreCase = sampleSet.CaseSensitiveMatchEnabled != true;
            var comparisonType = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            int prevSegmentCount = -1;
            int wildCardTokenIndex = -1;
            bool firstTime = true;
            foreach (var sortedSample in sortedSamples)
            {
                var (sampleIndex, sample) = sortedSample;
                if (firstTime)
                {
                    firstTime = false;
                    foreach (var segment in sample)
                    {
                        var token = new DefaultPathToken();
                        token.Type = DefaultPathToken.TokenTypeLiteral;
                        token.SampleIndexOfValue = sampleIndex;
                        token.Value = segment;
                        tokens.Add(token);
                    }
                }
                else
                {
                    if (sample.Count == prevSegmentCount)
                    {
                        for (int i = 0; i < tokens.Count; i++)
                        {
                            var token = tokens[i];
                            string segment;
                            if (wildCardTokenIndex == -1 || i < wildCardTokenIndex)
                            {
                                segment = sample[i];
                            }
                            else if (i == wildCardTokenIndex)
                            {
                                segment = PathUtilsInternal.GetFirstNonEmptyValue(
                                    sample, i, sample.Count - tokens.Count);
                            }
                            else
                            {
                                // skip extra wild card segments.
                                segment = sample[i + sample.Count - tokens.Count];
                            }
                            switch (token.Type)
                            {
                                case DefaultPathToken.TokenTypeLiteral:
                                    if (!token.Value.Equals(segment, comparisonType))
                                    {
                                        // replace with segment token which inherits all other attributes.
                                        token.Type = DefaultPathToken.TokenTypeSegment;
                                    }
                                    break;
                                case DefaultPathToken.TokenTypeSegment:
                                    break;
                                case DefaultPathToken.TokenTypeWildCard:
                                    if (i != wildCardTokenIndex)
                                    {
                                        throw new ExpectationViolationException($"{i} != {wildCardTokenIndex}");
                                    }
                                    break;
                                default:
                                    throw new ExpectationViolationException("unexpected token type: " +
                                        token.Type);
                            }
                            PathUtilsInternal.UpdateNonLiteralToken(token, sampleIndex, segment);
                        }
                    }
                    else if (wildCardTokenIndex != -1)
                    {
                        for (int i = 0; i < tokens.Count; i++)
                        {
                            var token = tokens[i];
                            string segment;
                            if (i < wildCardTokenIndex)
                            {
                                segment = sample[i];
                            }
                            else if (i == wildCardTokenIndex)
                            {
                                segment = PathUtilsInternal.GetFirstNonEmptyValue(
                                    sample, i, sample.Count - tokens.Count);
                            }
                            else
                            {
                                // skip extra wild card segments.
                                segment = sample[i + sample.Count - tokens.Count];
                            }
                            if (token.Type == DefaultPathToken.TokenTypeLiteral)
                            {
                                if (!token.Value.Equals(segment, comparisonType))
                                {
                                    throw new Exception($"at sample index {sampleIndex}: " +
                                        $"literal value does not match literal value " +
                                        $"at previous index {token.SampleIndexOfValue} " +
                                        $"({segment} != {token.Value})");
                                }
                            }
                            PathUtilsInternal.UpdateNonLiteralToken(token, sampleIndex, segment);
                        }
                    }
                    else
                    {
                        // locate new wild card position in tokens.
                        wildCardTokenIndex = PathUtilsInternal.LocateWildCardTokenPosition(
                            sampleSet.Samples[sampleIndex], ignoreCase, tokens);
                        if (wildCardTokenIndex == -1)
                        {
                            throw new Exception($"at sample index {sampleIndex}: " +
                                $"sample does not match a wildcard expansion of shorter samples in set");
                        }
                        var wildCardToken = new DefaultPathToken();
                        wildCardToken.Type = DefaultPathToken.TokenTypeWildCard;
                        string tokenValue = PathUtilsInternal.GetFirstNonEmptyValue(
                            sample, wildCardTokenIndex, sample.Count - tokens.Count);
                        tokens.Insert(wildCardTokenIndex, wildCardToken);
                        PathUtilsInternal.UpdateNonLiteralToken(wildCardToken, sampleIndex, tokenValue);
                    }

                    // update segment count for next iteration.
                    prevSegmentCount = sample.Count;
                }
            }

            // ensure uniqueness of tokens
            var tokenNames = tokens.Select(x => x.Value);
            if (tokenNames.Count() > tokenNames.Distinct().Count())
            {
                throw new Exception("token names in set are not unique: " +
                    string.Join(",", tokenNames));
            }

            return tokens;
        }
    }
}
