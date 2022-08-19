using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using static Kabomu.Mediator.Path.DefaultPathTemplateExampleInternal;

namespace Kabomu.Mediator.Path
{
    public class DefaultPathTemplateGenerator : IPathTemplateGenerator
    {
        private static readonly Regex SimpleTemplateSpecRegex = new Regex("(/{1,3})([^/]+)");

        public DefaultPathTemplateGenerator()
        {
            ConstraintFunctions = new Dictionary<string, IPathConstraint>();
        }

        /// <summary>
        /// Intended to be edited even if it leads to the removal of already existing values.
        /// </summary>
        public Dictionary<string, IPathConstraint> ConstraintFunctions { get; }

        public IPathTemplate Parse(string part1, object part2)
        {
            if (part1 == null)
            {
                throw new ArgumentNullException(nameof(part1));
            }

            DefaultPathTemplateMatchOptions optionsForAll = null;
            IList<DefaultPathTemplateMatchOptions> individualOptions = null;
            if (part2 != null)
            {
                if (part2 is IList<DefaultPathTemplateMatchOptions>)
                {
                    individualOptions = (IList<DefaultPathTemplateMatchOptions>)part2;
                }
                else
                {
                    optionsForAll = (DefaultPathTemplateMatchOptions)part2;
                }
            }

            var lines = PathUtilsInternal.SplitTemplateSpecIntoLines(part1);

            var endIdxOfRangeOfExamples = LocateEndIndexOfRangeOfExamples(lines);

            if (endIdxOfRangeOfExamples == -1)
            {
                throw new ArgumentException("no path template examples specified");
            }

            var pathTemplateExamples = new List<DefaultPathTemplateExampleInternal>();

            for (int i = 0; i < endIdxOfRangeOfExamples; i++)
            {
                var line = lines[i];
                if (line.Trim() == "")
                {
                    continue;
                }
                var tokens = ParsePathTemplateExample(i + 1, line);
                var parsedExample = new DefaultPathTemplateExampleInternal
                {
                    Tokens = tokens
                };
                pathTemplateExamples.Add(parsedExample);
            }

            // parse lines after examples together as CSV
            var remainder = new StringBuilder();
            for (int i = endIdxOfRangeOfExamples; i < lines.Count; i++)
            {
                remainder.AppendLine(lines[i]);
            }

            IList<IList<string>> parsedCsv = CsvUtils.Deserialize(remainder.ToString());

            Dictionary<string, string> defaultValues = null;
            if (parsedCsv.Count > 0)
            {
                defaultValues = new Dictionary<string, string>();
                var rowOfDefaultValues = parsedCsv[0];
                for (int i = 0; i < rowOfDefaultValues.Count; i += 2)
                {
                    var key = rowOfDefaultValues[i];
                    if (defaultValues.ContainsKey(key))
                    {
                        throw new ArgumentException("CSV row of default values contains duplicate keys");
                    }
                    if (i + 1 >= rowOfDefaultValues.Count)
                    {
                        throw new ArgumentException("last default value is missing");
                    }
                    var value = rowOfDefaultValues[i + 1];
                    defaultValues.Add(key, value);
                }
            }

            // end parsing by validating and saving constraints.
            var allConstraints = new Dictionary<string, IList<(string, string[])>>();
            var constraintFunctionIds = new HashSet<string>();
            for (int i = 1; i < parsedCsv.Count; i++) // skip default values row.
            {
                var row = parsedCsv[i];
                if (row.Count < 2)
                {
                    continue;
                }
                var targetValueKey = row[0];
                var constraintFunctionId = row[1];
                if (!ConstraintFunctions.ContainsKey(constraintFunctionId))
                {
                    throw new Exception($"constraint function '{constraintFunctionId}' not found");
                }
                constraintFunctionIds.Add(constraintFunctionId);
                IList<(string, string[])> targetValueConstraints;
                if (allConstraints.ContainsKey(targetValueKey))
                {
                    targetValueConstraints = allConstraints[targetValueKey];
                }
                else
                {
                    targetValueConstraints = new List<(string, string[])>();
                    allConstraints.Add(targetValueKey, targetValueConstraints);
                }
                var constraintFunctionArgs = row.Skip(2).ToArray();
                targetValueConstraints.Add(ValueTuple.Create(constraintFunctionId, constraintFunctionArgs));
            }

            // Copy over path constraints to make them available even after 
            // an update to this generator.
            var constraintFunctions = new Dictionary<string, IPathConstraint>();
            foreach (var constraintFunctionId in constraintFunctionIds)
            {
                constraintFunctions.Add(constraintFunctionId, ConstraintFunctions[constraintFunctionId]);
            }

            // assign options to parsed examples.
            if (individualOptions != null || optionsForAll != null)
            {
                int index = 0;
                foreach (var parsedExample in pathTemplateExamples)
                {
                    DefaultPathTemplateMatchOptions optionToUse;
                    if (individualOptions != null)
                    {
                        optionToUse = individualOptions[index];
                        index++;
                    }
                    else
                    {
                        optionToUse = optionsForAll;
                    }
                    SetParsedExampleOptions(parsedExample, optionToUse);
                }
            }

            // remove unnecessary escapes from all literal tokens after just determining
            // which of them are exempt from escaping.
            RemoveUnnecessaryUriEscapes(pathTemplateExamples);

            var pathTemplate = new DefaultPathTemplate
            {
                ParsedExamples = pathTemplateExamples,
                DefaultValues = defaultValues,
                AllConstraints = allConstraints,
                ConstraintFunctions = constraintFunctions,
            };

            return pathTemplate;
        }

