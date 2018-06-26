namespace EventTraceKit.VsExtension.Filtering
{
    public class FilterExpressionLexer
    {
        private const char EndOfFile = TextBuffer.InvalidCharacter;

        private readonly TextBuffer buffer;

        public FilterExpressionLexer(TextBuffer buffer)
        {
            this.buffer = buffer;
        }

        public bool Lex(ref Token result)
        {
            result.Reset();
            return LexToken(ref result);
        }

        private bool LexToken(ref Token result)
        {
            LexNextToken:
            int curPos = buffer.Position;

            if (buffer[curPos] == ' ' || buffer[curPos] == '\t') {
                ++curPos;
                while (buffer[curPos] == ' ' || buffer[curPos] == '\t')
                    ++curPos;

                buffer.Position = curPos;
            }

            TokenKind kind;
            switch (GetAndAdvanceChar(ref curPos)) {
                case EndOfFile:
                    return false;

                case '\r':
                    if (buffer[curPos] == '\n')
                        ++curPos;
                    goto case '\n';

                case '\n':
                case '\x2028':
                case '\x2029':
                    buffer.Position = curPos;
                    goto LexNextToken;

                // Number
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                    return LexNumericConstant(ref result, curPos);

                // Identifier
                case 'A':
                case 'B':
                case 'C':
                case 'D':
                case 'E':
                case 'F':
                case 'G':
                case 'H':
                case 'I':
                case 'J':
                case 'K':
                case 'L':
                case 'M':
                case 'N':
                case 'O':
                case 'P':
                case 'Q':
                case 'R':
                case 'S':
                case 'T':
                case 'U':
                case 'V':
                case 'W':
                case 'X':
                case 'Y':
                case 'Z':
                case 'a':
                case 'b':
                case 'c':
                case 'd':
                case 'e':
                case 'f':
                case 'g':
                case 'h':
                case 'i':
                case 'j':
                case 'k':
                case 'l':
                case 'm':
                case 'n':
                case 'o':
                case 'p':
                case 'q':
                case 'r':
                case 's':
                case 't':
                case 'u':
                case 'v':
                case 'w':
                case 'x':
                case 'y':
                case 'z':
                    return LexIdentifier(ref result, curPos);

                // Strings
                case '"':
                    return LexStringLiteral(ref result, curPos);

                // Guid
                case '{':
                    return LexGuidLiteral(ref result, curPos);

                // Special tokens
                case '&':
                    if (buffer[curPos] == '&') {
                        kind = TokenKind.AmpAmp;
                        ++curPos;
                    } else {
                        kind = TokenKind.Amp;
                    }
                    break;

                case '|':
                    if (buffer[curPos] == '|') {
                        kind = TokenKind.PipePipe;
                        ++curPos;
                    } else {
                        kind = TokenKind.Pipe;
                    }
                    break;
                case '(':
                    kind = TokenKind.LParen;
                    break;
                case ')':
                    kind = TokenKind.RParen;
                    break;
                case '<':
                    if (buffer[curPos] == '=') {
                        kind = TokenKind.LessEqual;
                        ++curPos;
                    } else if (buffer[curPos] == '<') {
                        kind = TokenKind.LessLess;
                        ++curPos;
                    } else {
                        kind = TokenKind.Less;
                    }
                    break;
                case '>':
                    if (buffer[curPos] == '=') {
                        kind = TokenKind.GreaterEqual;
                        ++curPos;
                    } else if (buffer[curPos] == '>') {
                        kind = TokenKind.GreaterGreater;
                        ++curPos;
                    } else {
                        kind = TokenKind.Greater;
                    }
                    break;
                case '?':
                    kind = TokenKind.Question;
                    break;
                case '!':
                    if (buffer[curPos] == '=') {
                        kind = TokenKind.ExclaimEqual;
                        ++curPos;
                    } else {
                        kind = TokenKind.Exclaim;
                    }
                    break;
                case '*':
                    kind = TokenKind.Star;
                    break;
                case '/':
                    kind = TokenKind.Slash;
                    break;
                case '+':
                    kind = TokenKind.Plus;
                    break;
                case '-':
                    kind = TokenKind.Minus;
                    break;
                case ':':
                    kind = TokenKind.Colon;
                    break;
                case '%':
                    kind = TokenKind.Percent;
                    break;

                case '=':
                    if (buffer[curPos] == '=') {
                        kind = TokenKind.EqualEqual;
                        ++curPos;
                    } else {
                        goto default;
                    }
                    break;

                case '^':
                    kind = TokenKind.Caret;
                    break;

                default:
                    kind = TokenKind.Unknown;
                    break;
            }

            FormToken(ref result, curPos, kind);
            return true;
        }

        private bool LexNumericConstant(ref Token result, int curPos)
        {
            while (IsNumberBody(buffer[curPos]))
                ++curPos;

            if (buffer[curPos] == 'E' || buffer[curPos] == 'e') {
                ++curPos;
                if (buffer[curPos] == '-' || buffer[curPos] == '+')
                    ++curPos;

                return LexNumericConstant(ref result, curPos);
            }

            int tokenStart = buffer.Position;
            FormToken(ref result, curPos, TokenKind.NumericConstant);
            result.SetLiteralData(buffer, tokenStart);
            return true;
        }

        private bool LexIdentifier(ref Token result, int curPos)
        {
            while (IsIdentifierBody(buffer[curPos]))
                ++curPos;

            int tokenStart = buffer.Position;
            FormToken(ref result, curPos, TokenKind.Identifier);
            result.SetLiteralData(buffer, tokenStart);
            result.Kind = TokenKind.Identifier;
            return true;
        }

        private bool LexStringLiteral(ref Token result, int curPos)
        {
            char c = GetAndAdvanceChar(ref curPos);
            while (c != '"') {
                if (c == '\\')
                    c = GetAndAdvanceChar(ref curPos);

                if (IsLineTerminator(c) || c == EndOfFile) {
                    --curPos;
                    break;
                }

                c = GetAndAdvanceChar(ref curPos);
            }

            int tokenStart = buffer.Position;
            FormToken(ref result, curPos, TokenKind.StringLiteral);
            result.SetLiteralData(buffer, tokenStart);
            return true;
        }

        private bool LexGuidLiteral(ref Token result, int curPos)
        {
            while (IsGuidBody(buffer[curPos]))
                ++curPos;

            var kind = TokenKind.GuidLiteral;
            if (buffer[curPos] != '}')
                kind = TokenKind.Unknown;
            else
                ++curPos;

            int tokenStart = buffer.Position;
            FormToken(ref result, curPos, kind);
            result.SetLiteralData(buffer, tokenStart);
            return true;
        }

        private char GetAndAdvanceChar(ref int curPos)
        {
            char c = buffer[curPos];
            ++curPos;
            return c;
        }

        private void FormToken(ref Token result, int tokenEnd, TokenKind kind)
        {
            int length = tokenEnd - buffer.Position;
            result.Length = length;
            result.Location = new SourceLocation(buffer.Position);
            result.Kind = kind;
            buffer.Position = tokenEnd;
        }

        private static bool IsIdentifierBody(char c)
        {
            return
                (c >= 'A' && c <= 'Z') ||
                (c >= 'a' && c <= 'z') ||
                (c >= '0' && c <= '9') ||
                c == '_'
                ;
        }

        private static bool IsNumberBody(char c)
        {
            return c >= '0' && c <= '9' || c == '.' || c == 'x';
        }

        private static bool IsGuidBody(char c)
        {
            return
                (c >= 'A' && c <= 'F') ||
                (c >= 'a' && c <= 'f') ||
                (c >= '0' && c <= '9') ||
                c == '-';
        }

        private static bool IsLineTerminator(char c)
        {
            return
                c == '\x000D' ||
                c == '\x000A' ||
                c == '\x2028' ||
                c == '\x2029'
                ;
        }
    }
}
