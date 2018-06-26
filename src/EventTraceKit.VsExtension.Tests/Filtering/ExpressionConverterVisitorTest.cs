namespace EventTraceKit.VsExtension.Tests.Filtering
{
    using System;
    using System.Linq.Expressions;
    using EventTraceKit.VsExtension.Filtering;
    using Xunit;
    using Xunit.Abstractions;

    public class ExpressionConverterVisitorTest
    {
        private readonly ITestOutputHelper output;

        public ExpressionConverterVisitorTest(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Theory]
        [InlineData("Id == 2")]
        [InlineData("Id != 2")]
        [InlineData("ProviderId != \"84104AF4-C35A-436D-9661-D7DC96AD4069\" || (Id >= 2 && Id <= 20)")]
        [InlineData("Version == Level")]
        [InlineData("Id == 0x12")]
        public void Convert(string input)
        {
            var visitor = new ExpressionFactoryVisitor();
            var expr = visitor.Visit(FilterSyntaxFactory.ParseExpression(input));
            Assert.NotNull(expr);
        }

        [Fact]
        public void HexLiteral()
        {
            var visitor = new ExpressionFactoryVisitor();
            var expr = visitor.Visit(FilterSyntaxFactory.ParseExpression("Id == 0x12"));
            Assert.NotNull(expr);
            Assert.Equal(ExpressionType.Equal, expr.NodeType);
            var binExpr = (BinaryExpression)expr;
            Assert.Equal(TraceLogFilterBuilder.Instance.Id, binExpr.Left);
            var valueExpr = Assert.IsType<ConstantExpression>(binExpr.Right);
            Assert.Equal((ushort)0x12, valueExpr.Value);
        }

        [Fact]
        public void Mask()
        {
            var expr = ExpressionFactoryVisitor.Convert(FilterSyntaxFactory.ParseExpression("(Id & 0x12) != 0"));
            Assert.NotNull(expr);
            Assert.Equal(ExpressionType.NotEqual, expr.NodeType);
            var cmpExpr = (BinaryExpression)expr;
            var zeroExpr = Assert.IsType<ConstantExpression>(cmpExpr.Right);
            Assert.Equal((ushort)0, zeroExpr.Value);
            Assert.Equal(ExpressionType.And, cmpExpr.Left.NodeType);
            var andExpr = (BinaryExpression)cmpExpr.Left;
            Assert.Equal(TraceLogFilterBuilder.Instance.Id, andExpr.Left);
            var valueExpr = Assert.IsType<ConstantExpression>(andExpr.Right);
            Assert.Equal((ushort)0x12, valueExpr.Value);
            Assert.Equal(typeof(bool), expr.Type);
        }

        [Theory]
        [InlineData("Id == Level")]
        [InlineData("Level == Id")]
        [InlineData("Id != Level")]
        [InlineData("Id < Level")]
        [InlineData("Id <= Level")]
        [InlineData("Id > Level")]
        [InlineData("Id >= Level")]
        [InlineData("Id + Level")]
        [InlineData("Id - Level")]
        [InlineData("Id * Level")]
        [InlineData("Id / Level")]
        [InlineData("Id % Level")]
        [InlineData("Id >> Level")]
        [InlineData("Id << Level")]
        [InlineData("Id == Keyword")]
        [InlineData("Id == 1234567")]
        [InlineData("Id != 1234567")]
        [InlineData("Id < 1234567")]
        [InlineData("Id <= 1234567")]
        [InlineData("Id > 1234567")]
        [InlineData("Id >= 1234567")]
        public void PromotesBinaryOperations(string input)
        {
            var visitor = new ExpressionFactoryVisitor();
            var expr = visitor.Visit(FilterSyntaxFactory.ParseExpression(input));
            Assert.NotNull(expr);
        }

        [Theory]
        [InlineData("Id == ProviderId")]
        public void ForbidsIncompatibleOperands(string input)
        {
            var visitor = new ExpressionFactoryVisitor();
            var syntaxNode = FilterSyntaxFactory.ParseExpression(input);

            var ex = Assert.Throws<ArgumentException>(() => visitor.Visit(syntaxNode));
            Assert.Contains("cannot compare incompatible operand types", ex.Message, StringComparison.InvariantCultureIgnoreCase);
        }

        [Fact]
        public void NoImplicitConversionToBool()
        {
            var syntaxNode = FilterSyntaxFactory.ParseExpression("Id & 1");

            var ex = Assert.Throws<InvalidOperationException>(() => ExpressionFactoryVisitor.Convert(syntaxNode));
            Assert.Contains("expression is not boolean", ex.Message, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
