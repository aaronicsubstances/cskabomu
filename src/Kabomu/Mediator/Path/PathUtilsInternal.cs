using Kabomu.Mediator.Handling;
using System;
using System.Collections.Generic;
using System.Text;
using static Kabomu.Mediator.Path.DefaultPathTemplateExampleInternal;

namespace Kabomu.Mediator.Path
{
    internal static class PathUtilsInternal
    {
        public static string ExtractPath(string requestTarget)
        {
            string path = new Uri(requestTarget ?? "").AbsolutePath;
            return path;
        }

        public static string ConvertPossibleNullToString(object obj)
        {
            return $"{obj}";
        }

        public static IList<string> SplitTemplateSpecIntoLines(string s)
        {
            string[] lines = s.Split(
               new string[] { "\r\n", "\r", "\n" },
               StringSplitOptions.None
            );
            return lines;
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

        public static bool ApplyValueConstraints(DefaultPathTemplateInternal pathTemplate,
            IContext context, IDictionary<string, string> pathValues,
            string valueKey, IList<(string, string[])> constraints, int direction)
        {
            foreach (var constraint in constraints)
            {
                var (constraintFunctionId, constraintFunctionArgs) = constraint;
                var constraintFxn = pathTemplate.ConstraintFunctions[constraintFunctionId];
                bool ok = constraintFxn.Match(context, pathTemplate, pathValues, valueKey,
                    constraintFunctionArgs, direction);
                if (!ok)
                {
                    return false;
                }
            }
            return true;
        }

        public static bool AreAllRelevantPathValuesSatisfiedFromDefaultValues(
            IDictionary<string, string> pathValues, bool? caseSensitiveMatchEnabledOverride,
            IList<DefaultPathTemplateExampleInternal> parsedExamples, int alreadySatisfiedIndex,
            IDictionary<string, string> defaultValues)
        {
            // begin recording all value keys already satisfied.
            var satisfiedValueKeys = new HashSet<string>();
            foreach (var token in parsedExamples[alreadySatisfiedIndex].Tokens)
            {
                if (token.Type != PathToken.TokenTypeLiteral)
                {
                    satisfiedValueKeys.Add(token.Value);
                }
            }
            for (var i = 0; i < parsedExamples.Count; i++)
            {
                if (i == alreadySatisfiedIndex)
                {
                    continue;
                }
                var otherParsedExample = parsedExamples[i];
                var otherTokens = otherParsedExample.Tokens;
                foreach (var otherToken in otherTokens)
                {
                    var valueKey = otherToken.Value;
                    if (!pathValues.ContainsKey(valueKey))
                    {
                        // not relevant.
                        continue;
                    }

                    // relevant path key found.
                    if (satisfiedValueKeys.Contains(valueKey))
                    {
                        // dealt wih already.
                        continue;
                    }

                    // Relevant path key not already dealt with.
                    // So it's all up to default values, not only
                    // for the path key to be present, but also
                    // for its value to match
                    if (defaultValues == null || !defaultValues.ContainsKey(valueKey))
                    {
                        return false;
                    }

                    var pathValue = pathValues[valueKey];
                    var defaultValue = defaultValues[valueKey];

                    bool ignoreCase;
                    if (caseSensitiveMatchEnabledOverride.HasValue)
                    {
                        ignoreCase = !caseSensitiveMatchEnabledOverride.Value;
                    }
                    else
                    {
                        ignoreCase = otherParsedExample.CaseSensitiveMatchEnabled != true;
                    }
                    if (!AreTwoPossiblyNullStringsEqual(pathValue, defaultValue, ignoreCase))
                    {
                        return false;
                    }

                    // mark as satisfied.
                    satisfiedValueKeys.Add(valueKey);
                }
            }

            return true;
        }

        internal static bool AreTwoPossiblyNullStringsEqual(string first, string second, bool ignoreCase)
        {
            if (first == null)
            {
                return second == null;
            }
            var comparisonType = ignoreCase ?
                StringComparison.OrdinalIgnoreCase :
                StringComparison.Ordinal;
            return first.Equals(second, comparisonType);
        }

        public static bool GetEffectiveEscapeNonWildCardSegment(DefaultPathTemplateFormatOptions options,
            DefaultPathTemplateExampleInternal parsedExample)
        {
            if (options != null && options.EscapeNonWildCardSegments.HasValue)
            {
                return options.EscapeNonWildCardSegments.Value;
            }
            if (parsedExample.UnescapeNonWildCardSegments != null)
            {
                return parsedExample.UnescapeNonWildCardSegments.Value;
            }
            return true;
        }

        public static bool GetEffectiveApplyLeadingSlash(DefaultPathTemplateFormatOptions options,
            DefaultPathTemplateExampleInternal parsedExample)
        {
            if (options?.ApplyLeadingSlash != null)
            {
                return options.ApplyLeadingSlash.Value;
            }
            if (parsedExample.MatchLeadingSlash != null)
            {
                return parsedExample.MatchLeadingSlash.Value;
            }
            // apply leading slashes by default.
            return true;
        }

        public static bool GetEffectiveApplyTrailingSlash(DefaultPathTemplateFormatOptions options,
            DefaultPathTemplateExampleInternal parsedExample)
        {
            if (options?.ApplyTrailingSlash != null)
            {
                return options.ApplyTrailingSlash.Value;
            }
            if (parsedExample.MatchTrailingSlash != null)
            {
                return parsedExample.MatchTrailingSlash.Value;
            }
            // omit trailing slash by default.
            return false;
        }

        public static string EncodeAlmostEveryUriChar(string uriComponent)
        {
            // encode every char except for those unreserved for any role according to RFC 3986: 
            //  a-zA-Z0-9 - . _ ~
            return Uri.EscapeDataString(uriComponent);
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

        internal static int FastConvertPercentEncodedToPositiveNum(StringBuilder s, int pos)
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

            // The unnecessary escapes are in the URI path portion according to RFC 3986 are
            // unreserved / sub-delims / ":" / "@":
            //  a-zA-Z0-9 - . _ ~ ! $ & ' ( ) * + , ; = : @

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
