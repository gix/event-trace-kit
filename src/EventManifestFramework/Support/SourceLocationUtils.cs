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

        public static SourceLocation GetValueLocation(this XAttribute attribute)
        {
            // Disable advanced location for now because the schema validation
            // only reports the location of the attribute name.
            return attribute.GetLocation();

            var lineInfo = (IXmlLineInfo)attribute;
            if (!lineInfo.HasLineInfo())
                return new SourceLocation();

            var linePosition = lineInfo.LinePosition;
            if (!string.IsNullOrEmpty(attribute.Name.NamespaceName))
                linePosition += attribute.Name.NamespaceName.Length + 1/*:*/;
            linePosition += attribute.Name.LocalName.Length + 2/*="*/;

            if (string.IsNullOrEmpty(attribute.BaseUri))
                return new SourceLocation(lineInfo.LineNumber, linePosition);
            return new SourceLocation(attribute.BaseUri, lineInfo.LineNumber, linePosition);
        }
    }
}
