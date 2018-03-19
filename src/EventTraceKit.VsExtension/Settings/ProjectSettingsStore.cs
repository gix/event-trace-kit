namespace EventTraceKit.VsExtension.Settings
{
    using System.IO;

    internal class ProjectSettingsStore : SettingsStore
    {
        public ProjectSettingsStore(ProjectInfo project)
            : base(GetPath(project), "ProjectPersonal", $"{project.Name}, Personal")
        {
        }

        private static string GetPath(ProjectInfo project)
        {
            return Path.ChangeExtension(project.FullName, "EtkSettings.user");
        }
    }
}
