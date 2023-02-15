﻿using Chess.Pgn.IParser.Exceptions;
using System.Text;


namespace Chess.Pgn.Parser
{
    /// <summary>
    /// Do the lexical analysis of a PGN document
    /// </summary>
    public class PgnLexical
    {

        /// <summary>
        /// Token type
        /// </summary>
        public enum TokenType
        {
            /// <summary>Integer value</summary>
            Integer,
            /// <summary>String value</summary>
            String,
            /// <summary>Symbol</summary>
            Symbol,
            /// <summary>Single DOT</summary>
            Dot,
            /// <summary>NAG value</summary>
            Nag,
            /// <summary>Openning square bracket</summary>
            OpenSBracket,
            /// <summary>Closing square bracket</summary>
            CloseSBracket,
            /// <summary>Termination symbol</summary>
            Termination,
            /// <summary>Unknown token</summary>
            UnknownToken,
            /// <summary>Comment</summary>
            Comment,
            /// <summary>End of file</summary>
            Eof
        }

        /// <summary>
        /// Token value
        /// </summary>
        public struct Token
        {
            /// <summary>Token type</summary>
            public TokenType Type;
            /// <summary>Token string value if any</summary>
            public string StrValue;
            /// <summary>Token integer value if any</summary>
            public int IntValue;
            /// <summary>Token starting position</summary>
            public long StartPos;
            /// <summary>Token size</summary>
            public int Size;
        }

        private const int MaxBufferSize = 1048576;
        /// <summary>List of buffers</summary>
        private List<char[]?> m_buffer;
        /// <summary>Position in the buffer</summary>
        private int m_posInBuffer;

        /// <summary>Current array</summary>
        private char[]? m_curArray;
        /// <summary>Current array size</summary>
        private int m_curArraySize;
        /// <summary>Position within the raw array</summary>
        private long m_curBasePos;

        /// <summary>Pushed character if any</summary>
        private char? m_chrPushed;
        /// <summary>true if at the first character of a line</summary>
        private bool m_isFirstChrInLine;
        /// <summary>Pushed token</summary>
        private Token? m_tokPushed;

        /// <summary>
        /// Ctor
        /// </summary>
        public PgnLexical()
        {
            m_buffer = new List<char[]?>(0);
            Clear(allocateEmpty: true);
        }

        /// <summary>
        /// Clear all buffers
        /// </summary>
        /// <param name="allocateEmpty">   true to allocate an empty block</param>
        public void Clear(bool allocateEmpty)
        {
            m_buffer = new List<char[]?>(256);
            m_posInBuffer = 0;
            CurrentBufferPos = 0;
            m_curBasePos = 0;
            TextSize = 0;
            m_chrPushed = null;
            m_tokPushed = null;
            m_isFirstChrInLine = true;
            if (allocateEmpty)
            {
                m_buffer.Add(Array.Empty<char>());
            }
        }

        /// <summary>
        /// Current position
        /// </summary>
        public long CurrentPosition => m_curBasePos + m_posInBuffer;

        /// <summary>
        /// Text size
        /// </summary>
        public long TextSize { get; private set; }

        /// <summary>
        /// Gets the number of buffer which has been allocated
        /// </summary>
        public int BufferCount => m_buffer.Count;

        /// <summary>
        /// Current buffer position
        /// </summary>
        public int CurrentBufferPos { get; private set; }

        /// <summary>
        /// Initialize the buffer from a file
        /// </summary>
        /// <param name="inpFileName">  File name to open</param>
        /// <returns>
        /// Stream or null if unable to open the file.
        /// </returns>
        public bool InitFromFile(string inpFileName)
        {
            bool retVal = false;
            FileStream? streamInp = null;
            StreamReader? streamReader = null;

            try
            {
                streamInp = File.OpenRead(inpFileName);
                if (streamInp != null)
                {
                    streamReader = new StreamReader(streamInp, Encoding.GetEncoding("utf-8"), true, 65536);
                    streamInp = null;
                    ReadInMemory(streamReader);
                    retVal = true;
                }
            }
            catch (Exception ex)
            {
                if (streamInp != null)
                {
                    streamInp.Dispose();
                }
                else if (streamReader != null)
                {
                    streamReader.Dispose();
                }
                throw new PgnParserException($"Unable to read the file - {inpFileName}.\r\n" + ex.Message);
            }
            return retVal;
        }

