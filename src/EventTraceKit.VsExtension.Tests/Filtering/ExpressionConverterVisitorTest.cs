namespace EventTraceKit.VsExtension.Tests.Filtering
{
    using System;
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
        public void Convert(string input)
        {
            var visitor = new ExpressionFactoryVisitor();
            var expr = visitor.Visit(FilterSyntaxFactory.ParseExpression(input));
            Assert.NotNull(expr);
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
    }
}
