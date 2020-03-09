namespace EventManifestCompiler.Build.Tasks.Tests
{
    using System.Collections.Generic;
    using System.Text;

    public class CommandLineUtils
    {
        public static IEnumerable<string> EnumerateCommandLineArgs(string commandLine)
        {
            if (string.IsNullOrEmpty(commandLine))
                yield break;

            int backslashCount = 0;
            bool inQuotes = false;

            var buffer = new StringBuilder();
            for (int i = 0; i < commandLine.Length; ++i) {
                char c = commandLine[i];
                switch (c) {
                    case '"':
                        bool literalQuote = (backslashCount % 2) != 0;
                        backslashCount /= 2;
                        for (; backslashCount > 0; --backslashCount)
                            buffer.Append('\\');

                        if (literalQuote)
                            buffer.Append('"');
                        else
                            inQuotes = !inQuotes;
                        break;

                    case '\\':
                        ++backslashCount;
                        break;

                    case ' ':
                    case '\t':
                        for (; backslashCount > 0; --backslashCount)
                            buffer.Append('\\');

                        if (inQuotes) {
                            buffer.Append(c);
                        } else {
                            if (buffer.Length != 0) {
                                yield return buffer.ToString();
                                buffer.Clear();
                            }
                        }
                        break;

                    default:
                        for (; backslashCount > 0; --backslashCount)
                            buffer.Append('\\');

                        buffer.Append(c);
                        break;
                }
            }

            if (buffer.Length != 0)
                yield return buffer.ToString();
        }
    }
}
