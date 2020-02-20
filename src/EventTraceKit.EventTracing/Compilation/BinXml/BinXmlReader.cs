namespace EventTraceKit.EventTracing.Compilation.BinXml
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Xml.Linq;
    using EventTraceKit.EventTracing.Internal.Extensions;
    using EventTraceKit.EventTracing.Schema;
    using EventTraceKit.EventTracing.Support;

    public sealed class BinXmlReader : IDisposable
    {
        private readonly Stack<XNamespace> namespaceStack = new Stack<XNamespace>();
        private readonly IList<BinXmlType> substitutionTypes;
        private readonly BinaryReader r;

        public BinXmlReader(Stream input, IList<BinXmlType> substitutionTypes = null)
        {
            this.substitutionTypes = substitutionTypes ?? new List<BinXmlType>();
            r = new BinaryReader(input, Encoding.ASCII, true);
        }

        public static XDocument Read(Stream input, IList<BinXmlType> substitutionTypes = null)
        {
            using (var reader = new BinXmlReader(input, substitutionTypes))
                return reader.Read();
        }

        public void Dispose()
        {
            r.Dispose();
        }

        public XDocument Read()
        {
            ReadFragmentHeader();

            namespaceStack.Clear();
            namespaceStack.Push(EventSchema.Namespace);
            var doc = new XDocument();

            bool eof = false;
            while (!eof) {
                byte token = r.ReadByte();
                switch (token & 0xF) {
                    case Token.OpenStartElementToken:
                        doc.Add(ReadElement((token & Constants.HasMoreFlag) != 0));
                        break;
                    case Token.EndOfFragmentToken:
                        eof = true;
                        break;
                    default:
                        throw UnexpectedToken(
                            r.BaseStream.Position - 1,
                            token, Token.OpenStartElementToken, Token.EndOfFragmentToken);
                }
            }

            return doc;
        }

        private void ReadFragmentHeader()
        {
            byte token = r.ReadByte();
            byte major = r.ReadByte();
            byte minor = r.ReadByte();
            byte flags = r.ReadByte();

            if (token != Token.FragmentHeaderToken)
                throw ParseError(
                    r.BaseStream.Position - 4,
                    "Unexpected token 0x{0:X} ({0}), expected 0x{1:X} ({1})",
                    token, Token.FragmentHeaderToken);

            if (major != Constants.MajorVersion || minor != Constants.MinorVersion)
                throw ParseError(
                    r.BaseStream.Position - 3,
                    "Unsupported version {0}.{1}, expected {2}.{3}",
                    major, minor, Constants.MajorVersion, Constants.MinorVersion);

            if (flags != 0)
                throw ParseError(
                    r.BaseStream.Position - 1,
                    "Unsupported flags 0x{0:X}, must be zero.", flags);
        }

        private void ReadNormalSubstitution(XElement parent)
        {
            ushort substitutionId = r.ReadUInt16();
            byte valueType = r.ReadByte();
            parent.Add("%" + (substitutionId + 1));
            while (substitutionTypes.Count <= substitutionId)
                substitutionTypes.Add(0);
            substitutionTypes[substitutionId] = (BinXmlType)valueType;
        }

        private XElement ReadElement(bool hasAttribs = false)
        {
            short dependencyId = r.ReadInt16();
            uint size = r.ReadUInt32();
            string name = ReadName();

            var attribs = new List<XAttribute>();
            if (hasAttribs)
                ReadAttributeList(attribs);

            XNamespace ns = PushNamespace(attribs);

            var element = new XElement(namespaceStack.Peek() + name);
            element.Add(attribs);

            byte token = r.ReadByte();
            if (token != Token.CloseStartElementToken &&
                token != Token.CloseEmptyElementToken)
                throw UnexpectedToken(
                    r.BaseStream.Position - 1, token,
                    Token.CloseStartElementToken, Token.CloseEmptyElementToken);

            if (token != Token.CloseEmptyElementToken)
                ReadElementContent(element);

            PopNamespace(ns);

            return element;
        }

        private void PopNamespace(XNamespace ns)
        {
            if (ns == null)
                return;

            if (namespaceStack.Count == 0 || namespaceStack.Peek() != ns)
                throw new InternalException("Unbalanced namespace stack.");
            namespaceStack.Pop();
        }

        private XNamespace PushNamespace(IEnumerable<XAttribute> attribs)
        {
            foreach (var attrib in attribs) {
                if (attrib.Name == "xmlns") {
                    var ns = XNamespace.Get(attrib.Value);
                    namespaceStack.Push(ns);
                    return ns;
                }
            }
            return null;
        }

        private void ReadElementContent(XElement parent)
        {
            while (true) {
                byte token = r.ReadByte();
                switch (token & 0xF) {
                    case Token.EndElementToken:
                        return;

                    case Token.OpenStartElementToken:
                        parent.Add(ReadElement((token & Constants.HasMoreFlag) != 0));
                        break;
                    case Token.NormalSubstitutionToken:
                        ReadNormalSubstitution(parent);
                        break;

                    default:
                        throw ParseError(
                            r.BaseStream.Position - 1, "Unexpected BinXml tag 0x{0:X}", token);
                }
            }
        }

        private string ReadName()
        {
            ushort hash = r.ReadUInt16();
            ushort nameLength = r.ReadUInt16();
            string name = r.ReadPaddedString(Encoding.Unicode, (uint)nameLength * 2);
            ushort nul = r.ReadUInt16();
            return name;
        }

        private void ReadAttributeList(List<XAttribute> attributes)
        {
            uint dataSize = r.ReadUInt32();
            long endOffset = r.BaseStream.Position + dataSize;

            byte token;
            do {
                if (r.BaseStream.Position > endOffset)
                    throw ParseError(
                        r.BaseStream.Position,
                        "Read past the end (at offset 0x{0:X}) of AttributeList block",
                        endOffset);

                token = r.ReadByte();
                if ((token & 0xF) != Token.AttributeToken)
                    throw ParseError(
                        r.BaseStream.Position - 1,
                        "Invalid attribute token 0x{0}.",
                        token & 0xF);

                string name = ReadName();

                byte dataToken = r.ReadByte();
                byte valueType = r.ReadByte();

                if (dataToken != 0x5)
                    throw ParseError(
                        r.BaseStream.Position - 1,
                        "Unexpected attribute data token {0}, expected 5.",
                        dataToken);
                if (valueType != 0x1)
                    throw ParseError(
                        r.BaseStream.Position - 2,
                        "Unexpected attribute value type {0}, expected 1.",
                        valueType);

                string value = ReadUnicodeString();

                attributes.Add(new XAttribute(name, value));
            } while ((token & Constants.HasMoreFlag) != 0);

            if (r.BaseStream.Position != endOffset)
                throw ParseError(
                    r.BaseStream.Position,
                    "Read past the end (at offset 0x{0:X}) of AttributeList block",
                    endOffset);

            r.BaseStream.Position = endOffset;
        }

        private string ReadUnicodeString()
        {
            ushort length = r.ReadUInt16();
            return r.ReadPaddedString(Encoding.Unicode, (uint)length * 2);
        }

        private Exception UnexpectedToken(long position, byte token, byte expected1, byte expected2)
        {
            return ParseError(
                position,
                "Unexpected BinXml tag 0x{0:X} ({0}), expected 0x{1:X} ({1}) or 0x{2:X} ({2})",
                token, expected1, expected2);
        }

        private Exception ParseError(long position, string format, params object[] args)
        {
            string message = string.Format(format, args);

            string input = GetStreamInput(r.BaseStream);
            return new InvalidOperationException($"{input}(offset {position}): {message}");
        }

        private static string GetStreamInput(Stream stream)
        {
            if (stream is FileStream file)
                return file.Name;
            return null;
        }
    }
}
