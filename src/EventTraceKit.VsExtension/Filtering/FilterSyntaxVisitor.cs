namespace EventTraceKit.VsExtension.Filtering
{
    public abstract class FilterSyntaxVisitor
    {
        public virtual void Visit(FilterSyntaxNode node)
        {
            node?.Accept(this);
        }

        public virtual void DefaultVisit(FilterSyntaxNode node)
        {
        }

        public virtual void VisitLiteralExpression(LiteralExpressionSyntax expr)
        {
            DefaultVisit(expr);
        }

        public virtual void VisitIdentifierName(IdentifierNameSyntax expr)
        {
            DefaultVisit(expr);
        }

        public virtual void VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax expr)
        {
            DefaultVisit(expr);
        }

        public virtual void VisitBinaryExpression(BinaryExpressionSyntax expr)
        {
            DefaultVisit(expr);
        }
    }

    public abstract class FilterSyntaxVisitor<TResult>
    {
        public virtual TResult Visit(FilterSyntaxNode node)
        {
            if (node != null)
                return node.Accept(this);
            return default;
        }

        public virtual TResult DefaultVisit(FilterSyntaxNode node)
        {
            return default;
        }

        public virtual TResult VisitLiteralExpression(LiteralExpressionSyntax expr)
        {
            return DefaultVisit(expr);
        }

        public virtual TResult VisitIdentifierName(IdentifierNameSyntax expr)
        {
            return DefaultVisit(expr);
        }

        public virtual TResult VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax expr)
        {
            return DefaultVisit(expr);
        }

        public virtual TResult VisitBinaryExpression(BinaryExpressionSyntax expr)
        {
            return DefaultVisit(expr);
        }
    }
}
