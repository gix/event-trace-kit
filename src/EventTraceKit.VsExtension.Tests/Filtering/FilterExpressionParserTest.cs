namespace EventTraceKit.VsExtension.Tests.Filtering
{
    using EventTraceKit.VsExtension.Filtering;
    using Xunit;
    using Xunit.Abstractions;

    public class FilterExpressionParserTest
    {
        private readonly ITestOutputHelper output;

        public FilterExpressionParserTest(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Theory]
        [InlineData(" 1 ")]
        [InlineData(" 18446744073709551615 ")] // ulong.MaxValue
        [InlineData(" 18446744073709551616 ")] // not representable
        [InlineData(" 10.25 ")]
        [InlineData(" 10.25e+10 ")]
        [InlineData(" 10.25E+10 ")]
        [InlineData(" 10.25e-10 ")]
        [InlineData(" 10.25E-10 ")]
        public void ParseNumericLiteral(string input)
        {
            var expr = ParseExpression(input);
            Assert.Equal(SyntaxKind.NumericLiteralExpression, expr.Kind);
            var literalExpr = (LiteralExpressionSyntax)expr;
            Assert.Equal(input.Trim(), literalExpr.Text);
        }

        [Theory]
        [InlineData(" \"\" ")]
        [InlineData(" \"f\" ")]
        [InlineData(" \"foo\" ")]
        [InlineData(" \"foo\\\"bar\" ")]
        [InlineData(" \"foo\\tbar\" ")]
        public void ParseStringLiteral(string input)
        {
            var expr = ParseExpression(input);
            Assert.Equal(SyntaxKind.StringLiteralExpression, expr.Kind);
            var literalExpr = (LiteralExpressionSyntax)expr;
            Assert.Equal(input.Trim(), literalExpr.Text);
        }

        [Theory]
        [InlineData(" {F88FDB88-8E1A-406C-A77F-69E72D8DEF8A} ")]
        [InlineData(" {f88fdb88-8e1a-406c-a77f-69e72d8def8a} ")]
        public void ParseGuidLiteral(string input)
        {
            var expr = ParseExpression(input);
            Assert.Equal(SyntaxKind.GuidLiteralExpression, expr.Kind);
            var literalExpr = (LiteralExpressionSyntax)expr;
            Assert.Equal(input.Trim(), literalExpr.Text);
        }

        [Theory]
        [InlineData("+1")]
        [InlineData("+10.25E-10")]
        public void ParseUnaryPlus(string input)
        {
            var expr = ParseExpression(input);
            Assert.Equal(SyntaxKind.UnaryPlusExpression, expr.Kind);
            var unaryExpr = (PrefixUnaryExpressionSyntax)expr;
            Assert.Equal(SyntaxKind.NumericLiteralExpression, unaryExpr.Operand.Kind);
        }

        [Theory]
        [InlineData("-1")]
        [InlineData("-10.25E-10")]
        public void ParseUnaryMinus(string input)
        {
            var expr = ParseExpression(input);
            Assert.Equal(SyntaxKind.UnaryMinusExpression, expr.Kind);
            var unaryExpr = (PrefixUnaryExpressionSyntax)expr;
            Assert.Equal(SyntaxKind.NumericLiteralExpression, unaryExpr.Operand.Kind);
        }

        [Fact]
        public void ParseUnary()
        {
            var expr = ParseExpression("-+1e+1");
            Assert.Equal(SyntaxKind.UnaryMinusExpression, expr.Kind);

            var unaryMinusExpr = (PrefixUnaryExpressionSyntax)expr;
            Assert.Equal(SyntaxKind.UnaryPlusExpression, unaryMinusExpr.Operand.Kind);

            var unaryPlusExpr = (PrefixUnaryExpressionSyntax)unaryMinusExpr.Operand;
            Assert.Equal(SyntaxKind.NumericLiteralExpression, unaryPlusExpr.Operand.Kind);

            Assert.Equal("1e+1", ((LiteralExpressionSyntax)unaryPlusExpr.Operand).Text);
        }

        [Theory]
        [InlineData("!1")]
        [InlineData("!10.25E-10")]
        public void ParseLogicalNot(string input)
        {
            var expr = ParseExpression(input);
            Assert.Equal(SyntaxKind.LogicalNotExpression, expr.Kind);
            var unaryExpr = (PrefixUnaryExpressionSyntax)expr;
            Assert.Equal(SyntaxKind.NumericLiteralExpression, unaryExpr.Operand.Kind);
        }

        [Theory]
        [InlineData("v+2", SyntaxKind.AddExpression)]
        [InlineData("v-2", SyntaxKind.SubtractExpression)]
        [InlineData("v*2", SyntaxKind.MultiplyExpression)]
        [InlineData("v/2", SyntaxKind.DivideExpression)]
        [InlineData("v%2", SyntaxKind.ModuloExpression)]
        [InlineData("v<<2", SyntaxKind.LeftShiftExpression)]
        [InlineData("v>>2", SyntaxKind.RightShiftExpression)]
        [InlineData("v&2", SyntaxKind.BitwiseAndExpression)]
        [InlineData("v|2", SyntaxKind.BitwiseOrExpression)]
        [InlineData("v^2", SyntaxKind.ExclusiveOrExpression)]
        [InlineData("v==2", SyntaxKind.EqualExpression)]
        [InlineData("v!=2", SyntaxKind.NotEqualExpression)]
        [InlineData("v<2", SyntaxKind.LessThanExpression)]
        [InlineData("v<=2", SyntaxKind.LessThanOrEqualExpression)]
        [InlineData("v>2", SyntaxKind.GreaterThanExpression)]
        [InlineData("v>=2", SyntaxKind.GreaterThanOrEqualExpression)]
        [InlineData("v&&2", SyntaxKind.LogicalAndExpression)]
        [InlineData("v||2", SyntaxKind.LogicalOrExpression)]
        public void ParseBinary(string input, SyntaxKind expectedKind)
        {
            var expr = ParseExpression(input);
            Assert.Equal(expectedKind, expr.Kind);
            var binaryExpr = (BinaryExpressionSyntax)expr;
            Assert.Equal(SyntaxKind.IdentifierNameExpression, binaryExpr.Left.Kind);
            Assert.Equal(SyntaxKind.NumericLiteralExpression, binaryExpr.Right.Kind);
        }

        [Fact]
        public void Precedence1()
        {
            var expr = ParseExpression("1+2*-3");
            Assert.Equal(SyntaxKind.AddExpression, expr.Kind);

            var addExpr = (BinaryExpressionSyntax)expr;
            Assert.Equal(SyntaxKind.NumericLiteralExpression, addExpr.Left.Kind);
            Assert.Equal(SyntaxKind.MultiplyExpression, addExpr.Right.Kind);

            var multExpr = (BinaryExpressionSyntax)addExpr.Right;
            Assert.Equal(SyntaxKind.NumericLiteralExpression, multExpr.Left.Kind);
            Assert.Equal(SyntaxKind.UnaryMinusExpression, multExpr.Right.Kind);

            var unaryExpr = (PrefixUnaryExpressionSyntax)multExpr.Right;
            Assert.Equal(SyntaxKind.NumericLiteralExpression, unaryExpr.Operand.Kind);

            Assert.Equal("1", ((LiteralExpressionSyntax)addExpr.Left).Text);
            Assert.Equal("2", ((LiteralExpressionSyntax)multExpr.Left).Text);
            Assert.Equal("3", ((LiteralExpressionSyntax)unaryExpr.Operand).Text);
        }

        [Fact]
        public void Precedence2()
        {
            var expr = ParseExpression("(-+1.5e2+2)*3");
            Assert.Equal(SyntaxKind.MultiplyExpression, expr.Kind);

            var multExpr = (BinaryExpressionSyntax)expr;
            Assert.Equal(SyntaxKind.AddExpression, multExpr.Left.Kind);
            Assert.Equal(SyntaxKind.NumericLiteralExpression, multExpr.Right.Kind);

            var addExpr = (BinaryExpressionSyntax)multExpr.Left;
            Assert.Equal(SyntaxKind.UnaryMinusExpression, addExpr.Left.Kind);
            Assert.Equal(SyntaxKind.NumericLiteralExpression, addExpr.Right.Kind);

            var unaryMinusExpr = (PrefixUnaryExpressionSyntax)addExpr.Left;
            Assert.Equal(SyntaxKind.UnaryPlusExpression, unaryMinusExpr.Operand.Kind);

            var unaryPlusExpr = (PrefixUnaryExpressionSyntax)unaryMinusExpr.Operand;
            Assert.Equal(SyntaxKind.NumericLiteralExpression, unaryPlusExpr.Operand.Kind);

            Assert.Equal("1.5e2", ((LiteralExpressionSyntax)unaryPlusExpr.Operand).Text);
            Assert.Equal("2", ((LiteralExpressionSyntax)addExpr.Right).Text);
            Assert.Equal("3", ((LiteralExpressionSyntax)multExpr.Right).Text);
        }

        [Fact]
        public void Precedence3()
        {
            var expr = ParseExpression("!var == ((4))");
            Assert.Equal(SyntaxKind.EqualExpression, expr.Kind);

            var equalExpr = (BinaryExpressionSyntax)expr;
            Assert.Equal(SyntaxKind.LogicalNotExpression, equalExpr.Left.Kind);
            Assert.Equal(SyntaxKind.NumericLiteralExpression, equalExpr.Right.Kind);

            var unaryMinusExpr = (PrefixUnaryExpressionSyntax)equalExpr.Left;
            Assert.Equal(SyntaxKind.IdentifierNameExpression, unaryMinusExpr.Operand.Kind);

            Assert.Equal("var", ((IdentifierNameSyntax)unaryMinusExpr.Operand).Identifier.GetText());
            Assert.Equal("4", ((LiteralExpressionSyntax)equalExpr.Right).Text);
        }

        [Theory]
        [InlineData("+", SyntaxKind.AddExpression)]
        [InlineData("-", SyntaxKind.SubtractExpression)]
        [InlineData("*", SyntaxKind.MultiplyExpression)]
        [InlineData("/", SyntaxKind.DivideExpression)]
        [InlineData("%", SyntaxKind.ModuloExpression)]
        [InlineData("<<", SyntaxKind.LeftShiftExpression)]
        [InlineData(">>", SyntaxKind.RightShiftExpression)]
        [InlineData("&", SyntaxKind.BitwiseAndExpression)]
        [InlineData("|", SyntaxKind.BitwiseOrExpression)]
        [InlineData("^", SyntaxKind.ExclusiveOrExpression)]
        [InlineData("==", SyntaxKind.EqualExpression)]
        [InlineData("!=", SyntaxKind.NotEqualExpression)]
        [InlineData("<", SyntaxKind.LessThanExpression)]
        [InlineData("<=", SyntaxKind.LessThanOrEqualExpression)]
        [InlineData(">", SyntaxKind.GreaterThanExpression)]
        [InlineData(">=", SyntaxKind.GreaterThanOrEqualExpression)]
        [InlineData("&&", SyntaxKind.LogicalAndExpression)]
        [InlineData("||", SyntaxKind.LogicalOrExpression)]
        public void Associativity(string op, SyntaxKind binExprKind)
        {
            var expr = ParseExpression(string.Join(op, "a", "b", "c", "d"));
            Assert.Equal(binExprKind, expr.Kind);

            var binExpr = (BinaryExpressionSyntax)expr;
            Assert.Equal(binExprKind, binExpr.Left.Kind);
            Assert.Equal(SyntaxKind.IdentifierNameExpression, binExpr.Right.Kind);

            var binExpr2 = (BinaryExpressionSyntax)binExpr.Left;
            Assert.Equal(binExprKind, binExpr2.Left.Kind);
            Assert.Equal(SyntaxKind.IdentifierNameExpression, binExpr2.Right.Kind);

            var binExpr3 = (BinaryExpressionSyntax)binExpr2.Left;
            Assert.Equal(SyntaxKind.IdentifierNameExpression, binExpr3.Left.Kind);
            Assert.Equal(SyntaxKind.IdentifierNameExpression, binExpr3.Right.Kind);

            Assert.Equal("a", ((IdentifierNameSyntax)binExpr3.Left).Identifier.GetText());
            Assert.Equal("b", ((IdentifierNameSyntax)binExpr3.Right).Identifier.GetText());
            Assert.Equal("c", ((IdentifierNameSyntax)binExpr2.Right).Identifier.GetText());
            Assert.Equal("d", ((IdentifierNameSyntax)binExpr.Right).Identifier.GetText());
        }

        private ExpressionSyntax ParseExpression(string input)
        {
            return FilterSyntaxFactory.ParseExpression(input);
        }
    }
}
