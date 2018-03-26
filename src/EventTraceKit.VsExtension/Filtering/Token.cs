namespace EventTraceKit.VsExtension.Filtering
{
    using System;
    using System.Diagnostics;

    [Flags]
    public enum TokenFlags
    {
        None = 0,
        Unary = 1
    }

    public struct SourceLocation
    {
        public SourceLocation(int id)
        {
            Id = id;
        }

        public int Id { get; }
    }

    public enum TokenKind
    {
        Unknown,
        LParen,
        RParen,
        Less,
        LessLess,
        LessEqual,
        Greater,
        GreaterGreater,
        GreaterEqual,
        Question,
        Exclaim,
        ExclaimEqual,
        Amp,
        AmpAmp,
        Pipe,
        PipePipe,
        Plus,
        Minus,
        Percent,
        Star,
        Slash,
        Colon,
        EqualEqual,
        Caret,
        NumericConstant,
        StringLiteral,
        GuidLiteral,
        Identifier,
    }

    [DebuggerDisplay("{" + nameof(Kind) + "}")]
    public struct Token
    {
        private TextBuffer buffer;
        private int literalOffset;

        public TokenKind Kind { get; set; }
        public TokenFlags Flags { get; set; }
        public int Length { get; set; }
        public SourceLocation Location { get; set; }

        public string GetText()
        {
            return buffer?.GetText(literalOffset, Length);
        }

        public string GetUnquotedText()
        {
            if (Length < 2)
                return null;
            return buffer?.GetText(literalOffset + 1, Length - 2);
        }

        public bool Is(TokenKind kind)
        {
            return Kind == kind;
        }

        public bool IsOneOf(TokenKind kind1, TokenKind kind2)
        {
            return Kind == kind1 || Kind == kind2;
        }

        public void Reset()
        {
            Kind = TokenKind.Unknown;
            Flags = 0;
            Length = 0;
            literalOffset = 0;
            buffer = null;
        }

        public void SetLiteralData(TextBuffer buffer, int offset)
        {
            this.buffer = buffer;
            literalOffset = offset;
        }
    }
}
