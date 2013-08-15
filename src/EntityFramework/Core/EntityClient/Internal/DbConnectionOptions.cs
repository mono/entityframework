﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.Core.EntityClient.Internal
{
    using System.Collections;
    using System.Data.Entity.Resources;
    using System.Data.Entity.Utilities;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Text;
    using System.Text.RegularExpressions;

    /// <summary>
    ///     Copied from System.Data.dll
    /// </summary>
    internal class DbConnectionOptions
    {
        // instances of this class are intended to be immutable, i.e readonly
        // used by pooling classes so it is much easier to verify correctness
        // when not worried about the class being modified during execution

#if DEBUG
        private const string ConnectionStringPattern = // may not contain embedded null except trailing last value
            "([\\s;]*" // leading whitespace and extra semicolons
            + "(?![\\s;])" // key does not start with space or semicolon
            + "(?<key>([^=\\s\\p{Cc}]|\\s+[^=\\s\\p{Cc}]|\\s+==|==)+)"
            // allow any visible character for keyname except '=' which must quoted as '=='
            + "\\s*=(?!=)\\s*" // the equal sign divides the key and value parts
            + "(?<value>"
            + "(\"([^\"\u0000]|\"\")*\")" // double quoted string, " must be quoted as ""
            + "|"
            + "('([^'\u0000]|'')*')" // single quoted string, ' must be quoted as ''
            + "|"
            + "((?![\"'\\s])" // unquoted value must not start with " or ' or space, would also like = but too late to change
            + "([^;\\s\\p{Cc}]|\\s+[^;\\s\\p{Cc}])*" // control characters must be quoted
            + "(?<![\"']))" // unquoted value must not stop with " or '
            + ")(\\s*)(;|\u0000|$)" // whitespace after value up to semicolon or end-of-line
            + ")*" // repeat the key-value pair
            + "[\\s;\u0000]*" // traling whitespace/semicolons and embedded nulls (DataSourceLocator)
                             ;

        private static readonly Regex _connectionStringRegex = new Regex(
            ConnectionStringPattern, RegexOptions.ExplicitCapture | RegexOptions.Compiled);
#endif
        internal const string DataDirectory = "|datadirectory|";

#if DEBUG
        private const string ConnectionStringValidKeyPattern = "^(?![;\\s])[^\\p{Cc}]+(?<!\\s)$";
        // key not allowed to start with semi-colon or space or contain non-visible characters or end with space

        private const string ConnectionStringValidValuePattern = "^[^\u0000]*$"; // value not allowed to contain embedded null   
        private static readonly Regex _connectionStringValidKeyRegex = new Regex(ConnectionStringValidKeyPattern, RegexOptions.Compiled);
        private static readonly Regex _connectionStringValidValueRegex = new Regex(ConnectionStringValidValuePattern, RegexOptions.Compiled);
#endif

        private readonly string _usersConnectionString;
        private readonly Hashtable _parsetable;
        internal readonly NameValuePair KeyChain;

        /// <summary>
        ///     For testing.
        /// </summary>
        internal DbConnectionOptions()
        {
        }

        // synonyms hashtable is meant to be read-only translation of parsed string
        // keywords/synonyms to a known keyword string
        internal DbConnectionOptions(string connectionString, Hashtable synonyms)
        {
            _parsetable = new Hashtable();
            _usersConnectionString = ((null != connectionString) ? connectionString : "");

            // first pass on parsing, initial syntax check
            if (0 < _usersConnectionString.Length)
            {
                KeyChain = ParseInternal(_parsetable, _usersConnectionString, synonyms);
            }
        }

        internal string UsersConnectionString
        {
            get { return _usersConnectionString ?? string.Empty; }
        }

        internal bool IsEmpty
        {
            get { return (null == KeyChain); }
        }

        internal Hashtable Parsetable
        {
            get { return _parsetable; }
        }

        internal virtual string this[string keyword]
        {
            get { return (string)_parsetable[keyword]; }
        }

        [SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase")]
        private static string GetKeyName(StringBuilder buffer)
        {
            var count = buffer.Length;
            while ((0 < count)
                   && Char.IsWhiteSpace(buffer[count - 1]))
            {
                count--; // trailing whitespace
            }
            return buffer.ToString(0, count).ToLowerInvariant();
        }

        private static string GetKeyValue(StringBuilder buffer, bool trimWhitespace)
        {
            var count = buffer.Length;
            var index = 0;
            if (trimWhitespace)
            {
                while ((index < count)
                       && Char.IsWhiteSpace(buffer[index]))
                {
                    index++; // leading whitespace
                }
                while ((0 < count)
                       && Char.IsWhiteSpace(buffer[count - 1]))
                {
                    count--; // trailing whitespace
                }
            }
            return buffer.ToString(index, count - index);
        }

        // transistion states used for parsing
        private enum ParserState
        {
            NothingYet = 1, //start point
            Key,
            KeyEqual,
            KeyEnd,
            UnquotedValue,
            DoubleQuoteValue,
            DoubleQuoteValueQuote,
            SingleQuoteValue,
            SingleQuoteValueQuote,
            QuotedValueEnd,
            NullTermination,
        };

        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        private static int GetKeyValuePair(
            string connectionString, int currentPosition, StringBuilder buffer, out string keyname, out string keyvalue)
        {
            var startposition = currentPosition;

            buffer.Length = 0;
            keyname = null;
            keyvalue = null;

            var currentChar = '\0';

            var parserState = ParserState.NothingYet;
            var length = connectionString.Length;
            for (; currentPosition < length; ++currentPosition)
            {
                currentChar = connectionString[currentPosition];

                switch (parserState)
                {
                    case ParserState.NothingYet: // [\\s;]*
                        if ((';' == currentChar)
                            || Char.IsWhiteSpace(currentChar))
                        {
                            continue;
                        }
                        if ('\0' == currentChar)
                        {
                            parserState = ParserState.NullTermination;
                            continue;
                        }
                        if (Char.IsControl(currentChar))
                        {
                            throw new ArgumentException(Strings.ADP_ConnectionStringSyntax(startposition));
                        }
                        startposition = currentPosition;
                        if ('=' != currentChar)
                        {
                            parserState = ParserState.Key;
                            break;
                        }
                        else
                        {
                            parserState = ParserState.KeyEqual;
                            continue;
                        }

                    case ParserState.Key: // (?<key>([^=\\s\\p{Cc}]|\\s+[^=\\s\\p{Cc}]|\\s+==|==)+)
                        if ('=' == currentChar)
                        {
                            parserState = ParserState.KeyEqual;
                            continue;
                        }
                        if (Char.IsWhiteSpace(currentChar))
                        {
                            break;
                        }
                        if (Char.IsControl(currentChar))
                        {
                            throw new ArgumentException(Strings.ADP_ConnectionStringSyntax(startposition));
                        }
                        break;

                    case ParserState.KeyEqual: // \\s*=(?!=)\\s*
                        if ('=' == currentChar)
                        {
                            parserState = ParserState.Key;
                            break;
                        }
                        keyname = GetKeyName(buffer);
                        if (string.IsNullOrEmpty(keyname))
                        {
                            throw new ArgumentException(Strings.ADP_ConnectionStringSyntax(startposition));
                        }
                        buffer.Length = 0;
                        parserState = ParserState.KeyEnd;
                        goto case ParserState.KeyEnd;

                    case ParserState.KeyEnd:
                        if (Char.IsWhiteSpace(currentChar))
                        {
                            continue;
                        }
                        if ('\'' == currentChar)
                        {
                            parserState = ParserState.SingleQuoteValue;
                            continue;
                        }
                        if ('"' == currentChar)
                        {
                            parserState = ParserState.DoubleQuoteValue;
                            continue;
                        }

                        if (';' == currentChar)
                        {
                            goto ParserExit;
                        }
                        if ('\0' == currentChar)
                        {
                            goto ParserExit;
                        }
                        if (Char.IsControl(currentChar))
                        {
                            throw new ArgumentException(Strings.ADP_ConnectionStringSyntax(startposition));
                        }
                        parserState = ParserState.UnquotedValue;
                        break;

                    case ParserState.UnquotedValue: // "((?![\"'\\s])" + "([^;\\s\\p{Cc}]|\\s+[^;\\s\\p{Cc}])*" + "(?<![\"']))"
                        if (Char.IsWhiteSpace(currentChar))
                        {
                            break;
                        }
                        if (Char.IsControl(currentChar)
                            || ';' == currentChar)
                        {
                            goto ParserExit;
                        }
                        break;

                    case ParserState.DoubleQuoteValue: // "(\"([^\"\u0000]|\"\")*\")"
                        if ('"' == currentChar)
                        {
                            parserState = ParserState.DoubleQuoteValueQuote;
                            continue;
                        }
                        if ('\0' == currentChar)
                        {
                            throw new ArgumentException(Strings.ADP_ConnectionStringSyntax(startposition));
                        }
                        break;

                    case ParserState.DoubleQuoteValueQuote:
                        if ('"' == currentChar)
                        {
                            parserState = ParserState.DoubleQuoteValue;
                            break;
                        }
                        keyvalue = GetKeyValue(buffer, false);
                        parserState = ParserState.QuotedValueEnd;
                        goto case ParserState.QuotedValueEnd;

                    case ParserState.SingleQuoteValue: // "('([^'\u0000]|'')*')"
                        if ('\'' == currentChar)
                        {
                            parserState = ParserState.SingleQuoteValueQuote;
                            continue;
                        }
                        if ('\0' == currentChar)
                        {
                            throw new ArgumentException(Strings.ADP_ConnectionStringSyntax(startposition));
                        }
                        break;

                    case ParserState.SingleQuoteValueQuote:
                        if ('\'' == currentChar)
                        {
                            parserState = ParserState.SingleQuoteValue;
                            break;
                        }
                        keyvalue = GetKeyValue(buffer, false);
                        parserState = ParserState.QuotedValueEnd;
                        goto case ParserState.QuotedValueEnd;

                    case ParserState.QuotedValueEnd:
                        if (Char.IsWhiteSpace(currentChar))
                        {
                            continue;
                        }
                        if (';' == currentChar)
                        {
                            goto ParserExit;
                        }
                        if ('\0' == currentChar)
                        {
                            parserState = ParserState.NullTermination;
                            continue;
                        }
                        throw new ArgumentException(Strings.ADP_ConnectionStringSyntax(startposition)); // unbalanced single quote

                    case ParserState.NullTermination: // [\\s;\u0000]*
                        if ('\0' == currentChar)
                        {
                            continue;
                        }
                        if (Char.IsWhiteSpace(currentChar))
                        {
                            continue;
                        }
                        throw new ArgumentException(Strings.ADP_ConnectionStringSyntax(currentPosition));

                    default:
                        throw new InvalidOperationException(
                            Strings.ADP_InternalProviderError((int)EntityUtil.InternalErrorCode.InvalidParserState1));
                }
                buffer.Append(currentChar);
            }
            ParserExit:
            switch (parserState)
            {
                case ParserState.Key:
                case ParserState.DoubleQuoteValue:
                case ParserState.SingleQuoteValue:
                    // keyword not found/unbalanced double/single quote
                    throw new ArgumentException(Strings.ADP_ConnectionStringSyntax(startposition));

                case ParserState.KeyEqual:
                    // equal sign at end of line
                    keyname = GetKeyName(buffer);
                    if (string.IsNullOrEmpty(keyname))
                    {
                        throw new ArgumentException(Strings.ADP_ConnectionStringSyntax(startposition));
                    }
                    break;

                case ParserState.UnquotedValue:
                    // unquoted value at end of line
                    keyvalue = GetKeyValue(buffer, true);

                    var tmpChar = keyvalue[keyvalue.Length - 1];
                    if (('\'' == tmpChar)
                        || ('"' == tmpChar))
                    {
                        throw new ArgumentException(Strings.ADP_ConnectionStringSyntax(startposition));
                        // unquoted value must not end in quote
                    }
                    break;

                case ParserState.DoubleQuoteValueQuote:
                case ParserState.SingleQuoteValueQuote:
                case ParserState.QuotedValueEnd:
                    // quoted value at end of line
                    keyvalue = GetKeyValue(buffer, false);
                    break;

                case ParserState.NothingYet:
                case ParserState.KeyEnd:
                case ParserState.NullTermination:
                    // do nothing
                    break;

                default:
                    throw new InvalidOperationException(
                        Strings.ADP_InternalProviderError((int)EntityUtil.InternalErrorCode.InvalidParserState2));
            }
            if ((';' == currentChar)
                && (currentPosition < connectionString.Length))
            {
                currentPosition++;
            }
            return currentPosition;
        }

#if DEBUG
        private static bool IsValueValidInternal(string keyvalue)
        {
            if (null != keyvalue)
            {
                var compValue = _connectionStringValidValueRegex.IsMatch(keyvalue);
                Debug.Assert((-1 == keyvalue.IndexOf('\u0000')) == compValue, "IsValueValid mismatch with regex");
                return (-1 == keyvalue.IndexOf('\u0000'));
            }
            return true;
        }
#endif

        private static bool IsKeyNameValid(string keyname)
        {
            if (null != keyname)
            {
#if DEBUG
                var compValue = _connectionStringValidKeyRegex.IsMatch(keyname);
                Debug.Assert(
                    ((0 < keyname.Length) && (';' != keyname[0]) && !Char.IsWhiteSpace(keyname[0]) && (-1 == keyname.IndexOf('\u0000')))
                    == compValue, "IsValueValid mismatch with regex");
#endif
                return ((0 < keyname.Length) && (';' != keyname[0]) && !Char.IsWhiteSpace(keyname[0]) && (-1 == keyname.IndexOf('\u0000')));
            }
            return false;
        }

#if DEBUG
        [SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase")]
        private static Hashtable SplitConnectionString(string connectionString, Hashtable synonyms)
        {
            var parsetable = new Hashtable();
            var parser = _connectionStringRegex;

            const int KeyIndex = 1, ValueIndex = 2;
            Debug.Assert(KeyIndex == parser.GroupNumberFromName("key"), "wrong key index");
            Debug.Assert(ValueIndex == parser.GroupNumberFromName("value"), "wrong value index");

            if (null != connectionString)
            {
                var match = parser.Match(connectionString);
                if (!match.Success
                    || (match.Length != connectionString.Length))
                {
                    throw new ArgumentException(Strings.ADP_ConnectionStringSyntax(match.Length));
                }
                var indexValue = 0;
                var keyvalues = match.Groups[ValueIndex].Captures;
                foreach (Capture keypair in match.Groups[KeyIndex].Captures)
                {
                    var keyname = keypair.Value.Replace("==", "=").ToLowerInvariant();
                    var keyvalue = keyvalues[indexValue++].Value;
                    if (0 < keyvalue.Length)
                    {
                        switch (keyvalue[0])
                        {
                            case '\"':
                                keyvalue = keyvalue.Substring(1, keyvalue.Length - 2).Replace("\"\"", "\"");
                                break;
                            case '\'':
                                keyvalue = keyvalue.Substring(1, keyvalue.Length - 2).Replace("\'\'", "\'");
                                break;
                            default:
                                break;
                        }
                    }
                    else
                    {
                        keyvalue = null;
                    }

                    var realkeyname = ((null != synonyms) ? (string)synonyms[keyname] : keyname);
                    if (!IsKeyNameValid(realkeyname))
                    {
                        throw new ArgumentException(Strings.ADP_KeywordNotSupported(keyname));
                    }
                    parsetable[realkeyname] = keyvalue; // last key-value pair wins (or first)
                }
            }
            return parsetable;
        }

        private static void ParseComparision(Hashtable parsetable, string connectionString, Hashtable synonyms, Exception e)
        {
            try
            {
                var parsedvalues = SplitConnectionString(connectionString, synonyms);
                foreach (DictionaryEntry entry in parsedvalues)
                {
                    var keyname = (string)entry.Key;
                    var value1 = (string)entry.Value;
                    var value2 = (string)parsetable[keyname];
                    Debug.Assert(parsetable.Contains(keyname), "ParseInternal code vs. regex mismatch keyname <" + keyname + ">");
                    Debug.Assert(value1 == value2, "ParseInternal code vs. regex mismatch keyvalue <" + value1 + "> <" + value2 + ">");
                }
            }
            catch (ArgumentException f)
            {
                if (null != e)
                {
                    var msg1 = e.Message;
                    var msg2 = f.Message;
                    if (msg1.StartsWith("Keyword not supported:", StringComparison.Ordinal)
                        && msg2.StartsWith("Format of the initialization string", StringComparison.Ordinal))
                    {
                    }
                    else
                    {
                        // Does not always hold.
                        Debug.Assert(msg1 == msg2, "ParseInternal code vs regex message mismatch: <" + msg1 + "> <" + msg2 + ">");
                    }
                }
                else
                {
                    Debug.Assert(false, "ParseInternal code vs regex throw mismatch " + f.Message);
                }
                e = null;
            }
            if (null != e)
            {
                Debug.Assert(false, "ParseInternal code threw exception vs regex mismatch");
            }
        }
#endif

        private static NameValuePair ParseInternal(Hashtable parsetable, string connectionString, Hashtable synonyms)
        {
            DebugCheck.NotNull(connectionString);
            var buffer = new StringBuilder();
            NameValuePair localKeychain = null, keychain = null;
#if DEBUG
            try
            {
#endif
            var nextStartPosition = 0;
            var endPosition = connectionString.Length;
            while (nextStartPosition < endPosition)
            {
                var startPosition = nextStartPosition;

                string keyname, keyvalue;
                nextStartPosition = GetKeyValuePair(connectionString, startPosition, buffer, out keyname, out keyvalue);
                if (string.IsNullOrEmpty(keyname))
                {
                    // if (nextStartPosition != endPosition) { throw; }
                    break;
                }

#if DEBUG
                    Debug.Assert(IsKeyNameValid(keyname), "ParseFailure, invalid keyname");
                    Debug.Assert(IsValueValidInternal(keyvalue), "parse failure, invalid keyvalue");
#endif
                var realkeyname = ((null != synonyms) ? (string)synonyms[keyname] : keyname);
                if (!IsKeyNameValid(realkeyname))
                {
                    throw new ArgumentException(Strings.ADP_KeywordNotSupported(keyname));
                }
                parsetable[realkeyname] = keyvalue; // last key-value pair wins (or first)

                if (null != localKeychain)
                {
                    localKeychain = localKeychain.Next = new NameValuePair();
                }
                else
                {
                    // first time only - don't contain modified chain from UDL file
                    keychain = localKeychain = new NameValuePair();
                }
            }
#if DEBUG
            }
            catch (ArgumentException e)
            {
                ParseComparision(parsetable, connectionString, synonyms, e);
                throw;
            }
            ParseComparision(parsetable, connectionString, synonyms, null);
#endif
            return keychain;
        }
    }
}
