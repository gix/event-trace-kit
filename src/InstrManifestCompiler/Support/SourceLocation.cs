namespace InstrManifestCompiler.Support
{
    using System;
    using System.Globalization;

    public sealed class SourceLocation : IEquatable<SourceLocation>
    {
        public SourceLocation()
        {
            LineNumber = -1;
            ColumnNumber = -1;
        }

        public SourceLocation(int lineNumber, int columnNumber)
        {
            LineNumber = lineNumber;
            ColumnNumber = columnNumber;
        }

        public SourceLocation(string filePath, int lineNumber, int columnNumber)
        {
            FilePath = filePath;
            LineNumber = lineNumber;
            ColumnNumber = columnNumber;
        }

        public string FilePath { get; private set; }
        public int LineNumber { get; private set; }
        public int ColumnNumber { get; private set; }

        public bool Equals(SourceLocation other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return
                string.Equals(FilePath, other.FilePath, StringComparison.OrdinalIgnoreCase) &&
                LineNumber == other.LineNumber &&
                ColumnNumber == other.ColumnNumber;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            return obj is SourceLocation && Equals((SourceLocation)obj);
        }

        public override int GetHashCode()
        {
            unchecked {
                int hashCode = (FilePath != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(FilePath) : 0);
                hashCode = (hashCode * 397) ^ LineNumber;
                hashCode = (hashCode * 397) ^ ColumnNumber;
                return hashCode;
            }
        }

        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture, "{0}:{1}:{2}", FilePath, LineNumber, ColumnNumber);
        }

        public static bool operator ==(SourceLocation left, SourceLocation right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(SourceLocation left, SourceLocation right)
        {
            return !Equals(left, right);
        }
    }
}
