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
        private static readonly string KeyConstraints = "constraints";
        private static readonly string KeyDefaults = "defaults";
        private static readonly string KeyNamed = "named";

        public IDictionary<string, IPathConstraint> ConstraintFunctions { get; set; }

        public IPathTemplate Parse(string part1, object part2)
        {
            if (part1 == null)
            {
                throw new ArgumentNullException(nameof(part1));
            }

            DefaultPathTemplateMatchOptions optionsForAll = null;
            IDictionary<string, DefaultPathTemplateMatchOptions> individualOptions = null;
            if (part2 != null)
            {
                if (part2 is IList<DefaultPathTemplateMatchOptions>)
                {
                    individualOptions = (IDictionary<string, DefaultPathTemplateMatchOptions>)part2;
                }
                else
                {
                    optionsForAll = (DefaultPathTemplateMatchOptions)part2;
                }
            }

            var parsedCsv = CsvUtils.Deserialize(part1);

            var parsedExamples = new List<DefaultPathTemplateExampleInternal>();
            var defaultValues = new Dictionary<string, string>();

            var allConstraints = new Dictionary<string, IList<(string, string[])>>();

            // Copy over used constraints to make them available even after 
            // an update to this generator.
            var usedConstraintFunctions = new Dictionary<string, IPathConstraint>();

            string referenceKey = null;
            string referenceAfterKey = null;

            for (int i = 0; i < parsedCsv.Count; i++)
            {
                int rowNum = i + 1;
                var row = parsedCsv[i];

                // identify type of row.
                if (row.Count == 0)
                {
                    // cancel reference points of empty keys.
                    referenceKey = null;
                    referenceAfterKey = null;
                }
                else if (row[0].StartsWith("/"))
                {
                    var parsedRow = ParseExamples(rowNum, row, 0, optionsForAll);
                    parsedExamples.AddRange(parsedRow);
                }
                else if (row[0] == "")
                {
                    if (referenceKey == null)
                    {
                        throw AbortParse(rowNum, 1, "non-empty key expected at the " +
                            "beginning or just after an empty CSV row");
                    }
                    if (referenceKey == KeyNamed)
                    {
                        // even if there is no name or example specified, still make 
                        // it possible to use as reference.
                        if (row.Count > 1)
                        {
                            string name = row[1];
                            if (name == "")
                            {
                                name = referenceAfterKey;
                            }
                            referenceAfterKey = name;
                            DefaultPathTemplateMatchOptions optionToUse = null;
                            if (individualOptions != null && individualOptions.ContainsKey(name))
                            {
                                optionToUse = individualOptions[name];
                            }
                            var parsedRow = ParseExamples(rowNum, row, 2, optionToUse);
                            parsedExamples.AddRange(parsedRow);
                        }
                    }
                    else if (referenceKey == KeyDefaults)
                    {
                        ParseDefaultValues(row, defaultValues);
                    }
                    else if (referenceKey == KeyConstraints)
                    {
                        // even if there is no target value key or constraint function specified, still make 
                        // it possible to use as reference.
                        if (row.Count > 1)
                        {
                            string targetValueKey = row[1];
                            if (targetValueKey == "")
                            {
                                targetValueKey = referenceAfterKey;
                            }
                            referenceAfterKey = targetValueKey;
                            ParseConstraints(rowNum, row, targetValueKey, allConstraints, usedConstraintFunctions);
                        }
                    }
                    else
                    {
                        throw new ExpectationViolationException($"unexpected reference key: {referenceKey}");
                    }
                }
                else
                {
                    var key = row[0];
                    referenceKey = key;
                    if (key == KeyNamed)
                    {
                        // even if there is no name or example specified, still make 
                        // it possible to use as reference.
                        if (row.Count > 1)
                        {
                            string name = row[1];
                            referenceAfterKey = name;
                            DefaultPathTemplateMatchOptions optionToUse = null;
                            if (individualOptions != null && individualOptions.ContainsKey(name))
                            {
                                optionToUse = individualOptions[name];
                            }
                            var parsedRow = ParseExamples(rowNum, row, 2, optionToUse);
                            parsedExamples.AddRange(parsedRow);
                        }
                    }
                    else if (key == KeyDefaults)
                    {
                        ParseDefaultValues(row, defaultValues);
                    }
                    else if (key == KeyConstraints)
                    {
                        // even if there is no constraint function specified, still make 
                        // it possible to use as reference.
                        if (row.Count > 1)
                        {
                            string targetValueKey = row[1];
                            referenceAfterKey = targetValueKey;
                            ParseConstraints(rowNum, row, targetValueKey, allConstraints, usedConstraintFunctions);
                        }
                    }
                    else
                    {
                        throw AbortParse(rowNum, 1, $"unknown key: {key}");
                    }
                }
            }

            if (parsedExamples.Count == 0)
            {
                var lastColNum = 0;
                if (parsedCsv.Count > 0)
                {
                    lastColNum = parsedCsv[parsedCsv.Count - 1].Count;
                }
                throw AbortParse(parsedCsv.Count, lastColNum, "no examples specified");
            }

            // remove unnecessary escapes from all literal tokens after just determining
            // which of them are exempt from escaping.
            RemoveUnnecessaryUriEscapes(parsedExamples);

            var pathTemplate = new DefaultPathTemplateInternal
            {
                ParsedExamples = parsedExamples,
                DefaultValues = defaultValues,
                AllConstraints = allConstraints,
                ConstraintFunctions = usedConstraintFunctions,
            };

            return pathTemplate;
        }

        private Exception AbortParse(int rowNum, int colNum, string msg)
        {
            throw new ArgumentException($"parse error in CSV at row {rowNum} column {colNum}: {msg}");
        }

        private IList<DefaultPathTemplateExampleInternal> ParseExamples(int rowNum, IList<string> row,
            int startColIndex, DefaultPathTemplateMatchOptions options)
        {
            var parsedExamples = new List<DefaultPathTemplateExampleInternal>();
            for (int i = startColIndex; i < row.Count; i++)
            {
                var src = row[i];
                var tokens = ParseExample(rowNum, i + 1, src);
                var parsedExample = new DefaultPathTemplateExampleInternal
                {
                    Tokens = tokens
                };
                parsedExamples.Add(parsedExample);
                if (options != null)
                {
                    SetParsedExampleOptions(parsedExample, options);
                }
            }
            return parsedExamples;
        }

        private IList<PathToken> ParseExample(int rowNum, int colNum, string src)
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
            int wildCardChPos = -1;
            var nonLiteralNames = new HashSet<string>();
            var tokens = new List<PathToken>();
            // deal specially with '/' to be the same as the empty string.
            if (src == "/")
            {
                // return empty tokens
                return tokens;
            }
            while (startIndex < src.Length)
            {
                int startChPos = startIndex + 1;
                var m = SimpleTemplateSpecRegex.Match(src, startIndex);
                if (!m.Success || m.Index != startIndex)
                {
                    throw AbortParse(rowNum, colNum, $"invalid spec seen at char pos {startChPos}");
                }
                string tokenTypeIndicator = m.Groups[1].Value;
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

                string tokenValueIndicator = m.Groups[2].Value;
                string tokenValue = tokenValueIndicator.Trim();
                bool emptyValueAllowed = tokenValueIndicator != tokenValue;

                if (tokenType != PathToken.TokenTypeLiteral)
                {
                    if (nonLiteralNames.Contains(tokenValue))
                    {
                        throw AbortParse(rowNum, colNum, $"duplicate use of segment name at char pos {startChPos}");
                    }
                    nonLiteralNames.Add(tokenValue);
                }
                if (tokenType == PathToken.TokenTypeWildCard)
                {
                    if (wildCardChPos != -1)
                    {
                        throw AbortParse(rowNum, colNum, "duplicate specification of wild card segment " +
                            $"at char pos {startChPos} " +
                            $"(wild card segment already specified at {wildCardChPos})");
                    }
                    wildCardChPos = startChPos;
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

        private void ParseDefaultValues(IList<string> row, Dictionary<string, string> defaultValues)
        {
            for (int i = 1; i < row.Count; i += 2)
            {
                var defaultKey = row[i];
                string defaultValue = null;
                if (i + 1 < row.Count)
                {
                    defaultValue = row[i + 1];
                }
                if (defaultValues.ContainsKey(defaultKey))
                {
                    defaultValues[defaultKey] = defaultValue;
                }
                else
                {
                    defaultValues.Add(defaultKey, defaultValue);
                }
            }
        }

        private void ParseConstraints(int rowNum, IList<string> row, string targetValueKey,
            IDictionary<string, IList<(string, string[])>> allConstraints,
            IDictionary<string, IPathConstraint> usedConstraintFunctions)
        {
            if (row.Count <= 2)
            {
                return;
            }
            string constraintFunctionId = row[2];

            if (ConstraintFunctions == null || !ConstraintFunctions.ContainsKey(constraintFunctionId))
            {
                throw AbortParse(rowNum, 3, $"constraint function '{constraintFunctionId}' not found");
            }
            if (!usedConstraintFunctions.ContainsKey(constraintFunctionId))
            {
                usedConstraintFunctions.Add(constraintFunctionId, ConstraintFunctions[constraintFunctionId]);
            }
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
            var constraintFunctionArgs = row.Skip(3).ToArray();
            targetValueConstraints.Add(ValueTuple.Create(constraintFunctionId, constraintFunctionArgs));
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