        private static int LocateEndIndexOfRangeOfExamples(IList<string> lines)
        {
            int endIdx = -1;

            // locate first non-whitespace line.
            int i = 0;
            for (; i < lines.Count; i++)
            {
                var line = lines[i];
                if (line.Trim() != "")
                {
                    break;
                }
            }

            // locate next whitespace line.
            for (; i < lines.Count; i++)
            {
                var line = lines[i];
                if (line.Trim() == "")
                {
                    endIdx = i;
                    break;
                }
            }

            // extend end to next non-whitespace line
            endIdx++;
            for (; i < lines.Count; i++)
            {
                var line = lines[i];
                if (line.Trim() != "")
                {
                    break;
                }
                endIdx++;
            }

            return endIdx;
        }

        private IList<PathToken> ParsePathTemplateExample(int lineNum, string lineOfTokens)
        {
            // Interpret path spec as either 
            //  1. a single slash, or
            //  2. zero or more concatenations of /literal or //segment or ///wildcard.
            // where literal, segment or wildcard have the ff x'tics:
            //  a. cannot be empty
            //  b. cannot contain slashes
            //  c. surrounding whitespace will be trimmed off.
            //  d. a segment surrounded by whitespace will be interpreted to mean it allows for empty values.

            int startIndex = 0;
            int wildCardCharIndex = -1;
            var nonLiteralNames = new HashSet<string>();
            var tokens = new List<PathToken>();
            // deal specially with '/' to be the same as the empty string.
            if (lineOfTokens == "/")
            {
                // return empty tokens
                return tokens;
            }
            while (startIndex < lineOfTokens.Length)
            {
                var m = SimpleTemplateSpecRegex.Match(lineOfTokens, startIndex);
                if (!m.Success || m.Index != startIndex)
                {
                    throw new ArgumentException($"invalid spec seen at char pos {startIndex + 1}");
                }
                string tokenTypeIndicator = m.Groups[0].Value;
                int tokenType;
                switch (tokenTypeIndicator)
                {
                    case "/":
                        tokenType = PathToken.TokenTypeLiteral;
                        break;
                    case "//":
                        tokenType = PathToken.TokenTypeSegment;
                        break;
                    case "///":
                        tokenType = PathToken.TokenTypeWildCard;
                        break;
                    default:
                        throw new ExpectationViolationException("unexpected token type indicator: " +
                            tokenTypeIndicator);
                }

                string tokenValueIndicator = m.Groups[1].Value;
                string tokenValue = tokenValueIndicator.Trim();
                bool emptyValueAllowed = tokenValueIndicator != tokenValue;

                if (tokenType != PathToken.TokenTypeLiteral)
                {
                    if (nonLiteralNames.Contains(tokenValue))
                    {
                        throw new ArgumentException($"duplicate use of segment name at char pos {startIndex + 1}");
                    }
                    nonLiteralNames.Add(tokenValue);
                }
                if (tokenType == PathToken.TokenTypeWildCard)
                {
                    if (wildCardCharIndex != -1)
                    {
                        throw new ArgumentException($"duplicate specification of wild card segment at char pos {startIndex + 1}. " +
                            $"(wild card segment already specified at {wildCardCharIndex + 1})");
                    }
                    wildCardCharIndex = startIndex;
                }

                // add new token
                var token = new PathToken
                {
                    Type = tokenType,
                    Value = tokenValue,
                    EmptySegmentAllowed = emptyValueAllowed
                };
                tokens.Add(token);

                // advance loop
                startIndex += m.Length;
            }

            return tokens;
        }

        private static void SetParsedExampleOptions(DefaultPathTemplateExampleInternal parsedExample,
            DefaultPathTemplateMatchOptions option)
        {
            parsedExample.UnescapeNonWildCardSegments = option.UnescapeNonWildCardSegments;
            parsedExample.CaseSensitiveMatchEnabled = option.CaseSensitiveMatchEnabled;
            parsedExample.MatchLeadingSlash = option.MatchLeadingSlash;
            parsedExample.MatchTrailingSlash = option.MatchTrailingSlash;
        }

        private static void RemoveUnnecessaryUriEscapes(IList<DefaultPathTemplateExampleInternal> parsedExamples)
        {
            foreach (var parsedExample in parsedExamples)
            {
                if (parsedExample.UnescapeNonWildCardSegments == false)
                {
                    continue;
                }
                foreach (var token in parsedExample.Tokens)
                {
                    if (token.Type == PathToken.TokenTypeLiteral)
                    {
                        token.Value = PathUtilsInternal.ReverseUnnecessaryUriEscapes(token.Value);
                    }
                }
            }
        }
    }
}
