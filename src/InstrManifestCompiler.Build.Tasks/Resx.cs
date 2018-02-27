using System.Threading;

namespace InstrManifestCompiler.Build.Tasks
{
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute(@"Resx.tt", "0")]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
    internal static class Strings
    {
        private static global::System.Resources.ResourceManager resourceMan;

        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Resources.ResourceManager ResourceManager
        {
            get
            {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("InstrManifestCompiler.Build.Tasks.Strings", typeof(Strings).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }

        /// <summary>
        ///   Returns the formatted resource string.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        private static string GetResourceString(string key, params string[] tokens)
        {
            var culture = Thread.CurrentThread.CurrentCulture;
            var str = ResourceManager.GetString(key, culture) ?? string.Empty;
            for (int i = 0; i + 1 < tokens.Length; i += 2)
                str = str.Replace(tokens[i], tokens[i + 1]);
            return str;
        }

        public static string Error_MissingFile
        {
            get { return GetResourceString("Error_MissingFile"); }
        }

        public static string General_InvalidValue
        {
            get { return GetResourceString("General_InvalidValue"); }
        }

        public static string Native_TrackingCommandMessage
        {
            get { return GetResourceString("Native_TrackingCommandMessage"); }
        }

        public static string TrackedToolTask_RebuildingAllSourcesCommandLineChanged
        {
            get { return GetResourceString("TrackedToolTask_RebuildingAllSourcesCommandLineChanged"); }
        }

        public static string TrackedToolTask_RebuildingDueToInvalidTLog
        {
            get { return GetResourceString("TrackedToolTask_RebuildingDueToInvalidTLog"); }
        }

        public static string TrackedToolTask_RebuildingDueToInvalidTLogContents
        {
            get { return GetResourceString("TrackedToolTask_RebuildingDueToInvalidTLogContents"); }
        }

        public static string TrackedToolTask_RebuildingNoCommandTLog
        {
            get { return GetResourceString("TrackedToolTask_RebuildingNoCommandTLog"); }
        }

        public static string TrackedToolTask_RebuildingSourceCommandLineChanged
        {
            get { return GetResourceString("TrackedToolTask_RebuildingSourceCommandLineChanged"); }
        }

        public static class Names
        {
            public const string Error_MissingFile = "Error_MissingFile";
            public const string General_InvalidValue = "General_InvalidValue";
            public const string Native_TrackingCommandMessage = "Native_TrackingCommandMessage";
            public const string TrackedToolTask_RebuildingAllSourcesCommandLineChanged = "TrackedToolTask_RebuildingAllSourcesCommandLineChanged";
            public const string TrackedToolTask_RebuildingDueToInvalidTLog = "TrackedToolTask_RebuildingDueToInvalidTLog";
            public const string TrackedToolTask_RebuildingDueToInvalidTLogContents = "TrackedToolTask_RebuildingDueToInvalidTLogContents";
            public const string TrackedToolTask_RebuildingNoCommandTLog = "TrackedToolTask_RebuildingNoCommandTLog";
            public const string TrackedToolTask_RebuildingSourceCommandLineChanged = "TrackedToolTask_RebuildingSourceCommandLineChanged";
        }
    }
}