        /// <summary>
        /// Initialize from string
        /// </summary>
        /// <param name="text"> Text string</param>
        public void InitFromString(string text)
        {
            Clear(allocateEmpty: false);
            m_buffer.Add(text.ToArray());
            m_curArray = m_buffer[0];
            m_curArraySize = m_curArray!.Length;
            TextSize = m_curArraySize;
        }

        /// <summary>
        /// Fill the buffer
        /// </summary>
        private void ReadInMemory(StreamReader streamReader)
        {
            char[] arr;
            char[] tmpArr;
            int readSize;

            Clear(allocateEmpty: false);
            arr = new char[MaxBufferSize];
            readSize = streamReader.ReadBlock(arr, 0, MaxBufferSize);
            TextSize = 0;
            while (readSize == MaxBufferSize)
            {
                TextSize += MaxBufferSize;
                m_buffer.Add(arr);
                arr = new char[MaxBufferSize];
                readSize = streamReader.ReadBlock(arr, 0, MaxBufferSize);
            }
            if (readSize != 0)
            {
                TextSize += readSize;
                tmpArr = new char[readSize];
                for (int i = 0; i < readSize; i++)
                {
                    tmpArr[i] = arr[i];
                }
                m_buffer.Add(tmpArr);
            }
            if (m_buffer.Count == 0)
            {
                m_buffer.Add(Array.Empty<char>());
            }
            m_curArray = m_buffer[0];
            m_curArraySize = m_curArray!.Length;
        }

        /// <summary>
        /// Select the next buffer in list
        /// </summary>
        /// <returns>
        /// true if succeed, false if EOF
        /// </returns>
        private bool SelectNextBuffer()
        {
            bool retVal;

            if (CurrentBufferPos + 1 < m_buffer.Count)
            {
                m_curBasePos += m_curArray!.Length;
                m_curArray = m_buffer[++CurrentBufferPos];
                m_posInBuffer = 0;
                m_curArraySize = m_curArray!.Length;
                retVal = true;
            }
            else
            {
                retVal = false;
            }
            return retVal;
        }

        /// <summary>
        /// Peek a character
        /// </summary>
        /// <returns>
        /// Character or 0 if EOF
        /// </returns>
        public char PeekChr()
        {
            char retVal;
            char[] arr;

            if (m_chrPushed.HasValue)
            {
                retVal = m_chrPushed.Value;
            }
            else if (m_posInBuffer < m_curArraySize)
            {
                retVal = m_curArray![m_posInBuffer];
            }
            else if (CurrentBufferPos + 1 < m_buffer.Count)
            {
                arr = m_buffer[CurrentBufferPos + 1]!;
                retVal = arr.Length == 0 ? '\0' : arr[0];
            }
            else
            {
                retVal = '\0';
            }
            return retVal;
        }

        /// <summary>
        /// Get the next character
        /// </summary>
        /// <returns>
        /// Character or 0 if EOF
        /// </returns>
        private char GetChrInt()
        {
            char retVal;

            if (m_chrPushed.HasValue)
            {
                retVal = m_chrPushed.Value;
                m_chrPushed = null;
            }
            else if (m_posInBuffer < m_curArraySize)
            {
                retVal = m_curArray![m_posInBuffer++];
            }
            else if (SelectNextBuffer())
            {
                if (m_curArraySize > 0)
                {
                    m_posInBuffer = 1;
                    retVal = m_curArray![0];
                }
                else
                {
                    m_posInBuffer = 0;
                    retVal = '\0';
                }
            }
            else
            {
                retVal = '\0';
            }
            if (retVal == '\r')
            {
                m_isFirstChrInLine = true;
            }
            else if (retVal != '\n')
            {
                m_isFirstChrInLine = false;
            }
            return retVal;
        }

