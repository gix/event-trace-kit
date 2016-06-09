namespace EventTraceKit.VsExtension.UITests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Media;
    using System.Xml.Linq;
    using System.Xml.XPath;
    using EventTraceKit.VsExtension;
    using Microsoft.VisualStudio.Settings;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Colors = EventTraceKit.VsExtension.Colors;
    using Fonts = EventTraceKit.VsExtension.Fonts;

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly List<string> availableThemes = new List<string>();

        public new static App Current => Application.Current as App;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            //ServiceProvider.GlobalProvider

            var setGlobalProvider = typeof(ServiceProvider)
                .GetMethod("SetGlobalProvider", BindingFlags.NonPublic | BindingFlags.Static);

            setGlobalProvider.Invoke(null, new object[] { CreateServiceProvider() });

            var sm = ServiceProvider.GlobalProvider.GetService(typeof(SVsSettingsManager)) as IVsSettingsManager;

            availableThemes.AddRange(FindAvailableThemes());
            TryLoadTheme("VisualStudio.Dark");
        }

        private ServiceProvider CreateServiceProvider()
        {
            var provider = new ServiceProviderStub();
            provider.AddService<SVsSettingsManager>(new VsSettingsManagerStub());
            return new ServiceProvider(provider);
        }

        private class ServiceProviderStub : Microsoft.VisualStudio.OLE.Interop.IServiceProvider
        {
            private readonly Dictionary<Guid, object> services = new Dictionary<Guid, object>();

            public void AddService<TService>(object service)
            {
                AddService(typeof(TService).GUID, service);
            }

            public void AddService(Guid guidService, object service)
            {
                services.Add(guidService, service);
            }

            public int QueryService(ref Guid guidService, ref Guid riid, out IntPtr ppvObject)
            {
                object obj;
                if (services.TryGetValue(guidService, out obj)) {
                    var punk = Marshal.GetIUnknownForObject(obj);
                    return Marshal.QueryInterface(punk, ref riid, out ppvObject);
                }

                ppvObject = new IntPtr();
                return -1;
            }
        }

        private class VsSettingsManagerStub : IVsSettingsManager
        {
            private readonly VsSettingsStoreStub readOnlyStore = new VsSettingsStoreStub();

            public int GetCollectionScopes(string collectionPath, out uint scopes)
            {
                throw new NotImplementedException();
            }

            public int GetPropertyScopes(string collectionPath, string propertyName, out uint scopes)
            {
                throw new NotImplementedException();
            }

            public int GetReadOnlySettingsStore(uint scope, out IVsSettingsStore store)
            {
                if (scope != (uint)SettingsScope.UserSettings) {
                    store = null;
                    return -1;
                }

                store = readOnlyStore;
                return 0;
            }

            public int GetWritableSettingsStore(uint scope, out IVsWritableSettingsStore writableStore)
            {
                throw new NotImplementedException();
            }

            public int GetApplicationDataFolder(uint folder, out string folderPath)
            {
                throw new NotImplementedException();
            }

            public int GetCommonExtensionsSearchPaths(uint paths, string[] commonExtensionsPaths, out uint actualPaths)
            {
                throw new NotImplementedException();
            }
        }

        private class VsSettingsStoreStub : IVsSettingsStore
        {
            public int GetBool(string collectionPath, string propertyName, out int value)
            {
                throw new NotImplementedException();
            }

            public int GetInt(string collectionPath, string propertyName, out int value)
            {
                throw new NotImplementedException();
            }

            public int GetUnsignedInt(string collectionPath, string propertyName, out uint value)
            {
                throw new NotImplementedException();
            }

            public int GetInt64(string collectionPath, string propertyName, out long value)
            {
                throw new NotImplementedException();
            }

            public int GetUnsignedInt64(string collectionPath, string propertyName, out ulong value)
            {
                throw new NotImplementedException();
            }

            public int GetString(string collectionPath, string propertyName, out string value)
            {
                throw new NotImplementedException();
            }

            public int GetBinary(
                string collectionPath,
                string propertyName,
                uint byteLength,
                byte[] pBytes = null,
                uint[] actualByteLength = null)
            {
                throw new NotImplementedException();
            }

            public int GetBoolOrDefault(string collectionPath, string propertyName, int defaultValue, out int value)
            {
                value = defaultValue;
                return 1;
            }

            public int GetIntOrDefault(string collectionPath, string propertyName, int defaultValue, out int value)
            {
                value = defaultValue;
                return 1;
            }

            public int GetUnsignedIntOrDefault(string collectionPath, string propertyName, uint defaultValue, out uint value)
            {
                value = defaultValue;
                return 1;
            }

            public int GetInt64OrDefault(string collectionPath, string propertyName, long defaultValue, out long value)
            {
                value = defaultValue;
                return 1;
            }

            public int GetUnsignedInt64OrDefault(string collectionPath, string propertyName, ulong defaultValue, out ulong value)
            {
                value = defaultValue;
                return 1;
            }

            public int GetStringOrDefault(string collectionPath, string propertyName, string defaultValue, out string value)
            {
                value = defaultValue;
                return 1;
            }

            public int GetPropertyType(string collectionPath, string propertyName, out uint type)
            {
                throw new NotImplementedException();
            }

            public int PropertyExists(string collectionPath, string propertyName, out int pfExists)
            {
                throw new NotImplementedException();
            }

            public int CollectionExists(string collectionPath, out int pfExists)
            {
                throw new NotImplementedException();
            }

            public int GetSubCollectionCount(string collectionPath, out uint subCollectionCount)
            {
                throw new NotImplementedException();
            }

            public int GetPropertyCount(string collectionPath, out uint propertyCount)
            {
                throw new NotImplementedException();
            }

            public int GetLastWriteTime(string collectionPath, SYSTEMTIME[] lastWriteTime)
            {
                throw new NotImplementedException();
            }

            public int GetSubCollectionName(string collectionPath, uint index, out string subCollectionName)
            {
                throw new NotImplementedException();
            }

            public int GetPropertyName(string collectionPath, uint index, out string propertyName)
            {
                throw new NotImplementedException();
            }
        }

        public IReadOnlyList<string> AvailableThemes => availableThemes;

        public string ActiveTheme { get; private set; }

        public bool TryLoadTheme(string name)
        {
            if (ActiveTheme == name)
                return true;

            if (!LoadThemeFromResource(name + ".vstheme"))
                return false;

            UpdateTraceLogTheme();
            ActiveTheme = name;
            return true;
        }

        private List<string> FindAvailableThemes()
        {
            var assembly = GetType().Assembly;
            return assembly.GetManifestResourceNames()
                .Select(GetThemeName).Where(n => n != null)
                .OrderBy(x => x).ToList();
        }

        private string GetThemeName(string resourceName)
        {
            string prefix = typeof(App).Namespace + ".Themes.";
            string suffix = ".vstheme";
            if (!resourceName.StartsWith(prefix) || !resourceName.EndsWith(suffix))
                return null;

            int length = resourceName.Length - prefix.Length - suffix.Length;
            return resourceName.Substring(prefix.Length, length);
        }

        private bool LoadThemeFromResource(string name)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var stream = assembly.GetManifestResourceStream(typeof(App), "Themes." + name);
            if (stream == null)
                return false;

            using (stream)
                LoadThemeFromStream(stream);

            return true;
        }

        private void LoadThemeFromStream(Stream input)
        {
            var doc = XDocument.Load(input);

            foreach (var category in doc.XPathSelectElements("Themes/Theme/Category")) {
                Guid id = Guid.Parse(category.Attribute("GUID").Value);

                foreach (var color in category.Elements("Color")) {
                    string name = color.Attribute("Name")?.Value;

                    var foreground = GetColor(color.Element("Foreground"));
                    if (foreground != null)
                        SetColor(id, name, foreground.Value, true);

                    var background = GetColor(color.Element("Background"));
                    if (background != null) {
                        var backgroundColor = background.Value;
                        if (name.Contains("Selected Text"))
                            backgroundColor.A = 102;
                        SetColor(id, name, backgroundColor, false);
                    }

                    if (foreground == null && name == "Plain Text")
                        SetColor(id, name, SystemColors.ControlTextColor, true);

                    if (background == null && name == "Plain Text")
                        SetColor(id, name, SystemColors.ControlColor, false);
                }
            }
        }

        private void UpdateTraceLogTheme()
        {
            var id = new Guid("9973EFDF-317D-431C-8BC1-5E88CBFD4F7F");
            var foregroundKey = new ThemeResourceKey(id, "Plain Text", ThemeResourceKeyType.ForegroundBrush);
            var backgroundKey = new ThemeResourceKey(id, "Plain Text", ThemeResourceKeyType.BackgroundBrush);
            var selectedBackgroundKey = new ThemeResourceKey(
                id, "Selected Text", ThemeResourceKeyType.BackgroundBrush);
            var inactiveSelectedBackgroundKey = new ThemeResourceKey(
                id, "Inactive Selected Text", ThemeResourceKeyType.BackgroundBrush);

            Resources[Fonts.TraceLogEntryFontFamilyKey] = new FontFamily("Consolas");
            Resources[Fonts.TraceLogEntryFontSizeKey] = 9;
            Resources[Colors.TraceLogForegroundKey] = Resources[foregroundKey];
            Resources[Colors.TraceLogBackgroundKey] = Resources[backgroundKey];
            Resources[Colors.TraceLogBackgroundAltKey] = GetAlternateBrush((SolidColorBrush)Resources[backgroundKey]);
            Resources[Colors.TraceLogSelectedForegroundKey] = Resources[foregroundKey];
            Resources[Colors.TraceLogSelectedBackgroundKey] = Resources[selectedBackgroundKey];
            Resources[Colors.TraceLogInactiveSelectedForegroundKey] = Resources[foregroundKey];
            Resources[Colors.TraceLogInactiveSelectedBackgroundKey] = Resources[inactiveSelectedBackgroundKey];
        }

        private SolidColorBrush GetAlternateBrush(
            SolidColorBrush brush, double amount = 0.03)
        {
            return new SolidColorBrush(GetAlternateColor(brush.Color, amount));
        }

        private Color GetAlternateColor(Color color, double amount = 0.03)
        {
            var hsl = color.ToHslColor();

            bool darken = hsl.Lightness >= 0.5;
            if (darken)
                hsl.Lightness -= amount;
            else
                hsl.Lightness += amount;

            return hsl.ToColor();
        }

        private void SetColor(Guid id, string name, Color color, bool isForeground)
        {
            var colorType = isForeground
                ? ThemeResourceKeyType.ForegroundColor
                : ThemeResourceKeyType.BackgroundColor;

            var brushType = isForeground
                ? ThemeResourceKeyType.ForegroundBrush
                : ThemeResourceKeyType.BackgroundBrush;

            var colorKey = new ThemeResourceKey(id, name, colorType);
            var brushKey = new ThemeResourceKey(id, name, brushType);

            Resources[colorKey] = color;
            Resources[brushKey] = new SolidColorBrush(color);
        }

        [DllImport("User32.dll")]
        private static extern uint GetSysColor(int nIndex);

        private Color? GetColor(XElement element)
        {
            if (element == null)
                return null;

            string type = element.Attribute("Type")?.Value;
            string source = element.Attribute("Source")?.Value;

            switch (type) {
                case "CT_RAW":
                    if (source == null || source.Length != 8)
                        return null;

                    byte a = Convert.ToByte(source.Substring(0, 2), 16);
                    byte r = Convert.ToByte(source.Substring(2, 2), 16);
                    byte g = Convert.ToByte(source.Substring(4, 2), 16);
                    byte b = Convert.ToByte(source.Substring(6, 2), 16);

                    return Color.FromArgb(a, r, g, b);

                case "CT_INVALID":
                    return null;

                case "CT_SYSCOLOR":
                    int index = Convert.ToInt32(source, 16);
                    uint color = GetSysColor(index);
                    a = (byte)((color >> 32) & 0xFF);
                    r = (byte)((color >> 16) & 0xFF);
                    g = (byte)((color >> 8) & 0xFF);
                    b = (byte)((color >> 0) & 0xFF);

                    return Color.FromArgb(a, r, g, b);

                default:
                    return null;
            }
        }
    }
}
