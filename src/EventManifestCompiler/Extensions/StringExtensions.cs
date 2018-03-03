namespace EventManifestCompiler.Extensions
{
    using System.Globalization;

    internal static class StringExtensions
    {
        public static string ToStringInvariant(this int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }
    }
}
