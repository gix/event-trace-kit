namespace EventTraceKit.VsExtension
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Security;
    using System.Security.Permissions;
    using System.Windows.Input;

    internal static class ResourceUtils
    {
        public static Cursor LoadCursor(Stream stream)
        {
            var permissions = new PermissionSet(null);

            var filePermission = new FileIOPermission(PermissionState.None) {
                AllLocalFiles = FileIOPermissionAccess.Write
            };
            permissions.AddPermission(filePermission);
            permissions.AddPermission(new EnvironmentPermission(PermissionState.Unrestricted));
            permissions.AddPermission(new SecurityPermission(SecurityPermissionFlag.UnmanagedCode));
            permissions.Assert();

            try {
                return new Cursor(stream);
            } finally {
                CodeAccessPermission.RevertAssert();
            }
        }

        public static Cursor LoadCursorFromResource(Type type, string resourceName)
        {
            Stream stream = type.Assembly.GetManifestResourceStream(type, resourceName);

            Debug.Assert(stream != null, "Resource stream is null");
            if (stream == null)
                return null;

            using (stream)
                return LoadCursor(stream);
        }
    }
}
