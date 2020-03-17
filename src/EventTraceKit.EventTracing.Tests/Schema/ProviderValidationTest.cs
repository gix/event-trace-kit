namespace EventTraceKit.EventTracing.Tests.Schema
{
    using System.Xml.Linq;
    using EventTraceKit.EventTracing.Support;
    using Xunit;

    public class ProviderValidationTest : ValidationTest
    {
        [Theory]
        [InlineData("P1.646E035B-4651-4D15-BC40-1897CC99E967", "{646E035B-4651-4D15-BC40-1897CC99E967}")]
        [InlineData("P2.69C67B48-2C50-4D58-AEC3-5DF3261C2B86", "{69C67B48-2C50-4D58-AEC3-5DF3261C2B86}")]
        [InlineData("P3.646E035B46514D15BC401897CC99E967", "{646E035B-4651-4D15-BC40-1897CC99E967}")]
        [InlineData("P4.(646E035B-4651-4D15-BC40-1897CC99E967)", "{646E035B-4651-4D15-BC40-1897CC99E967}")]
        [InlineData("P5.{646E035B-4651-4D15-BC40-1897CC99E967}", "{646E035B-4651-4D15-BC40-1897CC99E967}")]
        [InlineData("P5.6__46^E%0.,35B46514$D15BC?4018/97CC9@9E967~", "{646E035B-4651-4D15-BC40-1897CC99E967}")]
        public void Name_EndsWithGuid_WhenControlGuidSpecified(string providerName, string guid)
        {
            var provider = CreateProvider();
            provider.SetAttributeValue("name", providerName);
            provider.SetAttributeValue("guid", guid);
            provider.SetAttributeValue("controlGuid", "{35919CDE-6E0D-458F-84C1-D8930E18AC57}");

            ParseInput(ref provider);

            Assert.Empty(diags.Errors);
        }

        [Theory]
        [InlineData("P1", "{646E035B-4651-4D15-BC40-1897CC99E967}")]
        [InlineData("646E035B-4651-4D15-BC40-1897CC99E967.P1", "{646E035B-4651-4D15-BC40-1897CC99E967}")]
        [InlineData("P1.35919CDE-6E0D-458F-84C1-D8930E18AC57", "{646E035B-4651-4D15-BC40-1897CC99E967}")]
        public void Name_MustEndWithProviderId_WhenControlGuidSpecified(string providerName, string guid)
        {
            var provider = CreateProvider();
            provider.SetAttributeValue("name", providerName);
            provider.SetAttributeValue("guid", guid);
            provider.SetAttributeValue("controlGuid", "{35919CDE-6E0D-458F-84C1-D8930E18AC57}");

            ParseInput(ref provider);

            Assert.Single(diags.Errors);
            Assert.Contains("provider name must end with the provider guid", diags.Errors[0].FormattedMessage);
            Assert.Equal(provider.Attribute("name").GetLocation(), diags.Errors[0].Location);
        }

        [Fact]
        public void ResourceFileName_Required()
        {
            var provider = CreateProvider();
            provider.SetAttributeValue("resourceFileName", null);

            ParseInput(ref provider);

            Assert.Single(diags.Errors);
            Assert.Contains("resourceFileName", diags.Errors[0].FormattedMessage);
            Assert.Equal(provider.GetLocation(), diags.Errors[0].Location);
        }

        [Fact]
        public void MessageFileName_Required()
        {
            var provider = CreateProvider();
            provider.SetAttributeValue("messageFileName", null);

            ParseInput(ref provider);

            Assert.Single(diags.Errors);
            Assert.Contains("messageFileName", diags.Errors[0].FormattedMessage);
            Assert.Equal(provider.GetLocation(), diags.Errors[0].Location);
        }

        private void ParseInput(ref XElement provider)
        {
            parser.ParseManifest(CreateInput(ref provider), "<stdin>");
        }
    }
}
