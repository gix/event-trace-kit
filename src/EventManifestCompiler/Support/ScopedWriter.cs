namespace EventManifestCompiler.Support
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    internal class ScopedWriter
    {
        private readonly TextWriter writer;
        private int indentLevel;

        public ScopedWriter(TextWriter writer)
        {
            this.writer = writer;
        }

        public void Flush()
        {
            writer.Flush();
        }

        public void Indent(int levels = 1)
        {
            indentLevel += levels;
        }

        public void Unindent(int levels = 1)
        {
            indentLevel = Math.Max(0, indentLevel - levels);
        }

        public void ResetIndent() { indentLevel = 0; }

        public string Prefix { get; set; }

        public void WriteIndent()
        {
            if (Prefix != null)
                writer.Write(Prefix);

            for (int i = 0; i < indentLevel; ++i)
                writer.Write("  ");
        }

        public void WriteLine(object value)
        {
            StartLine();
            writer.WriteLine(value);
        }

        public void WriteLine(string format, params object[] args)
        {
            StartLine();
            writer.WriteLine(format, args);
        }

        public void WriteNumber(string label, int value)
        {
            WriteValue(label, value);
        }

        public void WriteNumber(string label, uint value)
        {
            WriteValue(label, value);
        }

        public void WriteHex(string label, uint value)
        {
            WriteValue(label, $"0x{value:X}");
        }

        public void WriteHex(string label, uint value, string extra)
        {
            WriteValue(label, $"0x{value:X} ({extra})");
        }

        public void WriteEnum<T>(string label, T value)
            where T : struct
        {
            WriteValue(label, $"0x{value:X} ({value})");
        }

        public void WriteString(string label, string value)
        {
            WriteValue(label, value);
        }

        public void WriteStringBlock(string label, string value)
        {
            PushDictScope(label);

            var lines = value.Split(new []{ '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
                WriteLine(line);

            PopScope();
        }

        public void WriteValue(string label, object value)
        {
            StartLine();
            writer.Write(label);
            writer.Write(": ");
            writer.Write(value);
            writer.Write('\n');
        }

        public void StartLine()
        {
            WriteIndent();
        }

        public void PushListScope(string format, params object[] args)
        {
            PushScope(string.Format(format, args), '[', ']');
        }

        public void PushDictScope(string format, params object[] args)
        {
            PushScope(string.Format(format, args), '{', '}');
        }

        public void PushScope(string name, char openDelimiter, char closeDelimiter)
        {
            StartLine();
            writer.Write(name);
            writer.Write(' ');
            writer.Write(openDelimiter);
            writer.Write('\n');
            ++indentLevel;
            scopes.Push(closeDelimiter);
        }

        public void PopScope()
        {
            char delimiter = scopes.Pop();
            --indentLevel;
            StartLine();
            writer.Write(delimiter);
            writer.Write('\n');
        }

        private readonly Stack<char> scopes = new Stack<char>();
    }
}
