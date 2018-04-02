namespace EventTraceKit.VsExtension.Settings
{
    using System;
    using System.Linq;

    internal class SettingsServiceImpl : ISettingsService
    {
        private readonly IVsSolutionManager solutionManager;
        private readonly string appDataDirectory;

        private readonly ISettingsStore globalStore;
        private ISettingsStore ambientStore;

        public SettingsServiceImpl(
            IVsSolutionManager solutionManager, string appDataDirectory)
        {
            this.solutionManager = solutionManager;
            this.appDataDirectory = appDataDirectory;

            globalStore = new GlobalSettingsStore(appDataDirectory);

            UpdateAmbientStore(solutionManager.EnumerateStartupProjects().FirstOrDefault());
            solutionManager.StartupProjectChanged += OnStartupProjectChanged;
        }

        public event EventHandler SettingsLayerChanged;

        private void UpdateAmbientStore(ProjectInfo startupProject)
        {
            if (startupProject == null)
                ambientStore = globalStore;
            else
                ambientStore = new ProjectSettingsStore(startupProject);
        }

        private void OnStartupProjectChanged(object sender, StartupProjectChangedEventArgs args)
        {
            ambientStore.Save();
            UpdateAmbientStore(args.Projects.FirstOrDefault());
            SettingsLayerChanged?.Invoke(this, EventArgs.Empty);
        }

        public ISettingsStore GetGlobalStore()
        {
            return globalStore;
        }

        public ISettingsStore GetAmbientStore()
        {
            return ambientStore;
        }

        public ISettingsStore GetProjectStore(ProjectInfo project)
        {
            return new ProjectSettingsStore(project);
        }

        public void SaveAmbient()
        {
            ambientStore.Save();
        }
    }
}