        /// <summary>
        /// Push back a character
        /// </summary>
        /// <param name="chr">  Character to push</param>
        public void PushChr(char chr)
        {
            m_chrPushed = m_chrPushed == null ? chr : throw new MethodAccessException("Cannot push two characters!");
        }

        /// <summary>
        /// Skip whitespace
        /// </summary>
        public void SkipSpace()
        {
            char chr;
            bool isNextArray;

            if (!m_chrPushed.HasValue || (chr = m_chrPushed.Value) == ' ' || chr == '\r' || chr == '\n' || chr == (char)9)
            {
                m_chrPushed = null;
                do
                {
                    while (m_posInBuffer < m_curArraySize && ((chr = m_curArray![m_posInBuffer]) == ' ' || chr == '\r' || chr == '\n' || chr == (char)9))
                    {
                        if (chr == '\r')
                        {
                            m_isFirstChrInLine = true;
                        }
                        else if (chr != '\n')
                        {
                            m_isFirstChrInLine = false;
                        }
                        m_posInBuffer++;
                    }
                    isNextArray = m_posInBuffer >= m_curArraySize && SelectNextBuffer();
                } while (isNextArray);
            }
        }

        /// <summary>
        /// Skip the rest of the line
        /// </summary>
        private void SkipLine()
        {
            char chr;

            do
            {
                chr = GetChrInt();
            } while (chr is not '\r' and not '\0');
            while (PeekChr() == '\n')
            {
                _ = GetChrInt();
            }
            m_isFirstChrInLine = true;
        }

        /// <summary>
        /// Get a character
        /// </summary>
        /// <returns>
        /// Character
        /// </returns>
        public char GetChr()
        {
            char retVal;
            bool toContinue;

            do
            {
                retVal = GetChrInt();
                toContinue = m_isFirstChrInLine && (retVal == ';' || retVal == '%');
                if (toContinue)
                {
                    SkipLine();
                }
            } while (toContinue);
            return retVal;
        }

        /// <summary>
        /// Gets the string at the specified position
        /// </summary>
        /// <param name="startingPos"> Starting position in text</param>
        /// <param name="length">      String size</param>
        /// <returns>
        /// String or null if bad position specified
        /// </returns>
        public string? GetStringAtPos(long startingPos, int length)
        {
            string? retVal;
            int posInBuf;
            int posInList;
            int maxSize;
            char[]? arr;
            StringBuilder strb;

            if (length > MaxBufferSize)
            {
                throw new ArgumentException("Length too big");
            }
            else if (length == 0)
            {
                retVal = "<empty>";
            }
            else
            {
                strb = new StringBuilder(length + 1);
                posInList = (int)(startingPos / MaxBufferSize);
                posInBuf = (int)(startingPos % MaxBufferSize);
                if (posInList < m_buffer.Count)
                {
                    arr = m_buffer[posInList];
                    maxSize = arr!.Length - posInBuf;
                    if (length <= maxSize)
                    {
                        _ = strb.Append(arr, posInBuf, length);
                    }
                    else if (posInList < m_buffer.Count)
                    {
                        _ = strb.Append(arr, posInBuf, maxSize);
                        _ = strb.Append(m_buffer[posInList + 1], 0, length - maxSize);
                    }
                }
                retVal = posInList == -1 ? null : strb.ToString();
            }
            return retVal;
        }

        /// <summary>
        /// Returns if the text is probably a single FEN (no more than one line)
        /// </summary>
        /// <returns>
        /// true if probably a single FEN
        /// </returns>
        public bool IsOnlyFen()
        {
            bool retVal = m_buffer.Count <= 1 && m_buffer[0]!.Count(x => x == '\r') <= 1;
            return retVal;
        }

        /// <summary>
        /// Flush old buffer to save memory
        /// </summary>
        public void FlushOldBuffer()
        {
            int index;

            index = CurrentBufferPos - 2;
            while (index >= 0 && m_buffer[index] != null)
            {
                m_buffer[index] = null;
                index--;
            }
        }

