using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Common
{
    /// <summary>
    /// Provides CSV parsing functions for the Kabomu library.
    /// </summary>
    /// <remarks>
    /// The variant of CSV supported resembles that of Microsoft Exccel, in which
    /// <list type="bullet">
    /// <item>the character for separating columns is the comma</item>
    /// <item>the character for escaping commas and newlines is the double quote.</item>
    /// </list>
    /// </remarks>
    public static class CsvUtils
    {
        private static readonly int TOKEN_EOI = -1;
        private static readonly int TOKEN_COMMA = 1;
        private static readonly int TOKEN_QUOTE = 2;
        private static readonly int TOKEN_CRLF = 3;
        private static readonly int TOKEN_LF = 4;
        private static readonly int TOKEN_CR = 5;

        private static readonly byte[] NewlineConstant = new byte[] { (byte)'\n' };
        private static readonly byte[] CommaConstant = new byte[] { (byte)',' };

        /// <summary>
        /// Acts as a lexing function during CSV parsing.
        /// </summary>
        /// <param name="csv">CSV string to lex</param>
        /// <param name="start">the position in the CSV source string from which to search for next token</param>
        /// <param name="insideQuotedValue">provides context from deserializing function on whether parsing is currently in the midst of
        /// a quoted value</param>
        /// <param name="tokenInfo">Required 2-element array which will be filled with the token type and token position.</param>
        /// <returns>true if a token was found; false if end of input was reached.</returns>
        private static bool LocateNextToken(string csv, int start, bool insideQuotedValue, int[] tokenInfo)
        {
            // set to end of input by default
            tokenInfo[0] = TOKEN_EOI;
            tokenInfo[1] = -1;
            for (int i = start; i < csv.Length; i++)
            {
                char c = csv[i];
                if (!insideQuotedValue && c == ',')
                {
                    tokenInfo[0] = TOKEN_COMMA;
                    tokenInfo[1] = i;
                    return true;
                }
                if (!insideQuotedValue && c == '\n')
                {
                    tokenInfo[0] = TOKEN_LF;
                    tokenInfo[1] = i;
                    return true;
                }
                if (!insideQuotedValue && c == '\r')
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
                    return true;
                }
                if (insideQuotedValue && c == '"')
                {
                    if (i + 1 < csv.Length && csv[i + 1] == '"')
                    {
                        // skip quote pair.
                        i++;
                    }
                    else
                    {
                        tokenInfo[0] = TOKEN_QUOTE;
                        tokenInfo[1] = i;
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Parses a CSV string.
        /// </summary>
        /// <param name="csv">the csv string to parse.</param>
        /// <returns>CSV parse results as a list of rows, in which each row is represented as a list of values
        /// corresponding to the row's columns.</returns>
        /// <exception cref="ArgumentException">If an error occurs</exception>
        public static IList<IList<string>> Deserialize(string csv)
        {
            var parsedCsv = new List<IList<string>>();
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
                    if (!LocateNextToken(csv, nextValueStartIdx + 1, true, tokenInfo))
                    {
                        throw CreateCsvParseError(parsedCsv.Count, currentRow.Count,
                            "ending double quote not found");
                    }
                    nextValueEndIdx = tokenInfo[1] + 1;
                }
                else
                {
                    LocateNextToken(csv, nextValueStartIdx, false, tokenInfo);
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
                    else if (tokenType == TOKEN_EOI)
                    {
                        nextValueEndIdx = csv.Length;
                    }
                    else
                    {
                        throw new NotImplementedException("unexpected token type: " + tokenType);
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
                    catch (ArgumentException ex)
                    {
                        throw CreateCsvParseError(parsedCsv.Count, currentRow.Count, ex.Message);
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
                                string.Format("unexpected character '{0}' found at beginning", c));
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
                        "looping.");
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

        private static Exception CreateCsvParseError(int row, int column, string errorMessage)
        {
            throw new ArgumentException(string.Format("CSV parse error at row {0} column {1}: {2}",
                row + 1, column + 1, errorMessage ?? ""));
        }

        /// <summary>
        /// Serializes CSV data to a custom writer.
        /// </summary>
        /// <param name="rows">CSV data</param>
        /// <param name="writer">destination of CSV data to be written</param>
        /// <returns>task representing end of serialization</returns>
        public static async Task SerializeTo(IList<IList<string>> rows,
            object writer)
        {
            foreach (var row in rows)
            {
                var addCommaSeparator = false;
                foreach (var value in row)
                {
                    if (addCommaSeparator)
                    {
                        await IOUtils.WriteBytes(writer, CommaConstant, 0,
                            CommaConstant.Length);
                    }
                    await EscapeValueTo(value, writer);
                    addCommaSeparator = true;
                }
                await IOUtils.WriteBytes(writer, NewlineConstant, 0,
                    NewlineConstant.Length);
            }
        }

        /// <summary>
        /// Generates a CSV string.
        /// </summary>
        /// <param name="rows">Data for CSV generation. Each row is a list whose entries will be treated as the values of
        /// columns in the row. Also no row is treated specially.</param>
        /// <returns>CSV string corresponding to rows</returns>
        public static string Serialize(IList<IList<string>> rows)
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

        /// <summary>
        /// Escapes a string to a custom writer. for use as a CSV column value
        /// </summary>
        /// <param name="raw">value to escape. Note that empty strings are always escaped as two double quotes.</param>
        /// <param name="writer">destination of escaped value</param>
        /// <returns>task representing end of escape</returns>
        public static async Task EscapeValueTo(string raw, object writer)
        {
            // escape empty strings with two double quotes to resolve ambiguity
            // between an empty row and a row containing an empty string - otherwise both
            // serialize to the same CSV output.
            if (raw == "" || DoesValueContainSpecialCharacters(raw))
            {
                raw = '"' + raw.Replace("\"", "\"\"") + '"';
            }
            var rawBytes = ByteUtils.StringToBytes(raw);
            await IOUtils.WriteBytes(writer, rawBytes, 0, rawBytes.Length);
        }

        /// <summary>
        /// Escapes a CSV value. Note that empty strings are always escaped as two double quotes.
        /// </summary>
        /// <param name="raw">CSV value to escape.</param>
        /// <returns>Escaped CSV value.</returns>
        public static string EscapeValue(string raw)
        {
            if (!DoesValueContainSpecialCharacters(raw))
            {
                // escape empty strings with two double quotes to resolve ambiguity
                // between an empty row and a row containing an empty string - otherwise both
                // serialize to the same CSV output.
                return raw == "" ? "\"\"" : raw;
            }
            return '"' + raw.Replace("\"", "\"\"") + '"';
        }

        /// <summary>
        /// Reverses the escaping of a CSV value.
        /// </summary>
        /// <param name="escaped">CSV escaped value.</param>
        /// <returns>CSV value which equals escaped argument when escaped.</returns>
        /// <exception cref="T:System.ArgumentException">The <paramref name="escaped"/> argument is an invalid escaped value.</exception>
        public static string UnescapeValue(string escaped)
        {
            if (!DoesValueContainSpecialCharacters(escaped))
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

        private static bool DoesValueContainSpecialCharacters(string s)
        {
            foreach (var c in s)
            {
                if (c == ',' || c == '"' || c == '\r' || c == '\n')
                {
                    return true;
                }
            }
            return false;
        }
    }
}
