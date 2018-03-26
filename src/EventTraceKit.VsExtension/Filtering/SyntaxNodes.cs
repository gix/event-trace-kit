namespace EventTraceKit.VsExtension.Filtering
{
    public enum SyntaxKind
    {
        IdentifierNameExpression,
        NumericLiteralExpression,
        StringLiteralExpression,
        GuidLiteralExpression,
        AddExpression,
        SubtractExpression,
        MultiplyExpression,
        DivideExpression,
        ModuloExpression,
        EqualExpression,
        NotEqualExpression,
        BitwiseAndExpression,
        BitwiseOrExpression,
        ExclusiveOrExpression,
        LogicalAndExpression,
        LogicalOrExpression,
        LeftShiftExpression,
        RightShiftExpression,
        LessThanExpression,
        LessThanOrEqualExpression,
        GreaterThanExpression,
        GreaterThanOrEqualExpression,
        UnaryPlusExpression,
        UnaryMinusExpression,
        LogicalNotExpression
    }

    public abstract class FilterSyntaxNode
    {
        protected FilterSyntaxNode(SyntaxKind kind)
        {
            Kind = kind;
        }

        public SyntaxKind Kind { get; }

        public abstract void Accept(FilterSyntaxVisitor visitor);
        public abstract TResult Accept<TResult>(FilterSyntaxVisitor<TResult> visitor);
    }

    public abstract class ExpressionSyntax : FilterSyntaxNode
    {
        protected ExpressionSyntax(SyntaxKind kind)
            : base(kind)
        {
        }
    }

    public class BinaryExpressionSyntax : ExpressionSyntax
    {
        public BinaryExpressionSyntax(SyntaxKind kind, ExpressionSyntax left, ExpressionSyntax right)
            : base(kind)
        {
            Left = left;
            Right = right;
        }

        public ExpressionSyntax Left { get; }
        public ExpressionSyntax Right { get; }

        public override void Accept(FilterSyntaxVisitor visitor)
        {
            visitor.VisitBinaryExpression(this);
        }

        public override TResult Accept<TResult>(FilterSyntaxVisitor<TResult> visitor)
        {
            return visitor.VisitBinaryExpression(this);
        }
    }

    public class PrefixUnaryExpressionSyntax : ExpressionSyntax
    {
        public PrefixUnaryExpressionSyntax(SyntaxKind kind, ExpressionSyntax operand)
            : base(kind)
        {
            Operand = operand;
        }

        public ExpressionSyntax Operand { get; }

        public override void Accept(FilterSyntaxVisitor visitor)
        {
            visitor.VisitPrefixUnaryExpression(this);
        }

        public override TResult Accept<TResult>(FilterSyntaxVisitor<TResult> visitor)
        {
            return visitor.VisitPrefixUnaryExpression(this);
        }
    }

    public class LiteralExpressionSyntax : ExpressionSyntax
    {
        public LiteralExpressionSyntax(SyntaxKind kind, string text)
            : base(kind)
        {
            Text = text;
        }

        public string Text { get; }

        public override void Accept(FilterSyntaxVisitor visitor)
        {
            visitor.VisitLiteralExpression(this);
        }

        public override TResult Accept<TResult>(FilterSyntaxVisitor<TResult> visitor)
        {
            return visitor.VisitLiteralExpression(this);
        }
    }

    public class IdentifierNameSyntax : ExpressionSyntax
    {
        private readonly Token identifier;

        public IdentifierNameSyntax(Token identifier)
            : base(SyntaxKind.IdentifierNameExpression)
        {
            this.identifier = identifier;
        }

        public ref readonly Token Identifier => ref identifier;

        public override void Accept(FilterSyntaxVisitor visitor)
        {
            visitor.VisitIdentifierName(this);
        }

        public override TResult Accept<TResult>(FilterSyntaxVisitor<TResult> visitor)
        {
            return visitor.VisitIdentifierName(this);
        }
    }
}
