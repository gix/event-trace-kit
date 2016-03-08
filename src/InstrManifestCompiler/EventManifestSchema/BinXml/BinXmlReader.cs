namespace InstrManifestCompiler.EventManifestSchema.BinXml
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Xml.Linq;
    using InstrManifestCompiler.Extensions;
    using InstrManifestCompiler.Support;

    internal sealed class BinXmlReader : IDisposable
    {
        private readonly Stack<XNamespace> namespaceStack = new Stack<XNamespace>();
        private readonly IList<BinXmlType> substitutionTypes;
        private readonly BinaryReader r;

        public BinXmlReader(Stream input, IList<BinXmlType> substitutionTypes)
        {
            this.substitutionTypes = substitutionTypes;
            r = new BinaryReader(input, Encoding.ASCII, true);
        }

        public static XDocument Read(Stream input, IList<BinXmlType> substitutionTypes)
        {
            return new BinXmlReader(input, substitutionTypes).Read();
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
                    case Token.EndOfFragmentToken:
                        eof = true;
                        break;
                    case Token.OpenStartElementToken:
                        doc.Add(ReadElement((token & Constants.HasMoreFlag) != 0));
                        break;
                    default:
                        throw new InternalException("Unexpected BinXml tag 0x{0:X}", token);
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
                Console.WriteLine(
                    "Unexpected BinXml tag 0x{0:X}, expected 0x{1:X} or 0x{2:X}",
                    token, Token.CloseStartElementToken, Token.CloseEmptyElementToken);

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

        private XElement ReadElementContent(XElement parent)
        {
            while (true) {
                byte token = r.ReadByte();
                switch (token & 0xF) {
                    case Token.EndElementToken:
                        return parent;

                    case Token.OpenStartElementToken:
                        parent.Add(ReadElement((token & Constants.HasMoreFlag) != 0));
                        break;
                    case Token.NormalSubstitutionToken:
                        ReadNormalSubstitution(parent);
                        break;

                    default:
                        throw new InternalException("Unexpected BinXml tag 0x{0:X}", token);
                }
            }
        }

        private string ReadName()
        {
            ushort hash = r.ReadUInt16();
            ushort nameLength = r.ReadUInt16();
            string name = r.ReadPaddedString(Encoding.Unicode, nameLength * 2);
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
                    throw new InternalException(
                        "Unexpected eof while reading attributes");

                token = r.ReadByte();
                if ((token & 0xF) != Token.AttributeToken)
                    throw new InternalException(
                        "Invalid attribute token 0x{0} at byte offset {1}",
                        token & 0xF,
                        r.BaseStream.Position - 1);

                string name = ReadName();

                byte dataToken = r.ReadByte();
                byte valueType = r.ReadByte();
                if (dataToken != 0x5)
                    throw new InternalException(
                        "Unhandled attribute data token {0} at byte offset {1}",
                        dataToken,
                        r.BaseStream.Position - 1);
                if (valueType != 0x1)
                    throw new InternalException(
                        "Unexpected attribute value type {0} at byte offset {1}",
                        valueType,
                        r.BaseStream.Position - 1);

                string value = ReadUnicodeString();

                attributes.Add(new XAttribute(name, value));
            } while ((token & Constants.HasMoreFlag) != 0);

            if (r.BaseStream.Position != endOffset)
                throw new InternalException(
                    "AttribList read error: curr offset: {0}, expected: {1})",
                    r.BaseStream.Position,
                    endOffset);
            r.BaseStream.Position = endOffset;
        }

        private string ReadUnicodeString()
        {
            ushort length = r.ReadUInt16();
            return r.ReadPaddedString(Encoding.Unicode, length * 2);
        }
    }
}
