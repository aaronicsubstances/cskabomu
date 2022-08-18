using Kabomu.Mediator.Handling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kabomu.Mediator.Path
{
    internal static class PathUtilsInternal
    {
        public static void UpdateNonLiteralToken(DefaultPathToken token, int sampleIndex, string value)
        {
            if (token.Type == DefaultPathToken.TokenTypeLiteral)
            {
                throw new ArgumentException("received literal token");
            }
            if (value == "")
            {
                token.EmptySegmentAllowed = true;
            }
            bool proceedWithUpdate = false;
            if (token.SampleIndexOfValue == -1)
            {
                // first time.
                proceedWithUpdate = true;
            }
            else if (token.Value == "" && value != "")
            {
                // always allow non empty values to override empty ones
                // even if non empty value appears after empty one in
                // sample.
                proceedWithUpdate = true;
            }
            else if (sampleIndex < token.SampleIndexOfValue)
            {
                // only update if incoming sample index precedes the existing sample
                // in original submission.
                proceedWithUpdate = true;
            }

            if (proceedWithUpdate)
            {
                token.SampleIndexOfValue = sampleIndex;
                token.Value = value;
            }
        }

        public static int LocateWildCardTokenPosition(string originalSample, bool ignoreCase,
            IList<DefaultPathToken> tokens)
        {
            if (tokens.Count == 0)
            {
                // this means that previous sample was one of empty string or single slash,
                // and both of these formats imply entire sample set is a wild card once a 
                // this original sample is encountered.
                return 0;
            }
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
            var mutableOriginalSample = new StringBuilder();
            if (ignoreCase)
            {
                mutableOriginalSample.Append(originalSample.ToLowerInvariant());
            }
            else
            {
                mutableOriginalSample.Append(originalSample);
            }
            // remove surrounding slashes.
            if (mutableOriginalSample.Length > 0 && mutableOriginalSample[0] == '/')
            {
                mutableOriginalSample.Remove(0, 1);
            }
            if (mutableOriginalSample.Length > 0 && mutableOriginalSample[mutableOriginalSample.Length - 1] == '/')
            {
                mutableOriginalSample.Remove(mutableOriginalSample.Length - 1, 1);
            }
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
                if (!MutableStringStartsWith(mutableOriginalSample, prefix))
                {
                    continue;
                }
                if (!MutableStringEndsWith(mutableOriginalSample, suffix))
                {
                    continue;
                }

                // found desired position, so stop search.
                return i;
            }

            return -1;
        }

        public static string ExtractPath(string requestTarget)
        {
            string path = new Uri(requestTarget ?? "").AbsolutePath;
            return path;
        }

        public static IList<string> NormalizeAndSplitPath(string path)
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

        public static string GetFirstNonEmptyValue(IList<string> sample, int startPos, int count)
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

        public static bool MutableStringStartsWith(StringBuilder originalSample, StringBuilder prefix)
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

        public static bool MutableStringEndsWith(StringBuilder originalSample, StringBuilder suffix)
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

        public static bool ApplyConstraint(DefaultPathTemplate pathTemplate,
            IContext context, IDictionary<string, string> pathValues,
            string valueKey, IList<IList<string>> constraints, int direction)
        {
            foreach (var row in constraints)
            {
                if (row.Count == 0)
                {
                    continue;
                }
                var constraintFxn = pathTemplate.PathConstraints[row[0]];
                string[] args = row.Skip(1).ToArray();
                bool ok = constraintFxn.Match(context, pathTemplate, pathValues, valueKey,
                    args, direction);
                if (!ok)
                {
                    return false;
                }
            }
            return true;
        }

        public static bool GetEffectiveEscapeNonWildCardSegment(IPathTemplateFormatOptions options,
            DefaultPathTemplateExample sampleSet)
        {
            if (options != null && options.EscapeNonWildCardSegments.HasValue)
            {
                return options.EscapeNonWildCardSegments.Value;
            }
            if (sampleSet.UnescapeNonWildCardSegments != null)
            {
                return sampleSet.UnescapeNonWildCardSegments.Value;
            }
            return true;
        }

        public static bool GetEffectiveApplyLeadingSlash(IPathTemplateFormatOptions options, DefaultPathTemplateExample sampleSet)
        {
            if (options?.ApplyLeadingSlash != null)
            {
                return options.ApplyLeadingSlash.Value;
            }
            if (sampleSet.MatchLeadingSlash != null)
            {
                return sampleSet.MatchLeadingSlash.Value;
            }
            // apply leading slashes by default.
            return true;
        }

        public static bool GetEffectiveApplyTrailingSlash(IPathTemplateFormatOptions options, DefaultPathTemplateExample sampleSet)
        {
            if (options?.ApplyTrailingSlash != null)
            {
                return options.ApplyTrailingSlash.Value;
            }
            if (sampleSet.MatchTrailingSlash != null)
            {
                return sampleSet.MatchTrailingSlash.Value;
            }
            // omit trailing slash by default.
            return false;
        }

        public static string ReverseUnnecessaryUriEscapes(string segment)
        {
            var transformed = new StringBuilder(segment);
            int i = 0;
            while (i < transformed.Length)
            {
                var ch = transformed[i];
                if (ch == '%')
                {
                    int possibleReplacement = FastConvertPercentEncodedToPositiveNum(transformed, i);
                    if (possibleReplacement > 0)
                    {
                        transformed.Remove(i, 3);
                        transformed.Insert(i, (char)possibleReplacement);
                    }
                }
                i++;
            }
            return transformed.ToString();
        }

        public static int FastConvertPercentEncodedToPositiveNum(StringBuilder s, int pos)
        {
            if (pos < 0 || pos >= s.Length)
            {
                return 0;
            }
            if (pos + 1 >= s.Length)
            {
                return 0;
            }
            char hi = s[pos];
            char lo = s[pos + 1];
            int ans = 0;
            int lowerA = 'a', lowerF = 'f', lowerZ = (int)'z';
            int upperA = 'A', upperF = 'F', upperZ = (int)'Z';
            int zeroCh = '0', nineCh = '9';
            for (int i = 0; i < 2; i++)
            {
                var chToUse = i == 0 ? hi : lo;
                int inc = 0;
                bool validHexDigitFound = false;
                if (chToUse >= lowerA && chToUse <= lowerF)
                {
                    validHexDigitFound = true;
                    inc = (chToUse - lowerA) + 10;
                }
                else if (chToUse >= upperA && chToUse <= upperF)
                {
                    validHexDigitFound = true;
                    inc = (chToUse - upperA) + 10;
                }
                else if (chToUse >= zeroCh && chToUse <= nineCh)
                {
                    validHexDigitFound = true;
                    inc = chToUse - zeroCh;
                }

                if (!validHexDigitFound)
                {
                    return 0;
                }

                ans += (ans * 16) + inc;
            }

            // The unnecessary escapes are:
            // a-zA-Z0-9 - . _ ~ ! $ & ' ( ) * + , ; = : @

            var unnecesaryEscapeFound = false;
            if (ans >= lowerA && ans <= lowerZ)
            {
                unnecesaryEscapeFound = true;
            }
            else if (ans >= upperA && ans <= upperZ)
            {
                unnecesaryEscapeFound = true;
            }
            else if (ans >= zeroCh && ans <= nineCh)
            {
                unnecesaryEscapeFound = true;
            }
            else
            {
                switch (ans)
                {
                    case '-':
                    case '.':
                    case '_':
                    case '~':
                    case '!':
                    case '$':
                    case '&':
                    case '\'':
                    case '(':
                    case ')':
                    case '*':
                    case '+':
                    case ',':
                    case ';':
                    case '=':
                    case ':':
                    case '@':
                        unnecesaryEscapeFound = true;
                        break;
                    default:
                        break;
                }
            }
            if (!unnecesaryEscapeFound)
            {
                return 0;
            }

            return ans;
        }
    }
}
