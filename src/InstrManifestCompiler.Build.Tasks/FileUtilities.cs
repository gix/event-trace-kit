namespace InstrManifestCompiler.Build.Tasks
{
    using System;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Security;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    internal static class FileUtilities
    {
        internal static string GetTemporaryFile()
        {
            return GetTemporaryFile(".tmp");
        }

        internal static string GetTemporaryFile(string extension)
        {
            return GetTemporaryFile(null, extension);
        }

        internal static string GetTemporaryFile(string directory, string extension)
        {
            Contract.Requires<ArgumentNullException>(directory == null || directory.Length != 0);
            Contract.Requires<ArgumentException>(!String.IsNullOrEmpty(extension));

            if (extension[0] != '.')
                extension = '.' + extension;

            string path;
            try {
                directory = directory ?? Path.GetTempPath();
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
                path = Path.Combine(directory, "tmp" + Guid.NewGuid().ToString("N") + extension);
                ErrorUtilities.VerifyThrow(!File.Exists(path), "Guid should be unique");
                File.WriteAllText(path, String.Empty);
            } catch (Exception ex) when (!ExceptionHandling.NotExpectedException(ex)) {
                throw new IOException(
                    //ResourceUtilities.FormatResourceString("Shared.FailedCreatingTempFile", ex.Message),
                    ex.Message,
                    ex);
            }

            return path;
        }

        public static int DeleteEmptyFile(ITaskItem[] filesToDelete)
        {
            if (filesToDelete == null)
                return 0;

            ITaskItem[] items = TrackedDependencies.ExpandWildcards(filesToDelete);
            if (items.Length == 0)
                return 0;

            int deleted = 0;
            foreach (ITaskItem item in items) {
                try {
                    var info = new FileInfo(item.ItemSpec);
                    if (info.Exists && info.Length <= 4L) {
                        info.Delete();
                        ++deleted;
                    }
                } catch (Exception exception) {
                    if (!(exception is SecurityException) &&
                        !(exception is ArgumentException) &&
                        !(exception is UnauthorizedAccessException) &&
                        !(exception is PathTooLongException) &&
                        !(exception is NotSupportedException))
                        throw;
                }
            }

            return deleted;
        }
    }
}
