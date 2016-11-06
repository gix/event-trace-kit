namespace EventTraceKit.VsExtension.Tests
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;

    public static class TestExtensions
    {
        public static IEnumerable<XAttribute> NonXmlnsAttributes(this XElement element)
        {
            return element.Attributes().Where(x => !IsXmlns(x.Name));
        }

        public static bool IsXmlns(this XName name)
        {
            return name.LocalName == "xmlns" || name.Namespace == XNamespace.Xmlns;
        }

        public static string ReadFullyAsString(this MemoryStream stream)
        {
            stream = new MemoryStream(stream.ToArray());
            return new StreamReader(stream).ReadToEnd();
        }
    }
}
