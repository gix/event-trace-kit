namespace EventTraceKit.VsExtension.Filtering
{
    using System;
    using System.IO;
    using System.Text;

    public class TextBuffer
    {
        public const char InvalidCharacter = char.MaxValue;

        private readonly char[] buffer;

        private TextBuffer(char[] buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (buffer.Length == 0 || buffer[buffer.Length - 1] != InvalidCharacter)
                throw new ArgumentException("Unterminated buffer.", nameof(buffer));
            this.buffer = buffer;
        }

        public static TextBuffer FromFile(string fileName)
        {
            var buffer = new char[new FileInfo(fileName).Length + 1];
            using (var stream = File.OpenRead(fileName))
            using (var reader = new StreamReader(stream, Encoding.UTF8))
                reader.Read(buffer, 0, buffer.Length - 1);
            buffer[buffer.Length - 1] = InvalidCharacter;

            return new TextBuffer(buffer);
        }

        public static TextBuffer FromString(string source)
        {
            var buffer = new char[source.Length + 1];
            source.CopyTo(0, buffer, 0, source.Length);
            buffer[buffer.Length - 1] = InvalidCharacter;

            return new TextBuffer(buffer);
        }

        public int Position { get; set; }

        public char this[int index] => buffer[index];

        public void AdvanceChar()
        {
            ++Position;
        }

        public char PeekChar()
        {
            if (Position >= buffer.Length)
                return InvalidCharacter;
            return buffer[Position];
        }

        public char PeekChar(int delta)
        {
            int deltaOffset = Position + delta;
            if (deltaOffset < 0 || deltaOffset >= buffer.Length)
                return InvalidCharacter;
            return buffer[deltaOffset];
        }

        public char GetAndAdvanceChar()
        {
            char c = PeekChar();
            AdvanceChar();
            return c;
        }

        public string GetText(int offset, int length)
        {
            return new string(buffer, offset, length);
        }
    }
}
