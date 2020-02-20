namespace EventTraceKit.EventTracing.Tests.Compilation.TestSupport
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    public static class StreamExtensions
    {
        /// <summary>
        ///   Reads a sequence of bytes from the specified stream and advances
        ///   the position within the stream by the number of bytes read.
        /// </summary>
        /// <param name="input">The stream from which to read from.</param>
        /// <param name="buffer">
        ///   An array of bytes. When this method returns, the buffer contains
        ///   the specified byte array with the values between <paramref name="offset"/>
        ///   and <c>(<paramref name="offset"/> + <paramref name="count"/> -
        ///   1)</c> replaced by the bytes read from the current source.
        /// </param>
        /// <param name="offset">
        ///   The zero-based byte offset in <paramref name="buffer"/> at which
        ///   to begin storing the data read from the stream.
        /// </param>
        /// <param name="count">
        ///   The exact number of bytes to be read from the stream.
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
        ///   An I/O error occured.
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

        /// <summary>
        ///   Asynchronously reads a sequence of bytes from the specified stream
        ///   and advances the position within the stream by the number of bytes
        ///   read.
        /// </summary>
        /// <param name="input">The stream from which to read from.</param>
        /// <param name="buffer">
        ///   An array of bytes. When this method returns, the buffer contains
        ///   the specified byte array with the values between <paramref name="offset"/>
        ///   and <c>(<paramref name="offset"/> + <paramref name="count"/> -
        ///   1)</c> replaced by the bytes read from the current source.
        /// </param>
        /// <param name="offset">
        ///   The zero-based byte offset in <paramref name="buffer"/> at which
        ///   to begin storing the data read from the stream.
        /// </param>
        /// <param name="count">
        ///   The exact number of bytes to be read from the stream.
        /// </param>
        /// <param name="cancellationToken">
        ///   The token to monitor for cancellation requests. The default value
        ///   is <see cref="CancellationToken.None"/>.
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
        ///   An I/O error occured.
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
        public static async Task<int> ReadExactAsync(
            this Stream input, byte[] buffer, int offset, int count,
            CancellationToken cancellationToken)
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
                int bytesRead = await input.ReadAsync(
                    buffer, offset + totalBytesRead, count - totalBytesRead,
                    cancellationToken);
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