        /// <summary>
        /// Fetch a string token
        /// </summary>
        /// <returns>
        /// String
        /// </returns>
        private string GetStringToken()
        {
            char chr;
            StringBuilder strb;

            strb = new StringBuilder();
            do
            {
                chr = GetChr();
                switch (chr)
                {
                    case '\r':
                        throw new PgnParserException("String cannot return a new line");
                    case '\0':
                        throw new PgnParserException("Missing string termination quote");
                    case '\\':
                        chr = GetChr();
                        if (chr == '"')
                        {
                            _ = strb.Append(chr);
                        }
                        else
                        {
                            _ = strb.Append('\\');
                            _ = strb.Append(chr);
                        }
                        break;
                    case '"':
                        break;
                    default:
                        _ = strb.Append(chr);
                        break;
                }
            } while (chr != '"');
            return strb.ToString();
        }

        /// <summary>
        /// Get an integer
        /// </summary>
        /// <param name="firstChr">    First character</param>
        /// <returns>
        /// Integer value
        /// </returns>
        private int GetIntegerToken(char firstChr)
        {
            int retVal;
            char chr;

            retVal = firstChr - '0';
            while ((chr = GetChr()) >= '0' && chr <= '9')
            {
                retVal = (retVal * 10) + (chr - '0');
            }
            PushChr(chr);
            return retVal;
        }

        /// <summary>
        /// Fetch a symbol token
        /// </summary>
        /// <param name="firstChr">     First character</param>
        /// <param name="isAllDigit">   true if symbol is only composed of digit</param>
        /// <param name="isSlashFound"> Found a slash in the symbol. Only valid for 1/2-1/2</param>
        /// <returns>
        /// Symbol
        /// </returns>
        private string GetSymbolToken(char firstChr, out bool isAllDigit, out bool isSlashFound)
        {
            char chr;
            StringBuilder strb;

            isSlashFound = false;
            isAllDigit = firstChr is >= '0' and <= '9';
            strb = new StringBuilder();
            _ = strb.Append(firstChr);
            chr = GetChr();
            while (chr is (>= 'a' and <= 'z') or
                   (>= 'A' and <= 'Z') or
                   (>= '0' and <= '9') or
                   '_' or
                   '+' or
                   '#' or
                   '=' or
                   ':' or
                   '-' or
                   '/')
            {
                if (chr == '/')
                {
                    isSlashFound = true;
                }
                _ = strb.Append(chr);
                if (isAllDigit && (chr < '0' || chr > '9'))
                {
                    isAllDigit = false;
                }
                chr = GetChr();
            }
            PushChr(chr);
            return strb.ToString();
        }

