namespace EventTraceKit.EventTracing.Tests.Compilation.BinXml
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using EventTraceKit.EventTracing.Compilation.BinXml;
    using Xunit;

    public class BinXmlReaderTest
    {
        [Fact]
        public void DisposeLeavesStreamOpen()
        {
            var substitutionTypes = new List<BinXmlType>();
            var input = new MemoryStream();
            var reader = new BinXmlReader(input, substitutionTypes);

            reader.Dispose();

            Assert.True(input.CanRead);
            input.Dispose();
            Assert.False(input.CanRead);
        }

        [Fact]
        public void ReadSimple()
        {
            var substitutionTypes = new List<BinXmlType>();
            var input = CreateInput(new byte[] {
                // FragmentHeader
                Token.FragmentHeaderToken,
                Constants.MajorVersion,
                Constants.MinorVersion,
                0, /*flags*/

                Token.EndOfFragmentToken
            });

            var doc = BinXmlReader.Read(input, substitutionTypes);
            Assert.Equal("", doc.ToString());
        }

        [Fact]
        public void FragmentHeader_InvalidToken()
        {
            var substitutionTypes = new List<BinXmlType>();
            var input = CreateInput(new byte[] {
                // FragmentHeader
                0,
                Constants.MajorVersion,
                Constants.MinorVersion,
                0, /*flags*/

                Token.EndOfFragmentToken
            });

            var ex = Assert.Throws<InvalidOperationException>(
                () => BinXmlReader.Read(input, substitutionTypes));
            Assert.Contains("Unexpected token", ex.Message);
        }

        [Theory]
        [InlineData(2, 1)] // Major higher
        [InlineData(1, 2)] // Minor higher
        [InlineData(0, 1)] // Major lower
        [InlineData(1, 0)] // Minor lower
        public void FragmentHeader_UnsupportedVersion(byte major, byte minor)
        {
            var substitutionTypes = new List<BinXmlType>();
            var input = CreateInput(new byte[] {
                // FragmentHeader
                Token.FragmentHeaderToken,
                major,
                minor,
                0, /*flags*/

                Token.EndOfFragmentToken
            });

            var ex = Assert.Throws<InvalidOperationException>(
                () => BinXmlReader.Read(input, substitutionTypes));
            Assert.Contains("Unsupported version", ex.Message);
        }

        [Fact]
        public void FragmentHeader_UnsupportedFlags()
        {
            var substitutionTypes = new List<BinXmlType>();
            var input = CreateInput(new byte[] {
                // FragmentHeader
                Token.FragmentHeaderToken,
                Constants.MajorVersion,
                Constants.MinorVersion,
                1, /*flags*/

                Token.EndOfFragmentToken
            });

            var ex = Assert.Throws<InvalidOperationException>(
                () => BinXmlReader.Read(input, substitutionTypes));
            Assert.Contains("Unsupported flags", ex.Message);
        }

        private static Stream CreateInput(byte[] input = null)
        {
            return new MemoryStream(input ?? new byte[0]);
        }
    }
}
