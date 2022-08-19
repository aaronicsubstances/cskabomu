using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Kabomu.Mediator.Path
{
    public class DefaultPathTemplateGenerator : IPathTemplateGenerator
    {
        private static readonly Regex SimpleTemplateSpecRegex = new Regex("(/{1,3})([^/]+)");

        public DefaultPathTemplateGenerator()
        {
            PathConstraints = new Dictionary<string, IPathConstraint>();
        }

        public IDictionary<string, IPathConstraint> PathConstraints { get; }

        public IPathTemplate Parse(string pathSpec)
        {
            // Interpret path spec as zero or more concatenations of /literal or //segment or ///wildcard.
            // where literal, segment or wildcard cannot be empty or contain slashes, will be trimmed of surrounding whitespace,
            // and a segment surrounded by whitespace will be interpreted to mean it allows for empty values.

            int startIndex = 0;
            int wildCardCharIndex = -1;
            var nonLiteralNames = new HashSet<string>();
            var tokens = new List<DefaultPathToken>();
            while (startIndex < pathSpec.Length)
            {
                var m = SimpleTemplateSpecRegex.Match(pathSpec, startIndex);
                if (!m.Success || m.Index != startIndex)
                {
                    throw new ArgumentException($"invalid spec seen at char pos {startIndex + 1}");
                }
                string tokenTypeIndicator = m.Groups[0].Value;
                int tokenType;
                switch (tokenTypeIndicator)
                {
                    case "/":
                        tokenType = DefaultPathToken.TokenTypeLiteral;
                        break;
                    case "//":
                        tokenType = DefaultPathToken.TokenTypeSegment;
                        break;
                    case "///":
                        tokenType = DefaultPathToken.TokenTypeWildCard;
                        break;
                    default:
                        throw new ExpectationViolationException("unexpected token type indicator: " +
                            tokenTypeIndicator);
                }

                string tokenValueIndicator = m.Groups[1].Value;
                string tokenValue = tokenValueIndicator.Trim();
                bool emptyValueAllowed = tokenValueIndicator != tokenValue;

                if (tokenType != DefaultPathToken.TokenTypeLiteral)
                {
                    if (nonLiteralNames.Contains(tokenValue))
                    {
                        throw new ArgumentException($"duplicate use of segment name at char pos {startIndex + 1}");
                    }
                    nonLiteralNames.Add(tokenValue);
                }
                if (tokenType == DefaultPathToken.TokenTypeWildCard)
                {
                    if (wildCardCharIndex != -1)
                    {
                        throw new ArgumentException($"duplicate specification of wild card segment at char pos {startIndex + 1}. " +
                            $"(wild card segment already specified at {wildCardCharIndex + 1})");
                    }
                    wildCardCharIndex = startIndex;
                }

                // add new token
                var token = new DefaultPathToken
                {
                    Type = tokenType,
                    Value = tokenValue,
                    EmptySegmentAllowed = emptyValueAllowed
                };
                tokens.Add(token);

                // advance loop
                startIndex += m.Length;
            }

            var pathTemplate = new DefaultPathTemplate();
            pathTemplate.ParsedSampleSets = new List<DefaultPathTemplateExample>();
            var parsedSample = new DefaultPathTemplateExample
            {
                ParsedSamples = tokens
            };
            pathTemplate.ParsedSampleSets.Add(parsedSample);

            // remove unnecessary escapes from all literal tokens
            RemoveUnnecessaryUriEscapes(pathTemplate.ParsedSampleSets);

            return pathTemplate;
        }

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
                    UnescapeNonWildCardSegments = sampleSet.UnescapeNonWildCardSegments,
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
            RemoveUnnecessaryUriEscapes(pathTemplate.ParsedSampleSets);

            return pathTemplate;
        }
        private static void RemoveUnnecessaryUriEscapes(IList<DefaultPathTemplateExample> sampleSets)
        {
            foreach (var sampleSet in sampleSets)
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

            // ensure uniqueness of segment names (both single and wild card).
            var nonLiteralNames = tokens.Where(x => x.Type != DefaultPathToken.TokenTypeLiteral).Select(x => x.Value);
            if (nonLiteralNames.Count() > nonLiteralNames.Distinct().Count())
            {
                throw new Exception("segment names in sample set are not unique: " +
                    string.Join(",", nonLiteralNames));
            }

            return tokens;
        }
    }
}
