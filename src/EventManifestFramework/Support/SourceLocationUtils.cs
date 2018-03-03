namespace EventManifestFramework.Support
{
    using System.Xml;
    using System.Xml.Linq;

    public static class SourceLocationUtils
    {
        public static SourceLocation GetLocation(this XObject obj)
        {
            var lineInfo = (IXmlLineInfo)obj;
            if (!lineInfo.HasLineInfo())
                return new SourceLocation();

            if (string.IsNullOrEmpty(obj.BaseUri))
                return new SourceLocation(lineInfo.LineNumber, lineInfo.LinePosition);
            return new SourceLocation(obj.BaseUri, lineInfo.LineNumber, lineInfo.LinePosition);
        }
    }
}
