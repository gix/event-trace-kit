namespace EventTraceKit.VsExtension.Debugging
{
    public class TraceLaunchTarget
    {
        public TraceLaunchTarget(string executable, string arguments, uint processId, string projectPath)
        {
            Executable = executable;
            Arguments = arguments;
            ProcessId = processId;
            ProjectPath = projectPath;
        }

        public string Executable { get; }
        public string Arguments { get; }
        public uint ProcessId { get; }
        public string ProjectPath { get; }
    }
}
