namespace EventManifestCompiler.BinXml
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml;
    using System.Xml.Linq;
    using EventManifestCompiler.Extensions;
    using EventManifestCompiler.Support;

    public sealed class BinXmlWriter : IDisposable
    {
        private readonly BinaryWriter w;
        private readonly IList<byte> substitutionTypes;
        private bool useEmptyElements = false;

        public BinXmlWriter(Stream output, IList<byte> substitutionTypes)
        {
            this.substitutionTypes = substitutionTypes;
            w = IO.CreateBinaryWriter(output);
        }

        public static void Write(Stream output, XDocument doc, IList<byte> types)
        {
            using (var writer = new BinXmlWriter(output, types))
                writer.WriteFragment(doc);
        }

        public void Dispose()
        {
            w.Dispose();
        }

        public void WriteFragment(XDocument doc)
        {
            WriteFragmentHeader();
            WriteElement(doc.Root);
            w.WriteUInt8(Token.EndOfFragmentToken);
            w.FillAlignment(4);
        }

        private void WriteFragmentHeader()
        {
            const byte token = Token.FragmentHeaderToken;
            const byte major = Constants.MajorVersion;
            const byte minor = Constants.MinorVersion;
            const byte flags = 0;

            w.WriteUInt8(token);
            w.WriteUInt8(major);
            w.WriteUInt8(minor);
            w.WriteUInt8(flags);
        }

        private void WriteElement(XElement elem)
        {
            byte token = Token.OpenStartElementToken;
            if (elem.HasAttributes)
                token |= Constants.HasMoreFlag;
            short dependencyId = -1;

            w.WriteUInt8(token);
            w.WriteInt16(dependencyId);
            var size = w.ReserveUInt32();
            WriteName(elem.Name.LocalName);

            if (elem.HasAttributes)
                WriteAttributeList(elem);

            if (elem.IsEmpty && useEmptyElements) {
                w.WriteUInt8(Token.CloseEmptyElementToken);
                return;
            }

            w.WriteUInt8(Token.CloseStartElementToken);
            foreach (var child in elem.Nodes())
                WriteNode(child);
            w.WriteUInt8(Token.EndElementToken);

            size.UpdateRelative(-4);
        }

        private void WriteNode(XNode node)
        {
            switch (node.NodeType) {
                case XmlNodeType.Element:
                    WriteElement((XElement)node);
                    break;
                case XmlNodeType.Text:
                    ParseTemplateString(((XText)node).Value);
                    break;
                case XmlNodeType.None:
                case XmlNodeType.Attribute:
                case XmlNodeType.CDATA:
                case XmlNodeType.EntityReference:
                case XmlNodeType.Entity:
                case XmlNodeType.ProcessingInstruction:
                case XmlNodeType.Comment:
                case XmlNodeType.Document:
                case XmlNodeType.DocumentType:
                case XmlNodeType.DocumentFragment:
                case XmlNodeType.Notation:
                case XmlNodeType.Whitespace:
                case XmlNodeType.SignificantWhitespace:
                case XmlNodeType.EndElement:
                case XmlNodeType.EndEntity:
                case XmlNodeType.XmlDeclaration:
                    throw new NotSupportedException();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void ParseTemplateString(string value)
        {
            int startIdx = value.IndexOf('%');
            if (startIdx == -1)
                return;

            ++startIdx;
            int endIdx = startIdx;
            while (endIdx < value.Length && char.IsDigit(value[endIdx]))
                ++endIdx;

            string digits = value.Substring(startIdx, endIdx - startIdx);

            if (!int.TryParse(digits, NumberStyles.None, null, out var id))
                throw new InternalException("Invalid substitution '{0}'.", digits);
            WriteNormalSubstitution(id);
        }

        private void WriteNormalSubstitution(int id)
        {
            if (id == 0 || id > substitutionTypes.Count)
                throw new InternalException("SubstitutionId out of range.");
            --id;
            byte valueType = substitutionTypes[id];
            w.WriteUInt8(Token.NormalSubstitutionToken);
            w.WriteUInt16((ushort)id);
            w.WriteUInt8(valueType);
        }

        private void WriteName(string name)
        {
            int byteCount = Encoding.Unicode.GetByteCount(name);
            var bytes = new byte[byteCount];
            Encoding.Unicode.GetBytes(name, 0, name.Length, bytes, 0);

            w.WriteUInt16(Utils.HashString(name));
            w.WriteUInt16((ushort)name.Length);
            w.WriteBytes(bytes);
            w.WriteUInt16(0);
        }

        private void WriteLengthPrefixedUnicodeString(string str)
        {
            int byteCount = Encoding.Unicode.GetByteCount(str);
            var bytes = new byte[byteCount];
            Encoding.Unicode.GetBytes(str, 0, str.Length, bytes, 0);

            w.WriteUInt16((ushort)str.Length);
            w.WriteBytes(bytes);
        }

        private void WriteAttributeList(XElement elem)
        {
            var byteLength = w.ReserveUInt32();

            var attribs = elem.Attributes().ToList();
            for (int i = 0; i < attribs.Count; ++i) {
                var attrib = attribs[i];
                WriteAttribute(attrib, i == attribs.Count - 1);
            }

            byteLength.UpdateRelative(-4);
        }

        private void WriteAttribute(XAttribute attrib, bool last = false)
        {
            byte token = Token.AttributeToken;
            if (!last)
                token |= Constants.HasMoreFlag;

            w.WriteUInt8(token);
            WriteName(attrib.Name.LocalName);
            WriteValueText(attrib.Value);
        }

        private void WriteValueText(string value)
        {
            w.WriteUInt8(Token.ValueTextToken);
            w.WriteUInt8((byte)BinXmlType.String);
            WriteLengthPrefixedUnicodeString(value);
        }
    }
}
