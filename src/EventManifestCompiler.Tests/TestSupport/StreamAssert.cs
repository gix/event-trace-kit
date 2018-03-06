namespace EventManifestCompiler.Tests.TestSupport
{
    using System;
    using System.IO;
    using Xunit;
    using Xunit.Sdk;

    public static class StreamAssert
    {
        public static void SequenceEqual(
            Stream actual, Stream expected, Func<Stream, string> streamFormatter = null)
        {
            if (actual.Length != expected.Length) {
                if (streamFormatter == null)
                    throw new AssertActualExpectedException(
                        expected.Length, actual.Length, "Stream length mismatch");

                throw new AssertActualExpectedException(
                    streamFormatter(expected), streamFormatter(actual),
                    $"Stream length expected to be {expected.Length}, but is {actual.Length}");
            }

            actual.Position = 0;
            expected.Position = 0;

            var actualBuffer = new byte[4096];
            var expectedBuffer = new byte[4096];

            long unreadCount = actual.Length;
            long position = 0;
            while (unreadCount > 0) {
                var blockSize = Math.Min((int)unreadCount, actualBuffer.Length);
                actual.ReadExact(actualBuffer, 0, blockSize);
                expected.ReadExact(expectedBuffer, 0, blockSize);

                for (int i = 0; i < blockSize; ++i) {
                    if (actualBuffer[i] != expectedBuffer[i]) {
                        if (streamFormatter == null)
                            throw new AssertActualExpectedException(
                                expectedBuffer[i], actualBuffer[i],
                                $"Byte mismatch at offset {position + i}");

                        throw new AssertActualExpectedException(
                            streamFormatter(expected), streamFormatter(actual),
                            $"Byte mismatch at offset {position + i}: expected {expectedBuffer[i]}, but is {actualBuffer[i]}");
                    }
                }

                position += blockSize;
                unreadCount -= blockSize;
            }
        }
    }
}
