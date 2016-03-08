namespace InstrManifestCompiler.Support
{
    using System;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Text;

    internal sealed class IndentableTextWriter : TextWriter
    {
        private readonly TextWriter writer;
        private bool indentationPending;
        private string indentChars;
        private int indentLevel;

        public IndentableTextWriter(TextWriter writer)
        {
            Contract.Requires<ArgumentNullException>(writer != null);
            this.writer = writer;

            NewLine = "\n";
            IndentChars = "    ";
        }

        public int IndentLevel
        {
            get
            {
                Contract.Ensures(Contract.Result<int>() >= 0);
                return indentLevel;
            }
            set
            {
                Contract.Requires<ArgumentOutOfRangeException>(value >= 0);
                indentLevel = value;
            }
        }

        public string IndentChars
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);
                return indentChars;
            }
            set
            {
                Contract.Requires<ArgumentNullException>(value != null);
                indentChars = value;
            }
        }

        public override string NewLine
        {
            get { return writer.NewLine; }
            set { writer.NewLine = value; }
        }

        public override void Write(bool value)
        {
            WriteIndentation();
            writer.Write(value);
        }

        public override void Write(char value)
        {
            WriteIndentation();
            writer.Write(value);
        }

        public override void Write(double value)
        {
            WriteIndentation();
            writer.Write(value);
        }

        public override void Write(int value)
        {
            WriteIndentation();
            writer.Write(value);
        }

        public override void Write(char[] buffer)
        {
            WriteIndentation();
            writer.Write(buffer);
        }

        public override void Write(long value)
        {
            WriteIndentation();
            writer.Write(value);
        }

        public override void Write(object value)
        {
            WriteIndentation();
            writer.Write(value);
        }

        public override void Write(float value)
        {
            WriteIndentation();
            writer.Write(value);
        }

        public override void Write(string str)
        {
            WriteIndentation();
            if (str.IndexOf(NewLine, StringComparison.Ordinal) == -1) {
                writer.Write(str);
                return;
            }

            throw new NotImplementedException();
            //for (int s = 0, i = 0; i < str.Length; ++i) {
            //    if (i > 0)
            //        WriteIndentation();
            //    if (str[i] == '\n') {
            //        writer.Write(str.Substring(s, i + 1));
            //        s = i + 1;
            //        indentationPending = true;
            //    }
            //}
        }

        public override void Write(string format, params object[] arg)
        {
            WriteIndentation();
            writer.Write(format, arg);
        }

        public override void Write(string format, object arg0)
        {
            WriteIndentation();
            writer.Write(format, arg0);
        }

        public override void Write(char[] buffer, int index, int count)
        {
            WriteIndentation();
            writer.Write(buffer, index, count);
        }

        public override void Write(string format, object arg0, object arg1)
        {
            WriteIndentation();
            writer.Write(format, arg0, arg1);
        }

        public override void Write(string format, object arg0, object arg1, object arg2)
        {
            WriteIndentation();
            writer.Write(format, arg0, arg1, arg2);
        }

        private void WriteIndentation()
        {
            if (indentationPending) {
                for (int i = 0; i < IndentLevel; ++i)
                    writer.Write(IndentChars);
                indentationPending = false;
            }
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

        public override void WriteLine(char value)
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

        public override void WriteLine(char[] buffer)
        {
            WriteIndentation();
            writer.WriteLine(buffer);
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

        public override void WriteLine(object value)
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

        public override void WriteLine(string s)
        {
            WriteIndentation();
            writer.WriteLine(s);
            indentationPending = true;
        }

        public override void WriteLine(string format, object arg0)
        {
            WriteIndentation();
            writer.WriteLine(format, arg0);
            indentationPending = true;
        }

        public override void WriteLine(string format, params object[] arg)
        {
            WriteIndentation();
            writer.WriteLine(format, arg);
            indentationPending = true;
        }

        public override void WriteLine(string format, object arg0, object arg1)
        {
            WriteIndentation();
            writer.WriteLine(format, arg0, arg1);
            indentationPending = true;
        }

        public override void WriteLine(string format, object arg0, object arg1, object arg2)
        {
            WriteIndentation();
            writer.WriteLine(format, arg0, arg1, arg2);
            indentationPending = true;
        }

        public override void WriteLine(char[] buffer, int index, int count)
        {
            WriteIndentation();
            writer.WriteLine(buffer, index, count);
            indentationPending = true;
        }

        public override Encoding Encoding
        {
            get { return writer.Encoding; }
        }

        private sealed class InternalIndentScope : IDisposable
        {
            private readonly IndentableTextWriter writer;

            public InternalIndentScope(IndentableTextWriter writer)
            {
                this.writer = writer;
                writer.Indent();
            }

            public void Dispose()
            {
                writer.Outdent();
            }
        }

        public override void Flush()
        {
            writer.Flush();
        }

        public IDisposable IndentScope()
        {
            return new InternalIndentScope(this);
        }

        public void Indent()
        {
            ++IndentLevel;
        }

        public void Outdent()
        {
            --IndentLevel;
        }
    }
}
