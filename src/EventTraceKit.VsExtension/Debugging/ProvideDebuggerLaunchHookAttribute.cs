namespace EventTraceKit.VsExtension.Debugging
{
    using System;
    using Microsoft.VisualStudio.Shell;

    internal sealed class ProvideDebuggerLaunchHookAttribute : RegistrationAttribute
    {
        public ProvideDebuggerLaunchHookAttribute(Type launchHookType)
        {
            LaunchHookType = launchHookType;
        }

        public Type LaunchHookType { get; }

        public override void Register(RegistrationContext context)
        {
            Guid typeId = LaunchHookType.GUID;
            using (Key key = context.CreateKey($"CLSID\\{typeId:B}")) {
                key.SetValue(string.Empty, LaunchHookType.FullName);
                key.SetValue("Assembly", LaunchHookType.Assembly.FullName);
                key.SetValue("CodeBase", context.CodeBase);
                key.SetValue("Class", LaunchHookType.FullName);
                key.SetValue("InprocServer32", context.InprocServerPath);
                key.SetValue("ThreadingModel", "Both");
            }
            using (Key key = context.CreateKey(@"Debugger\LaunchHooks110")) {
                key.SetValue(LaunchHookType.Name, typeId.ToString("B"));
            }
        }

        public override void Unregister(RegistrationContext context)
        {
            context.RemoveKey($"CLSID\\{LaunchHookType.GUID:B}");
            using (Key key = context.CreateKey(@"Debugger\LaunchHooks110"))
                key.SetValue(LaunchHookType.Name, null);
        }
    }
}
