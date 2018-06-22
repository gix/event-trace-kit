namespace EventTraceKit.VsExtension.Filtering
{
    public static class FilterSyntaxFactory
    {
        public static ExpressionSyntax ParseExpression(string text)
        {
            var buffer = TextBuffer.FromString(text);
            var lexer = new FilterExpressionLexer(buffer);
            var parser = new FilterExpressionParser(lexer);
            return parser.ParseExpression();
        }
    }
}
