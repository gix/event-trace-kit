namespace EventTraceKit.EventTracing.Support
{
    using System;
    using System.IO;
    using System.Text;

    public sealed class IndentableTextWriter : TextWriter
    {
        private readonly TextWriter writer;
        private bool indentationPending = true;
        private string indentChars;
        private int indentLevel;

        public IndentableTextWriter(TextWriter writer)
        {
            this.writer = writer ?? throw new ArgumentNullException(nameof(writer));

            NewLine = "\n";
            IndentChars = "    ";
        }

        public override Encoding Encoding => writer.Encoding;

        public int IndentLevel
        {
            get => indentLevel;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), value, null);
                indentLevel = value;
            }
        }

        public string IndentChars
        {
            get => indentChars;
            set => indentChars = value ?? throw new ArgumentNullException(nameof(value));
        }

        public override string NewLine
        {
            get => writer.NewLine;
            set => writer.NewLine = value;
        }

        public override void Write(bool value)
        {
            WriteIndentation();
            writer.Write(value);
        }

        public override void Write(int value)
        {
            WriteIndentation();
            writer.Write(value);
        }

        public override void Write(long value)
        {
            WriteIndentation();
            writer.Write(value);
        }

        public override void Write(float value)
        {
            WriteIndentation();
            writer.Write(value);
        }

        public override void Write(double value)
        {
            WriteIndentation();
            writer.Write(value);
        }

        public override void Write(object value)
        {
            WriteIndentation();
            writer.Write(value);
        }

        public override void Write(char value)
        {
            WriteIndentation();
            writer.Write(value);
        }

        public override void Write(string str)
        {
            int startIdx = 0;
            for (int i = 0; i < str.Length; ++i) {
                if (str[i] == '\n') {
                    int length = i - startIdx;
                    if (length >= 1 && str[i - 1] == '\r')
                        --length;

                    if (length > 0) // Do not indent empty lines.
                        WriteIndentation();

                    writer.Write(str.Substring(startIdx, length));
                    writer.Write(NewLine);

                    startIdx = i + 1;
                    indentationPending = true;
                }
            }

            if (startIdx < str.Length) {
                WriteIndentation();
                writer.Write(str.Substring(startIdx));
            }
        }

        public override void Write(string format, params object[] args)
        {
            Write(string.Format(writer.FormatProvider, format, args));
        }

        public override void Write(string format, object arg0)
        {
            Write(string.Format(writer.FormatProvider, format, arg0));
        }

        public override void Write(string format, object arg0, object arg1)
        {
            Write(string.Format(writer.FormatProvider, format, arg0, arg1));
        }

        public override void Write(string format, object arg0, object arg1, object arg2)
        {
            Write(string.Format(writer.FormatProvider, format, arg0, arg1, arg2));
        }

        public override void Write(char[] buffer, int index, int count)
        {
            Write(new string(buffer, index, count));
        }

        public override void Write(char[] buffer)
        {
            Write(new string(buffer));
        }

        public override void WriteLine()
        {
            writer.WriteLine();
            indentationPending = true;
        }

        public override void WriteLine(bool value)
        {
            WriteIndentation();
            writer.WriteLine(value);
            indentationPending = true;
        }

        public override void WriteLine(int value)
        {
            WriteIndentation();
            writer.WriteLine(value);
            indentationPending = true;
        }

        public override void WriteLine(long value)
        {
            WriteIndentation();
            writer.WriteLine(value);
            indentationPending = true;
        }

        public override void WriteLine(float value)
        {
            WriteIndentation();
            writer.WriteLine(value);
            indentationPending = true;
        }

        public override void WriteLine(double value)
        {
            WriteIndentation();
            writer.WriteLine(value);
            indentationPending = true;
        }

        public override void WriteLine(object value)
        {
            WriteIndentation();
            writer.WriteLine(value);
            indentationPending = true;
        }

        public override void WriteLine(char value)
        {
            WriteIndentation();
            writer.WriteLine(value);
            indentationPending = true;
        }

        public override void WriteLine(string s)
        {
            Write(s);
            if (!indentationPending) {
                writer.WriteLine();
                indentationPending = true;
            }
        }

        public override void WriteLine(string format, params object[] args)
        {
            WriteLine(string.Format(writer.FormatProvider, format, args));
        }

        public override void WriteLine(string format, object arg0)
        {
            WriteLine(string.Format(writer.FormatProvider, format, arg0));
        }

        public override void WriteLine(string format, object arg0, object arg1)
        {
            WriteLine(string.Format(writer.FormatProvider, format, arg0, arg1));
        }

        public override void WriteLine(string format, object arg0, object arg1, object arg2)
        {
            WriteLine(string.Format(writer.FormatProvider, format, arg0, arg1, arg2));
        }

        public override void WriteLine(char[] buffer, int index, int count)
        {
            WriteLine(new string(buffer, index, count));
        }

        public override void WriteLine(char[] buffer)
        {
            WriteLine(new string(buffer));
        }

        public override void Flush()
        {
            writer.Flush();
        }

        public void WriteRaw(string str)
        {
            writer.Write(str);
        }

        private void WriteIndentation()
        {
            if (indentationPending) {
                for (int i = 0; i < IndentLevel; ++i)
                    writer.Write(IndentChars);
                indentationPending = false;
            }
        }

        public IDisposable IndentScope(int n = 1)
        {
            return new InternalIndentScope(this, n);
        }

        private sealed class InternalIndentScope : IDisposable
        {
            private readonly IndentableTextWriter writer;
            private readonly int n;

            public InternalIndentScope(IndentableTextWriter writer, int n)
            {
                this.writer = writer;
                this.n = n;

                writer.IndentLevel += n;
            }

            public void Dispose()
            {
                writer.IndentLevel -= n;
            }
        }
    }
}
