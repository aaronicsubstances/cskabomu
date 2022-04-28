﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Helpers
{
    public static class CsvUtils
    {
        private static readonly int TOKEN_EOF = -1;
        private static readonly int TOKEN_COMMA = 1;
        private static readonly int TOKEN_QUOTE = 2;
        private static readonly int TOKEN_CRLF = 3;
        private static readonly int TOKEN_LF = 4;
        private static readonly int TOKEN_CR = 5;

        private static bool LocateNextToken(string csv, int start,
            bool searchForQuote, bool searchForQuoteOnly, int[] tokenInfo)
        {
            if (tokenInfo != null)
            {
                tokenInfo[0] = TOKEN_EOF;
                tokenInfo[1] = -1;
            }
            for (int i = start; i < csv.Length; i++)
            {
                char c = csv[i];
                if (!searchForQuoteOnly && c == ',')
                {
                    if (tokenInfo != null)
                    {
                        tokenInfo[0] = TOKEN_COMMA;
                        tokenInfo[1] = i;
                    }
                    return true;
                }
                if (!searchForQuoteOnly && c == '\n')
                {
                    if (tokenInfo != null)
                    {
                        tokenInfo[0] = TOKEN_LF;
                        tokenInfo[1] = i;
                    }
                    return true;
                }
                if (!searchForQuoteOnly && c == '\r')
                {
                    if (tokenInfo != null)
                    {
                        if (i + 1 < csv.Length && csv[i + 1] == '\n')
                        {
                            tokenInfo[0] = TOKEN_CRLF;
                        }
                        else
                        {
                            tokenInfo[0] = TOKEN_CR;
                        }
                        tokenInfo[1] = i;
                    }
                    return true;
                }
                if (searchForQuote && c == '"')
                {
                    if (searchForQuoteOnly && i + 1 < csv.Length &&
                        csv[i + 1] == '"')
                    {
                        // skip quote pair.
                        i++;
                    }
                    else
                    {
                        if (tokenInfo != null)
                        {
                            tokenInfo[0] = TOKEN_QUOTE;
                            tokenInfo[1] = i;
                        }
                        return true;
                    }
                }
            }
            return false;
        }

        public static List<List<string>> Deserialize(string csv)
        {
            var parsedCsv = new List<List<string>>();
            var currentRow = new List<string>();
            var nextValueStartIdx = 0;
            var isCommaTheLastSeparatorSeen = false;
            var tokenInfo = new int[2];
            while (nextValueStartIdx < csv.Length)
            {
                // use to detect infinite looping
                int savedNextValueStartIdx = nextValueStartIdx;

                // look for comma, quote or newline, whichever comes first.
                int newlineLen = 1;
                bool tokenIsNewline = false;
                isCommaTheLastSeparatorSeen = false;

                int nextValueEndIdx;
                int tokenType;

                // only respect quote separator at the very beginning
                // of parsing a column value
                if (csv[nextValueStartIdx] == '"')
                {
                    tokenType = TOKEN_QUOTE;
                    // locate ending quote, while skipping over
                    // double occurences of quotes.
                    if (!LocateNextToken(csv, nextValueStartIdx + 1, true, true, tokenInfo))
                    {
                        throw CreateCsvParseError(parsedCsv.Count, currentRow.Count,
                            "ending double quote not found", null);
                    }
                    nextValueEndIdx = tokenInfo[1] + 1;
                }
                else
                {
                    LocateNextToken(csv, nextValueStartIdx, false, false, tokenInfo);
                    tokenType = tokenInfo[0];
                    if (tokenType == TOKEN_COMMA)
                    {
                        nextValueEndIdx = tokenInfo[1];
                        isCommaTheLastSeparatorSeen = true;
                    }
                    else if (tokenType == TOKEN_LF || tokenType == TOKEN_CR)
                    {
                        nextValueEndIdx = tokenInfo[1];
                        tokenIsNewline = true;
                    }
                    else if (tokenType == TOKEN_CRLF)
                    {
                        nextValueEndIdx = tokenInfo[1];
                        tokenIsNewline = true;
                        newlineLen = 2;
                    }
                    else if (tokenType == TOKEN_EOF)
                    {
                        nextValueEndIdx = csv.Length;
                    }
                    else
                    {
                        throw new Exception("unexpected token type: " + tokenType);
                    }
                }

                // create new value for current row,
                // but skip empty values between newlines, or between BOI and newline.
                if (nextValueStartIdx < nextValueEndIdx || !tokenIsNewline || currentRow.Count > 0)
                {
                    string nextValue;
                    try
                    {
                        nextValue = UnescapeValue(csv.Substring(nextValueStartIdx,
                            nextValueEndIdx - nextValueStartIdx));
                    }
                    catch (Exception ex)
                    {
                        throw CreateCsvParseError(parsedCsv.Count, currentRow.Count, null, ex);
                    }
                    currentRow.Add(nextValue);
                }

                // advance input pointer.
                if (tokenType == TOKEN_COMMA)
                {
                    nextValueStartIdx = nextValueEndIdx + 1;
                }
                else if (tokenType == TOKEN_QUOTE)
                {
                    // validate that character after quote is EOI, comma or newline.
                    nextValueStartIdx = nextValueEndIdx;
                    if (nextValueStartIdx < csv.Length)
                    {
                        char c = csv[nextValueStartIdx];
                        if (c == ',')
                        {
                            isCommaTheLastSeparatorSeen = true;
                            nextValueStartIdx++;
                        }
                        else if (c == '\n' || c == '\r')
                        {
                            parsedCsv.Add(currentRow);
                            currentRow = new List<string>();
                            if (c == '\r' && nextValueStartIdx + 1 < csv.Length && csv[nextValueStartIdx + 1] == '\n')
                            {
                                nextValueStartIdx += 2;
                            }
                            else
                            {
                                nextValueStartIdx++;
                            }
                        }
                        else
                        {
                            throw CreateCsvParseError(parsedCsv.Count, currentRow.Count,
                                string.Format("unexpected character '{0}' found at beginning", c), null);
                        }
                    }
                    else
                    {
                        // leave to aftermath processing.
                    }
                }
                else if (tokenIsNewline)
                {
                    parsedCsv.Add(currentRow);
                    currentRow = new List<string>();
                    nextValueStartIdx = nextValueEndIdx + newlineLen;
                }
                else
                {
                    // leave to aftermath processing.
                    nextValueStartIdx = nextValueEndIdx;
                }

                // ensure input pointer has advanced.
                if (savedNextValueStartIdx >= nextValueStartIdx)
                {
                    throw CreateCsvParseError(parsedCsv.Count, currentRow.Count,
                        "algorithm bug detected as parsing didn't make an advance. Potential for infinite " +
                        "looping.", null);
                }
            }

            // generate empty value for case of trailing comma
            if (isCommaTheLastSeparatorSeen)
            {
                currentRow.Add("");
            }

            // add any leftover values to parsed csv rows.
            if (currentRow.Count > 0)
            {
                parsedCsv.Add(currentRow);
            }

            return parsedCsv;
        }

        private static Exception CreateCsvParseError(int row, int column, string errorMessage,
            Exception innerException)
        {
            throw new ArgumentException(string.Format("CSV parse error at row {0} column {1}: {2}",
                row + 1, column + 1, errorMessage ?? ""), innerException);
        }

        public static string Serialize(List<List<string>> rows)
        {
            var csvBuilder = new StringBuilder();
            foreach (var row in rows)
            {
                var addCommaSeparator = false;
                foreach (var value in row)
                {
                    if (addCommaSeparator)
                    {
                        csvBuilder.Append(",");
                    }
                    csvBuilder.Append(EscapeValue(value));
                    addCommaSeparator = true;
                }
                csvBuilder.Append("\n");
            }
            return csvBuilder.ToString();
        }

        public static string EscapeValue(string raw)
        {
            if (!LocateNextToken(raw, 0, true, false, null))
            {
                return raw;
            }
            return '"' + raw.Replace("\"", "\"\"") + '"';
        }

        public static string UnescapeValue(string escaped)
        {
            if (!LocateNextToken(escaped, 0, true, false, null))
            {
                return escaped;
            }
            if (escaped.Length < 2 || !escaped.StartsWith("\"") || !escaped.EndsWith("\""))
            {
                throw new ArgumentException("missing enclosing double quotes around csv value: " + escaped);
            }
            var unescaped = new StringBuilder();
            for (int i = 1; i < escaped.Length - 1; i++)
            {
                char c = escaped[i];
                unescaped.Append(c);
                if (c == '"')
                {
                    if (i == escaped.Length - 2 || escaped[i + 1] != '"')
                    {
                        throw new ArgumentException("unescaped double quote found in csv value: " + escaped);
                    }
                    i++;
                }
            }
            return unescaped.ToString();
        }
    }
}