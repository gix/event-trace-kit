namespace EventTraceKit.VsExtension.Tests.Filtering
{
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
        public void Convert(string input)
        {
            var visitor = new ExpressionFactoryVisitor();
            var expr = visitor.Visit(FilterSyntaxFactory.ParseExpression(input));
            output.WriteLine("{0}: {1}", input, expr);
        }

        [Theory]
        [InlineData("Id != 123456")]
        public void Convert2(string input)
        {
            var visitor = new ExpressionFactoryVisitor();
            var expr = visitor.Visit(FilterSyntaxFactory.ParseExpression(input));
            output.WriteLine("{0}: {1}", input, expr);
        }
    }
}