        /// <summary>
        /// Get the next token
        /// </summary>
        /// <returns>
        /// Token
        /// </returns>
        public Token GetNextToken()
        {
            Token retVal;
            char chr;
            bool isComment;
            int parCount;

            if (m_tokPushed.HasValue)
            {
                retVal = m_tokPushed.Value;
                m_tokPushed = null;
            }
            else
            {
                retVal = new Token();
                do
                {
                    SkipSpace();
                    isComment = false;
                    retVal.StartPos = CurrentPosition;
                    chr = GetChr();
                    switch (chr)
                    {
                        case '\0':
                            retVal.Type = TokenType.Eof;
                            break;
                        case '\"':
                            retVal.Type = TokenType.String;
                            retVal.StrValue = GetStringToken();
                            retVal.Size = (int)(CurrentPosition - retVal.StartPos);
                            break;
                        case '.':
                            retVal.Type = TokenType.Dot;
                            while (PeekChr() == '.')
                            {
                                _ = GetChr();
                            }
                            retVal.Size = (int)(CurrentPosition - retVal.StartPos + 1);
                            break;
                        case '$':
                            chr = GetChr();
                            if (chr is < '0' or > '9')
                            {
                                throw new PgnParserException("Invalid NAG");
                            }
                            else
                            {
                                retVal.Type = TokenType.Nag;
                                retVal.IntValue = GetIntegerToken(chr);
                            }
                            retVal.Size = (int)(CurrentPosition - retVal.StartPos - 1);
                            break;
                        case '[':
                            retVal.Type = TokenType.OpenSBracket;
                            retVal.Size = 1;
                            break;
                        case ']':
                            retVal.Type = TokenType.CloseSBracket;
                            retVal.Size = 1;
                            break;
                        case '{':
                            isComment = true;
                            while ((chr = GetChr()) != 0 && chr != '}')
                            {
                                ;
                            }

                            break;
                        case '(':
                            isComment = true;
                            parCount = 1;
                            while (parCount != 0 && (chr = GetChr()) != 0)
                            {
                                if (chr == '(')
                                {
                                    parCount++;
                                }
                                else if (chr == ')')
                                {
                                    parCount--;
                                }
                                else if (chr == '{')
                                {
                                    while ((chr = GetChr()) != 0 && chr != '}')
                                    {
                                        ;
                                    }
                                }
                            }
                            break;
                        case '-':
                            retVal.Type = TokenType.UnknownToken;
                            retVal.StrValue = GetSymbolToken('-', out _, out _);
                            break;
                        case '*':
                            retVal.Type = TokenType.Termination;
                            retVal.StrValue = "*";
                            retVal.Size = 1;
                            break;
                        default:
                            if (chr is (>= 'a' and <= 'z') or
                                (>= 'A' and <= 'Z') or
                                (>= '0' and <= '9'))
                            {
                                retVal.StrValue = GetSymbolToken(chr, out bool isAllDigit, out bool isSlashFound);
                                retVal.Size = (int)(CurrentPosition - retVal.StartPos - 1);
                                if (isAllDigit)
                                {
                                    retVal.Type = TokenType.Integer;
                                    retVal.IntValue = int.Parse(retVal.StrValue);
                                }
                                else
                                {
                                    switch (retVal.StrValue)
                                    {
                                        case "0-1":
                                        case "1-0":
                                        case "1/2-1/2":
                                            retVal.Type = TokenType.Termination;
                                            break;
                                        default:
                                            if (isSlashFound)
                                            {
                                                throw new PgnParserException("'/' character found at an unexpected location.");
                                            }
                                            retVal.Type = TokenType.Symbol;
                                            break;
                                    }
                                }
                            }
                            else
                            {
                                throw new PgnParserException("Unknown token character '" + chr + "'");
                            }
                            break;
                    }
                } while (isComment);
            }
            return retVal;
        }

        /// <summary>
        /// Assume the specified token
        /// </summary>
        /// <param name="tokType"> Token type</param>
        /// <param name="tok">     Assumed token</param>
        /// <returns>
        /// Token
        /// </returns>
        public void AssumeToken(TokenType tokType, Token tok)
        {
            if (tok.Type != tokType)
            {
                throw new PgnParserException($"Expecing a token of type - {tokType}", GetStringAtPos(tok.StartPos, tok.Size));
            }
        }

        /// <summary>
        /// Assume the specified token
        /// </summary>
        /// <param name="tokType">  Token type</param>
        /// <returns>
        /// Token
        /// </returns>
        public Token AssumeToken(TokenType tokType)
        {
            Token retVal;

            retVal = GetNextToken();
            AssumeToken(tokType, retVal);
            return retVal;
        }

        /// <summary>
        /// Push back a token
        /// </summary>
        /// <returns>
        /// Token
        /// </returns>
        public void PushToken(Token tok)
        {
            m_tokPushed = !m_tokPushed.HasValue ? (Token?)tok : throw new MethodAccessException("Cannot push two tokens!");
        }

        /// <summary>
        /// Peek a token
        /// </summary>
        /// <returns>
        /// Token
        /// </returns>
        public Token PeekToken()
        {
            Token retVal;

            retVal = GetNextToken();
            PushToken(retVal);
            return retVal;
        }
    }
}
