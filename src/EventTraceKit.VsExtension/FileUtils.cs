namespace EventTraceKit.VsExtension
{
    using System;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Security.Permissions;
    using Native;

    public static class FileUtils
    {
        public static void Replace(string sourceFileName, string destFileName)
        {
            if (sourceFileName == null)
                throw new ArgumentNullException(nameof(sourceFileName));
            if (destFileName == null)
                throw new ArgumentNullException(nameof(destFileName));

            string sourcePath = Path.GetFullPath(sourceFileName);
            string destPath = Path.GetFullPath(destFileName);
            new FileIOPermission(FileIOPermissionAccess.Write | FileIOPermissionAccess.Read, sourcePath).Demand();
            new FileIOPermission(FileIOPermissionAccess.Write, destPath).Demand();

            var flags = MOVEFILE_FLAGS.REPLACE_EXISTING;
            if (!NativeMethods.MoveFileEx(sourcePath, destPath, flags)) {
                int ec = Marshal.GetLastWin32Error();
                ThrowWinIOError(ec, sourcePath);
            }
        }

        [SecurityCritical]
        public static void ThrowWinIOError(int errorCode, string maybeFullPath)
        {
            bool isInvalidPath = errorCode == NativeMethods.ERROR_INVALID_NAME ||
                                 errorCode == NativeMethods.ERROR_BAD_PATHNAME;
            string str = GetDisplayablePath(maybeFullPath, isInvalidPath);

            switch (errorCode) {
                case NativeMethods.ERROR_FILE_NOT_FOUND:
                    if (str.Length == 0)
                        throw new FileNotFoundException("Unable to find the specified file.");
                    throw new FileNotFoundException($"Could not find file '{str}'.", str);

                case NativeMethods.ERROR_PATH_NOT_FOUND:
                    if (str.Length == 0)
                        throw new DirectoryNotFoundException("Could not find a part of the path.");
                    throw new DirectoryNotFoundException(
                        $"Could not find a part of the path '{str}'.");

                case NativeMethods.ERROR_ACCESS_DENIED:
                    if (str.Length == 0)
                        throw new UnauthorizedAccessException("Access to the path is denied.");
                    throw new UnauthorizedAccessException($"Access to the path '{str}' is denied.");

                case NativeMethods.ERROR_ALREADY_EXISTS:
                    if (str.Length == 0)
                        goto default;

                    throw new IOException(
                        $"Cannot create \"{str}\" because a file or directory with the same name already exists.",
                        NativeMethods.HResultFromWin32(errorCode));

                case NativeMethods.ERROR_FILENAME_EXCED_RANGE:
                    throw new PathTooLongException(
                        "The specified path, file name, or both are too long.");

                case NativeMethods.ERROR_INVALID_DRIVE:
                    throw new DriveNotFoundException(
                        $"Could not find the drive '{str}'. The drive might not be ready or might not be mapped.");

                case NativeMethods.ERROR_INVALID_PARAMETER:
                    throw new IOException(
                        NativeMethods.GetMessage(errorCode),
                        NativeMethods.HResultFromWin32(errorCode));

                case NativeMethods.ERROR_SHARING_VIOLATION:
                    if (str.Length == 0)
                        throw new IOException(
                            "The process cannot access the file because it is being used by another process.",
                            NativeMethods.HResultFromWin32(errorCode));
                    else
                        throw new IOException(
                            $"The process cannot access the file '{str}' because it is being used by another process.",
                            NativeMethods.HResultFromWin32(errorCode));

                case NativeMethods.ERROR_FILE_EXISTS:
                    if (str.Length == 0)
                        goto default;
                    throw new IOException(
                        $"The file '{str}' already exists.",
                        NativeMethods.HResultFromWin32(errorCode));

                case NativeMethods.ERROR_OPERATION_ABORTED:
                    throw new OperationCanceledException();

                default:
                    throw new IOException(
                        NativeMethods.GetMessage(errorCode),
                        NativeMethods.HResultFromWin32(errorCode));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsDirectorySeparator(char c)
        {
            return c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar;
        }

        internal static bool IsPartiallyQualifiedPath(string path)
        {
            if (path.Length < 2)
                return true;

            if (IsDirectorySeparator(path[0]))
                return path[1] != '?' && !IsDirectorySeparator(path[1]);

            if (path.Length >= 3 && path[1] == Path.VolumeSeparatorChar && IsDirectorySeparator(path[2]))
                return !IsValidDriveChar(path[0]);

            return true;
        }

        internal static bool IsValidDriveChar(char value)
        {
            return (value >= 'A' && value <= 'Z') ||
                   (value >= 'a' && value <= 'z');
        }

        [SecurityCritical]
        internal static string GetDisplayablePath(string path, bool isInvalidPath)
        {
            if (string.IsNullOrEmpty(path))
                return string.Empty;

            if (path.Length < 2)
                return path;

            // Return the path as is if we're relative (not fully qualified) and not a bad path
            if (IsPartiallyQualifiedPath(path) && !isInvalidPath)
                return path;

            bool safeToReturn = false;
            try {
                if (!isInvalidPath) {
                    new FileIOPermission(FileIOPermissionAccess.PathDiscovery, path).Demand();
                    safeToReturn = true;
                }
            } catch (SecurityException) {
            } catch (ArgumentException) {
                // ? and * characters cause ArgumentException to be thrown from HasIllegalCharacters
                // inside FileIOPermission.AddPathList
            } catch (NotSupportedException) {
                // paths like "!Bogus\\dir:with/junk_.in it" can cause NotSupportedException to be thrown
                // from Security.Util.StringExpressionSet.CanonicalizePath when ':' is found in the path
                // beyond string index position 1.  
            }

            if (!safeToReturn) {
                if (IsDirectorySeparator(path[path.Length - 1]))
                    path = "<Path discovery permission to the specified directory was denied.>";
                else
                    path = Path.GetFileName(path);
            }

            return path;
        }
    }
}
