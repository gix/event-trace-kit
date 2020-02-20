namespace EventTraceKit.EventTracing.Support
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

        public static SourceLocation GetValueLocation(this XAttribute attribute)
        {
            // No advanced location for now because the schema validation only
            // reports the location of the attribute name.
            return attribute.GetLocation();
        }
    }
}
