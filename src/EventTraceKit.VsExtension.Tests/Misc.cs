namespace EventTraceKit.VsExtension.Tests
{
    using System;
    using EventTraceKit.VsExtension.Views;
    using Xunit;
    using Xunit.Abstractions;

    public class Misc
    {
        private readonly ITestOutputHelper output;

        public Misc(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact(Skip = "Manual")]
        public void ReportException()
        {
            ErrorUtils.ReportException(null, new Exception("Exception message"), "Custom message");
        }
    }
}
