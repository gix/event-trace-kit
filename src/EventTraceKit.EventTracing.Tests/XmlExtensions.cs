namespace EventTraceKit.EventTracing.Tests
{
    using System.Xml.Linq;

    internal static class XmlExtensions
    {
        public static XElement GetOrCreateElement(this XElement element, XName name)
        {
            var child = element.Element(name);
            if (child == null) {
                child = new XElement(name);
                element.Add(child);
            }
            return child;
        }
    }
}
