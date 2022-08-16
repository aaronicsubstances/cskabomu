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
                CaseSensitiveMatchEnabled = pathTemplateSpec.CaseSensitiveMatchEnabled,
                MatchTrailingSlash = pathTemplateSpec.MatchTrailingSlash,
                DefaultValues = pathTemplateSpec.DefaultValues
            };

            // Parse examples for each set.
            pathTemplate.ParsedSampleSets = new List<IList<DefaultPathToken>>();
            foreach (var sampleSet in pathTemplateSpec.SampleSets)
            {
                var tokens = AnalyzeSampleSet(sampleSet);
                pathTemplate.ParsedSampleSets.Add(tokens);
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

        internal static IList<DefaultPathToken> AnalyzeSampleSet(IList<string> samples)
        {
            // sort by segment count (preserve original submission order for ties).
            var parsedSamples = new List<IList<string>>();
            foreach (var sample in samples)
            {
                var segments = SplitPath(sample);
                parsedSamples.Add(segments);
            }

            // NB: stable sort needed, hence cannot use List.Sort
            var sortedSamples = parsedSamples.Select((x, i) => (i, x)).OrderBy(x => x.Item2.Count).ToList();

            // if first time, mark all as literals.
            // else it is a subsequent one.

            // if same count as latest one, convert some literals before and after wild card
            // to single segments. 
            // Allow both new and existing ones to be empty.
            // else must have more;

            // if wildcard has already been determined, then just validate that
            // it matches expected literals in prefixes and suffixes.

            // else look for wildcard location, ie which split succeeds.

            var tokens = new List<DefaultPathToken>();
            int prevSegmentCount = -1;
            int wildCardTokenIndex = -1;
            foreach (var sortedSample in sortedSamples)
            {
                var (sampleIndex, sample) = sortedSample;
                if (prevSegmentCount == -1)
                {
                    // first time.
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
                                    if (token.Value != segment)
                                    {
                                        token.Type = DefaultPathToken.TokenTypeSegment;
                                    }
                                    break;
                                case DefaultPathToken.TokenTypeSegment:
                                    break;
                                case DefaultPathToken.TokenTypeWildCard:
                                    if (i != wildCardTokenIndex)
                                    {
                                        throw new ExpectationViolationException($"sample set analysis bug: " +
                                            $"{i} != {wildCardTokenIndex}");
                                    }
                                    break;
                                default:
                                    throw new ExpectationViolationException("unexpected token type: " +
                                        token.Type);
                            }
                            if (token.Type != DefaultPathToken.TokenTypeLiteral)
                            {
                                if (segment == "")
                                {
                                    token.EmptySegmentAllowed = true;
                                }
                                else
                                {
                                    token.UpdateValue(sampleIndex, segment);
                                }
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
                                if (token.Value != segment)
                                {
                                    throw new Exception($"at sample index {sampleIndex}: " +
                                        $"literal value does not match literal value " +
                                        $"at previous indices like index {token.SampleIndexOfValue} " +
                                        $"({segment} != {token.Value})");
                                }
                            }
                            else
                            {
                                if (segment == "")
                                {
                                    token.EmptySegmentAllowed = true;
                                }
                                else
                                {
                                    token.UpdateValue(sampleIndex, segment);
                                }
                            }
                        }
                    }
                    else
                    {
                        // locate new wild card position in tokens.
                        StringBuilder prefix = new StringBuilder(), suffix = new StringBuilder();
                        for (int i = 0; i < tokens.Count; i++)
                        {
                            suffix.Append(tokens[i].Value);
                        }
                        var originalSample = new StringBuilder(samples[sampleIndex]);
                        // remove surrounding slashes.
                        if (originalSample.Length > 0 && originalSample[0] == '/')
                        {
                            originalSample.Remove(0, 1);
                        }
                        if (originalSample.Length > 0 && originalSample[originalSample.Length - 1] == '/')
                        {
                            originalSample.Remove(originalSample.Length - 1, 1);
                        }
                        int candidatePos = 0;
                        for ( ; candidatePos <= tokens.Count; candidatePos++)
                        {
                            if (candidatePos > 0)
                            {
                                // add to end of prefix, and remove from beginning of suffix.
                                var tokenDiff = tokens[candidatePos - 1];
                                prefix.Append(tokenDiff.Value);
                                suffix.Remove(0, tokenDiff.Value.Length);
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
                            break;
                        }
                        if (candidatePos > tokens.Count)
                        {
                            throw new Exception($"at sample index {sampleIndex}: " +
                                $"sample does not match a wildcard expansion of shorter samples in set");
                        }
                        var wildcardToken = new DefaultPathToken();
                        wildcardToken.Type = DefaultPathToken.TokenTypeWildCard;
                        wildcardToken.Value = GetFirstNonEmptyValue(sample, candidatePos, sample.Count - tokens.Count);
                        tokens.Insert(candidatePos, wildcardToken);
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
            string v = sample[startPos];
            int endPos = startPos + count;
            while (v == "" && startPos < endPos)
            {
                v = sample[++startPos];
            }
            return v;
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

        internal static IList<string> SplitPath(string s)
        {
            s = RemoveSurroundingSlashes(s);
            var segments = new List<string>();
            var internalSlashIndices = FindPathSlashes(s);
            if (internalSlashIndices.Count == 0)
            {
                segments.Add(s);
            }
            else
            {
                int firstSlashIndex = internalSlashIndices[0];
                segments.Add(s.Substring(0, firstSlashIndex));
            }
            for (var i = 1; i < internalSlashIndices.Count; i++)
            {
                var startPos = internalSlashIndices[i - 1] + 1;
                var endSlashIndex = internalSlashIndices[i];
                segments.Add(s.Substring(startPos, endSlashIndex - startPos));
            }
            return segments;
        }

        private static string RemoveSurroundingSlashes(string s)
        {
            // be mindful of string with single character
            if (s.StartsWith("/"))
            {
                s = s.Substring(1);
            }
            if (s.EndsWith("/"))
            {
                s = s.Substring(0, s.Length - 1);
            }
            return s;
        }

        private static IList<int> FindPathSlashes(string s)
        {
            var slashIndices = new List<int>();
            for (int i = 0; i < s.Length; i++)
            {
                var slashPos = s.IndexOf('/', i);
                if (slashPos != -1)
                {
                    slashIndices.Add(slashPos);
                }
                else
                {
                    break;
                }
            }
            return slashIndices;
        }
    }
}
