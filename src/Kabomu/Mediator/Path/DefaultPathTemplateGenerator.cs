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
                var tokens = AnalyzeSampleSet(setNum, sampleSet);
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

            return pathTemplate;
        }

        internal static IList<DefaultPathToken> AnalyzeSampleSet(int setNum, DefaultPathTemplateExample sampleSet)
        {
            // sort by segment count. preserve original submission order for ties.
            var parsedSamples = new List<IList<string>>();
            foreach (var sample in sampleSet.Samples)
            {
                var segments = NormalizeAndSplitPath(sample);
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
            var ignoreCase = !sampleSet.CaseSensitiveMatchEnabled;
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
                        var token = new DefaultPathToken(null, DefaultPathToken.TokenTypeLiteral);
                        token.Update(sampleIndex, segment);
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
                                segment = GetFirstNonEmptyValue(sample, i, sample.Count - tokens.Count);
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
                                        tokens[i] = new DefaultPathToken(token, DefaultPathToken.TokenTypeSegment);
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
                            if (token.Type != DefaultPathToken.TokenTypeLiteral)
                            {
                                token.Update(sampleIndex, segment);
                            }
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
                                segment = GetFirstNonEmptyValue(sample, i, sample.Count - tokens.Count);
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
                            else
                            {
                                token.Update(sampleIndex, segment);
                            }
                        }
                    }
                    else
                    {
                        // locate new wild card position in tokens.
                        StringBuilder prefix = new StringBuilder(), suffix = new StringBuilder();
                        for (int i = 0; i < tokens.Count; i++)
                        {
                            if (ignoreCase)
                            {
                                suffix.Append(tokens[i].Value.ToLowerInvariant());
                            }
                            else
                            {
                                suffix.Append(tokens[i].Value);
                            }
                        }
                        var originalSample = new StringBuilder();
                        if (ignoreCase)
                        {
                            originalSample.Append(sampleSet.Samples[sampleIndex].ToLowerInvariant());
                        }
                        else
                        {
                            originalSample.Append(sampleSet.Samples[sampleIndex]);
                        }
                        // remove surrounding slashes.
                        if (originalSample.Length > 0 && originalSample[0] == '/')
                        {
                            originalSample.Remove(0, 1);
                        }
                        if (originalSample.Length > 0 && originalSample[originalSample.Length - 1] == '/')
                        {
                            originalSample.Remove(originalSample.Length - 1, 1);
                        }
                        int wildCardTokenPos = -1;
                        for (int i = 0; i <= tokens.Count; i++)
                        {
                            if (i > 0)
                            {
                                // add to end of prefix, and remove from beginning of suffix.
                                var tokenDiff = tokens[i - 1].Value;
                                if (ignoreCase)
                                {
                                    tokenDiff = tokenDiff.ToLowerInvariant();
                                }
                                prefix.Append(tokenDiff);
                                suffix.Remove(0, tokenDiff.Length);
                            }
                            if (!MutableStringStartsWith(originalSample, prefix))
                            {
                                continue;
                            }
                            if (!MutableStringEndsWith(originalSample, suffix))
                            {
                                continue;
                            }

                            // found desired position, so stop search.
                            wildCardTokenPos = i;
                            break;
                        }
                        if (wildCardTokenPos == -1)
                        {
                            throw new Exception($"at sample index {sampleIndex}: " +
                                $"sample does not match a wildcard expansion of shorter samples in set");
                        }
                        var wildCardToken = new DefaultPathToken(null, DefaultPathToken.TokenTypeWildCard);
                        string tokenValue = GetFirstNonEmptyValue(sample, wildCardTokenPos, sample.Count - tokens.Count);
                        wildCardToken.Update(sampleIndex, tokenValue);
                        tokens.Insert(wildCardTokenPos, wildCardToken);
                    }

                    // update segment count for next iteration.
                    prevSegmentCount = sample.Count;
                }
            }

            // ensure uniqueness of tokens
            var tokenNames = tokens.Select(x => x.Value).Where(x => !string.IsNullOrEmpty(x));
            if (tokenNames.Count() > tokenNames.Distinct().Count())
            {
                throw new Exception("token names in set are not unique: " +
                    string.Join(",", tokenNames));
            }

            return tokens;
        }

        internal static string GetFirstNonEmptyValue(IList<string> sample, int startPos, int count)
        {
            for (int i = startPos; i < startPos + count; i++)
            {
                var v = sample[i];
                if (v != "")
                {
                    return v;
                }
            }
            return "";
        }

        internal static bool MutableStringStartsWith(StringBuilder originalSample, StringBuilder prefix)
        {
            if (originalSample.Length < prefix.Length)
            {
                return false;
            }
            for (int i = 0; i < prefix.Length; i++)
            {
                if (originalSample[i] != prefix[i])
                {
                    return false;
                }
            }
            return true;
        }

        internal static bool MutableStringEndsWith(StringBuilder originalSample, StringBuilder suffix)
        {
            if (originalSample.Length < suffix.Length)
            {
                return false;
            }
            for (int i = 0; i < suffix.Length; i++)
            {
                if (originalSample[originalSample.Length - suffix.Length + i] != suffix[i])
                {
                    return false;
                }
            }
            return true;
        }

        internal static string ExtractPath(string requestTarget)
        {
            string path = new Uri(requestTarget ?? "").AbsolutePath;
            return path;
        }

        internal static IList<string> NormalizeAndSplitPath(string path)
        {
            // Generate split of normalized version of path, such that joining segements together
            // with slash will result in normalized path which neither begin nor end with slashes.
            // In BNF-like format, normalized paths have the form
            //     e | no-slash [ '/' no-slash ]*
            // where e is the empty set, and no-slash is any string lacking slashes (including empty strings).

            // NB: '/' is normalized to be the same as empty string, ie as empty set.

            var segments = new List<string>();

            int startPos = 0, endPos = path.Length;

            // remove surrounding slashes, and be mindful of empty string, single and double slash cases.
            if (path.StartsWith("/"))
            {
                startPos++;
            }
            if (path.EndsWith("/"))
            {
                endPos--;
            }

            if (endPos >= startPos && path != "")
            {
                while (true)
                {
                    var slashIndex = path.IndexOf('/', startPos, endPos - startPos);
                    if (slashIndex == -1)
                    {
                        break;
                    }
                    segments.Add(path.Substring(startPos, slashIndex - startPos));

                    // advance loop by setting start position to last slash index increased by length of slash
                    startPos = slashIndex + 1;
                }

                // add remaining segment even if it is empty
                segments.Add(path.Substring(startPos, endPos - startPos));
            }

            return segments;
        }
    }
}
