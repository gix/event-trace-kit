namespace TraceLaunchTester
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.IO.Pipes;

    class DebugLaunchSettings
    {
        public string Executable { get; set; }
        public string Arguments { get; set; }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            var launchSettings = QueryDebugTargets(true);

            var pipeName = Guid.NewGuid().ToString("D");
            Log("[SERVER] pipe: {0}", pipeName);

            var client = new Process();
            client.StartInfo.FileName = @"C:\Users\nrieck\dev\EventTraceKit\build\x86-dbg\bin\TraceLaunch.x86.exe";
            client.StartInfo.Arguments = pipeName + " " + launchSettings.Arguments;
            client.StartInfo.UseShellExecute = false;

            var pipe = new NamedPipeServerStream(
                pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Message,
                PipeOptions.WriteThrough, 4, 4);
            pipe.ReadMode = PipeTransmissionMode.Message;

            using (pipe) {
                Log("[SERVER] Starting client");
                client.Start();
                Log("[SERVER] Waiting for incoming connection");
                pipe.WaitForConnection();

                try {
                    var buffer = new byte[4];
                    pipe.ReadExact(buffer, 0, buffer.Length);
                    uint pid = BitConverter.ToUInt32(buffer, 0);
                    Log("[SERVER] Received pid: {0}", pid);
                    Console.WriteLine("Pid: {0}", pid);

                    //Log("[SERVER] Simulating work");
                    //Thread.Sleep(2000);

                    buffer[0] = 1;
                    pipe.Write(buffer, 0, 1);
                    pipe.WaitForPipeDrain();
                } catch (IOException e) {
                    Log("[SERVER] Error: {0}", e.Message);
                }
            }

            client.WaitForExit();
            client.Close();
            Log("[SERVER] Client quit. Server terminating.");
        }

        private static void Log(string format, params object[] args)
        {
            return;
            Console.WriteLine(format, args);
        }

        private static DebugLaunchSettings QueryDebugTargets(bool isConsoleApp)
        {
            var launchSettings = new DebugLaunchSettings();
            launchSettings.Executable = @"C:\Users\nrieck\dev\Samples\ConsoleApplication3\Debug\ConsoleApplication3.exe";
            launchSettings.Arguments = @"CA3 ""foo bar""";

            if (isConsoleApp) {
                launchSettings.Arguments = string.Format(
                    CultureInfo.InvariantCulture, "/c \"\"{0}\" {1} & pause\"",
                    launchSettings.Executable, launchSettings.Arguments);
                launchSettings.Executable = Path.Combine(Environment.SystemDirectory, "cmd.exe");
            }

            return launchSettings;
        }
    }

    public static class StreamExtensions
    {
        /// <summary>
        ///   Reads a sequence of bytes from the specified stream and advances
        ///   the position within the stream by the number of bytes read.
        /// </summary>
        /// <param name="input">
        ///   The stream from which to read from.
        /// </param>
        /// <param name="buffer">
        ///   An array of bytes. When this method returns, the buffer contains
        ///   the specified byte array with the values between <paramref name="offset"/>
        ///   and <c>(<paramref name="offset"/> + <paramref name="count"/> - 1)</c>
        ///   replaced by the bytes read from the current source.
        /// </param>
        /// <param name="offset">
        ///   The zero-based byte offset in <paramref name="buffer"/> at which
        ///   to begin storing the data read from the stream.
        /// </param>
        /// <param name="count">
        ///   The maximum number of bytes to be read from the stream.
        /// </param>
        /// <returns>
        ///   The total number of bytes read into the buffer. This will always
        ///   be equal to <paramref name="count"/>. If not enough bytes can be
        ///   read <see cref="EndOfStreamException"/> will be raised.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   The sum of <paramref name="offset"/> and <paramref name="count"/>
        ///   is larger than the buffer length.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="input"/> or <paramref name="buffer"/> is
        ///   <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="offset"/> or <paramref name="count"/> is negative.
        /// </exception>
        /// <exception cref="IOException">
        ///   An I/O error occurs.
        /// </exception>
        /// <exception cref="NotSupportedException">
        ///   The stream does not support reading.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   Methods were called after the stream was closed.
        /// </exception>
        /// <exception cref="EndOfStreamException">
        ///   End of <paramref name="input"/> stream is reached without reading
        ///   <paramref name="count"/> bytes.
        /// </exception>
        public static int ReadExact(this Stream input, byte[] buffer, int offset, int count)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0 || offset >= buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 1 || offset + count > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(count));

            int totalBytesRead = 0;
            while (totalBytesRead < count) {
                int bytesRead = input.Read(buffer, offset + totalBytesRead, count - totalBytesRead);
                if (bytesRead == 0) {
                    int bytesLeft = count - totalBytesRead;
                    throw new EndOfStreamException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "End of stream reached with {0} byte{1} left to read.",
                            bytesLeft,
                            bytesLeft == 1 ? string.Empty : "s"));
                }

                totalBytesRead += bytesRead;
            }

            return totalBytesRead;
        }
    }
}
