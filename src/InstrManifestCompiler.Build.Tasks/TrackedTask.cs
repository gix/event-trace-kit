namespace InstrManifestCompiler.Build.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    ///   Base class for tasks tracked by <see cref="FileTracker"/>.
    /// </summary>
    public abstract class TrackedTask : Task, ICancelableTask
    {
        private readonly EventWaitHandle cancelEvent;
        private readonly TaskLoggingHelper logPrivate;

        private string trackerLogDirectory;
        private MessageImportance standardOutputImportanceToUse;
        private MessageImportance standardErrorImportanceToUse;
        private string standardOutputImportance;
        private string standardErrorImportance;

        public enum OptionFormat
        {
            ForBuildLog,
            ForTracking
        }

        protected TrackedTask()
            : base(Strings.ResourceManager)
        {
            standardOutputImportanceToUse = MessageImportance.Low;
            standardErrorImportanceToUse = MessageImportance.Normal;

            ToolCanceled = new ManualResetEvent(false);
            CancelEventName = "MSBuildConsole_CancelEvent" + Guid.NewGuid().ToString("N");
            cancelEvent = new EventWaitHandle(false, EventResetMode.AutoReset, CancelEventName);
            logPrivate = new TaskLoggingHelper(this);
            logPrivate.TaskResources = Strings.ResourceManager;
            logPrivate.HelpKeywordPrefix = "MSBuild.";
        }

        public bool TrackFileAccess { get; set; }
        public bool MinimalRebuildFromTracking { get; set; }
        public ITaskItem TLogCommandFile { get; set; }
        public ITaskItem[] TLogReadFiles { get; set; }
        public ITaskItem[] TLogWriteFiles { get; set; }
        public ITaskItem[] ExcludedInputPaths { get; set; }
        public ITaskItem[] TrackedInputFilesToIgnore { get; set; }
        public ITaskItem[] TrackedOutputFilesToIgnore { get; set; }
        public ManualResetEvent ToolCanceled { get; private set; }

        [Output]
        public ITaskItem[] SourcesCompiled { get; set; }

        [Output]
        public bool SkippedExecution { get; set; }

        public virtual bool AttributeFileTracking
        {
            get { return false; }
        }

        protected abstract string[] ReadTLogNames { get; }
        protected abstract string[] WriteTLogNames { get; }
        protected abstract string CommandTLogName { get; }
        protected abstract ITaskItem[] TrackedInputFiles { get; }

        protected CanonicalTrackedInputFiles SourceDependencies { get; set; }
        protected string RootSource { get; set; }
        protected string CancelEventName { get; private set; }
        protected abstract string ActionName { get; }

        protected virtual bool UseMinimalRebuildOptimization
        {
            get { return false; }
        }

        protected virtual bool MaintainCompositeRootingMarkers
        {
            get { return false; }
        }

        protected virtual Encoding ResponseFileEncoding
        {
            get { return Encoding.Unicode; }
        }

        protected virtual MessageImportance StandardErrorLoggingImportance
        {
            get { return MessageImportance.High; }
        }

        protected virtual MessageImportance StandardOutputLoggingImportance
        {
            get { return MessageImportance.High; }
        }

        /// <summary>Gets or sets the tracker log directory path.</summary>
        public virtual string TrackerLogDirectory
        {
            get { return trackerLogDirectory; }
            set { trackerLogDirectory = EnsureTrailingSlash(value); }
        }

        protected virtual string TrackerIntermediateDirectory
        {
            get { return TrackerLogDirectory ?? string.Empty; }
        }

        public override bool Execute()
        {
            bool ret = ExecuteCore();
            cancelEvent.Close();
            return ret;
        }

        private bool AssignStandardStreamLoggingImportance()
        {
            if (string.IsNullOrEmpty(standardErrorImportance)) {
                standardErrorImportanceToUse = StandardErrorLoggingImportance;
            } else {
                try {
                    standardErrorImportanceToUse = (MessageImportance)Enum.Parse(
                        typeof(MessageImportance), standardErrorImportance, true);
                } catch (ArgumentException) {
                    Log.LogErrorWithCodeFromResources(
                        "Message.InvalidImportance", standardErrorImportance);
                    return false;
                }
            }

            if (string.IsNullOrEmpty(standardOutputImportance)) {
                standardOutputImportanceToUse = StandardOutputLoggingImportance;
            } else {
                try {
                    standardOutputImportanceToUse = (MessageImportance)Enum.Parse(
                        typeof(MessageImportance), standardOutputImportance, true);
                } catch (ArgumentException) {
                    Log.LogErrorWithCodeFromResources(
                        "Message.InvalidImportance", standardOutputImportance);
                    return false;
                }
            }

            return true;
        }

        private bool ExecuteCore()
        {
            if (!ValidateParameters())
                return false;

            if (!AssignStandardStreamLoggingImportance())
                return false;

            if (SkipTaskExecution())
                return true;

            string options = GenerateOptions();
            if (string.IsNullOrEmpty(options)) {
                options = string.Empty;
            } else {
                options = " " + options;
            }

            bool success = false;
            try {
                try {
                    if (TrackFileAccess) {
                        Log.LogMessageFromResources(
                            MessageImportance.Low,
                            Strings.Names.Native_TrackingCommandMessage);
                        Log.LogMessage(
                            MessageImportance.Low,
                            options);
                    }
                    success = ExecuteAction();
                } finally {
                    FinishExecute(success);
                }
            } catch (Exception ex) {
                logPrivate.LogErrorFromException(ex);
                return false;
            }

            return success;
        }

        public virtual void Cancel()
        {
            ToolCanceled.Set();
            cancelEvent.Set();
        }

        protected virtual bool SkipTaskExecution()
        {
            return ComputeOutOfDateSources();
        }

        protected virtual void LogPathToTool(string toolName, string pathToTool)
        {
            //base.LogPathToTool(toolName, pathToLog);
        }

        protected virtual bool HandleTaskExecutionErrors()
        {
            //if (this.HasLoggedErrors) {
            //    logPrivate.LogMessageFromResources(MessageImportance.Low, "General.ToolCommandFailedNoErrorCode", new object[] { this.exitCode });
            //} else {
            //    logPrivate.LogErrorWithCodeFromResources("ToolTask.ToolCommandFailed", new object[] { this.ToolExe, this.exitCode });
            //}
            return false;
        }

        protected virtual bool ValidateParameters()
        {
            return !logPrivate.HasLoggedErrors && !Log.HasLoggedErrors;
        }

        protected abstract bool ExecuteAction();

        private void FinishExecute(bool success)
        {
            if (!MinimalRebuildFromTracking && !TrackFileAccess)
                return;

            var outputs = new CanonicalTrackedOutputFiles(TLogWriteFiles);
            var compactInputs = new CanonicalTrackedInputFiles(
                TLogReadFiles,
                TrackedInputFiles,
                ExcludedInputPaths,
                outputs,
                false,
                MaintainCompositeRootingMarkers);

            IDictionary<string, string> sourcesToOptions = MapSourcesToOptions();
            if (!success) {
                outputs.RemoveEntriesForSource(SourcesCompiled);
                outputs.SaveTlog();
                compactInputs.RemoveEntriesForSource(SourcesCompiled);
                compactInputs.SaveTlog();
                if (MaintainCompositeRootingMarkers) {
                    sourcesToOptions.Remove(
                        FileTracker.FormatRootingMarker(SourcesCompiled));
                } else {
                    foreach (ITaskItem item in SourcesCompiled) {
                        sourcesToOptions.Remove(
                            FileTracker.FormatRootingMarker(item));
                    }
                }
                WriteSourcesToOptionsTable(sourcesToOptions);
            } else {
                AddTaskSpecificOutputs(SourcesCompiled, outputs);
                RemoveTaskSpecificOutputs(outputs);
                outputs.RemoveDependenciesFromEntryIfMissing(SourcesCompiled);

                string[] roots = null;
                if (MaintainCompositeRootingMarkers) {
                    roots = outputs.RemoveRootsWithSharedOutputs(SourcesCompiled);
                    foreach (string marker in roots)
                        compactInputs.RemoveEntryForSourceRoot(marker);
                }

                if (TrackedOutputFilesToIgnore != null && (TrackedOutputFilesToIgnore.Length > 0)) {
                    var trackedOutputFilesToRemove = new Dictionary<string, ITaskItem>(
                        StringComparer.OrdinalIgnoreCase);
                    foreach (ITaskItem item in TrackedOutputFilesToIgnore)
                        trackedOutputFilesToRemove.Add(item.GetMetadata("FullPath"), item);
                    outputs.SaveTlog(
                        fullTrackedPath => !trackedOutputFilesToRemove.ContainsKey(fullTrackedPath));
                } else
                    outputs.SaveTlog();

                FileUtilities.DeleteEmptyFile(TLogWriteFiles);
                RemoveTaskSpecificInputs(compactInputs);
                compactInputs.RemoveDependenciesFromEntryIfMissing(SourcesCompiled);
                if (TrackedInputFilesToIgnore != null && TrackedInputFilesToIgnore.Length > 0) {
                    var trackedInputFilesToRemove = new Dictionary<string, ITaskItem>(
                        StringComparer.OrdinalIgnoreCase);
                    foreach (ITaskItem item in TrackedInputFilesToIgnore)
                        trackedInputFilesToRemove.Add(item.GetMetadata("FullPath"), item);
                    compactInputs.SaveTlog(
                        fullTrackedPath => !trackedInputFilesToRemove.ContainsKey(fullTrackedPath));
                } else
                    compactInputs.SaveTlog();

                FileUtilities.DeleteEmptyFile(TLogReadFiles);
                if (MaintainCompositeRootingMarkers) {
                    sourcesToOptions[FileTracker.FormatRootingMarker(SourcesCompiled)] =
                        GenerateOptions(OptionFormat.ForTracking);
                    if (roots != null) {
                        foreach (string root in roots)
                            sourcesToOptions.Remove(root);
                    }
                } else {
                    string options = GenerateOptionsExceptSources(OptionFormat.ForTracking);
                    foreach (ITaskItem item in SourcesCompiled) {
                        sourcesToOptions[FileTracker.FormatRootingMarker(item)] =
                            options + " " + item.GetMetadata("FullPath").ToUpperInvariant();
                    }
                }

                WriteSourcesToOptionsTable(sourcesToOptions);
            }
        }

        protected virtual string GenerateOptions()
        {
            return GenerateOptions(OptionFormat.ForBuildLog);
        }

        protected virtual void AddTaskSpecificOutputs(
            ITaskItem[] sources, CanonicalTrackedOutputFiles compactOutputs)
        {
        }

        protected virtual void RemoveTaskSpecificInputs(
            CanonicalTrackedInputFiles compactInputs)
        {
        }

        protected virtual void RemoveTaskSpecificOutputs(
            CanonicalTrackedOutputFiles compactOutputs)
        {
        }

        protected void WriteSourcesToOptionsTable(
            IDictionary<string, string> sourcesToOptions)
        {
            using (var writer = CreateUnicodeWriter(TLogCommandFile.GetMetadata("FullPath"))) {
                foreach (KeyValuePair<string, string> pair in sourcesToOptions) {
                    writer.WriteLine("^" + pair.Key);
                    writer.WriteLine(ApplyPrecompareCommandFilter(pair.Value));
                }
            }
        }

        protected virtual bool ForcedRebuildRequired()
        {
            string path;
            try {
                path = TLogCommandFile.GetMetadata("FullPath");
            } catch (Exception ex) {
                if (!(ex is InvalidOperationException) &&
                    !(ex is NullReferenceException))
                    throw;

                Log.LogWarningWithCodeFromResources(
                    Strings.Names.TrackedToolTask_RebuildingDueToInvalidTLog, ex.Message);

                return true;
            }

            if (!File.Exists(path)) {
                Log.LogMessageFromResources(
                    MessageImportance.Low,
                    Strings.Names.TrackedToolTask_RebuildingNoCommandTLog,
                    TLogCommandFile.GetMetadata("FullPath"));
                return true;
            }

            return false;
        }

        protected virtual void AssignDefaultTLogPaths()
        {
            if (TLogReadFiles == null) {
                TLogReadFiles = new ITaskItem[ReadTLogNames.Length];
                for (int i = 0; i < ReadTLogNames.Length; ++i)
                    TLogReadFiles[i] = CreateIntermediateTaskItem(ReadTLogNames[i]);
            }

            if (TLogWriteFiles == null) {
                TLogWriteFiles = new ITaskItem[WriteTLogNames.Length];
                for (int i = 0; i < WriteTLogNames.Length; ++i)
                    TLogWriteFiles[i] = CreateIntermediateTaskItem(WriteTLogNames[i]);
            }

            if (TLogCommandFile == null)
                TLogCommandFile = CreateIntermediateTaskItem(CommandTLogName);
        }

        protected ITaskItem[] MergeOutOfDateSourceLists(
            ITaskItem[] sourcesOutOfDateThroughTracking,
            List<ITaskItem> sourcesWithChangedCommandLines)
        {
            if (sourcesWithChangedCommandLines.Count == 0)
                return sourcesOutOfDateThroughTracking;

            if (sourcesOutOfDateThroughTracking.Length == 0) {
                if (sourcesWithChangedCommandLines.Count == TrackedInputFiles.Length) {
                    Log.LogMessageFromResources(
                        MessageImportance.Low,
                        Strings.Names.TrackedToolTask_RebuildingAllSourcesCommandLineChanged);
                } else {
                    foreach (ITaskItem item in sourcesWithChangedCommandLines) {
                        Log.LogMessageFromResources(
                            MessageImportance.Low,
                            Strings.Names.TrackedToolTask_RebuildingSourceCommandLineChanged,
                            item.GetMetadata("FullPath"));
                    }
                }
                return sourcesWithChangedCommandLines.ToArray();
            }

            if (sourcesOutOfDateThroughTracking.Length == TrackedInputFiles.Length)
                return TrackedInputFiles;

            if (sourcesWithChangedCommandLines.Count == TrackedInputFiles.Length) {
                Log.LogMessageFromResources(
                    MessageImportance.Low,
                    Strings.Names.TrackedToolTask_RebuildingAllSourcesCommandLineChanged);
                return TrackedInputFiles;
            }

            var itemMap = new Dictionary<ITaskItem, bool>();
            foreach (ITaskItem item in sourcesOutOfDateThroughTracking)
                itemMap[item] = false;

            foreach (ITaskItem item in sourcesWithChangedCommandLines) {
                if (!itemMap.ContainsKey(item))
                    itemMap.Add(item, true);
            }

            var outOfDateSources = new List<ITaskItem>();
            foreach (ITaskItem item in TrackedInputFiles) {
                if (!itemMap.TryGetValue(item, out var optionsChanged))
                    continue;

                outOfDateSources.Add(item);
                if (optionsChanged)
                    Log.LogMessageFromResources(
                        MessageImportance.Low,
                        Strings.Names.TrackedToolTask_RebuildingSourceCommandLineChanged,
                        item.GetMetadata("FullPath"));
            }

            return outOfDateSources.ToArray();
        }

        protected virtual ITaskItem[] AssignOutOfDateSources(ITaskItem[] sources)
        {
            return sources;
        }

        protected internal virtual bool ComputeOutOfDateSources()
        {
            if (MinimalRebuildFromTracking || TrackFileAccess)
                AssignDefaultTLogPaths();

            if (MinimalRebuildFromTracking && !ForcedRebuildRequired()) {
                var outputs = new CanonicalTrackedOutputFiles(this, TLogWriteFiles);
                SourceDependencies = new CanonicalTrackedInputFiles(
                    this,
                    TLogReadFiles,
                    TrackedInputFiles,
                    ExcludedInputPaths,
                    outputs,
                    UseMinimalRebuildOptimization,
                    MaintainCompositeRootingMarkers);

                ITaskItem[] sourcesOutOfDateThroughTracking =
                    SourceDependencies.ComputeSourcesNeedingCompilation(false);
                List<ITaskItem> sourcesWithChangedOptions =
                    GenerateSourcesOutOfDateDueToOptions();

                SourcesCompiled = MergeOutOfDateSourceLists(
                    sourcesOutOfDateThroughTracking,
                    sourcesWithChangedOptions);

                if (SourcesCompiled.Length == 0) {
                    SkippedExecution = true;
                    return SkippedExecution;
                }

                SourcesCompiled = AssignOutOfDateSources(SourcesCompiled);
                SourceDependencies.RemoveEntriesForSource(SourcesCompiled);
                SourceDependencies.SaveTlog();
                outputs.RemoveEntriesForSource(SourcesCompiled);
                outputs.SaveTlog();
            } else {
                SourcesCompiled = TrackedInputFiles;
                if (SourcesCompiled == null || SourcesCompiled.Length == 0) {
                    SkippedExecution = true;
                    return SkippedExecution;
                }
            }

            if (TrackFileAccess)
                RootSource = FileTracker.FormatRootingMarker(SourcesCompiled);

            SkippedExecution = false;
            return SkippedExecution;
        }

        protected IDictionary<string, string> MapSourcesToOptions()
        {
            var sourceMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            string metadata = TLogCommandFile.GetMetadata("FullPath");
            if (!File.Exists(metadata))
                return sourceMap;

            using (var reader = File.OpenText(metadata)) {
                bool invalidTLog = false;
                string source = string.Empty;
                for (string line = reader.ReadLine(); line != null; line = reader.ReadLine()) {
                    if (line.Length == 0) {
                        invalidTLog = true;
                        break;
                    }

                    if (line[0] == '^') {
                        if (line.Length == 1) {
                            invalidTLog = true;
                            break;
                        }
                        source = line.Substring(1);
                    } else {
                        if (!sourceMap.ContainsKey(source))
                            sourceMap[source] = line;
                        else
                            sourceMap[source] += "\r\n" + line;
                    }
                }

                if (invalidTLog) {
                    Log.LogWarningWithCodeFromResources(
                        Strings.Names.TrackedToolTask_RebuildingDueToInvalidTLogContents,
                        metadata);
                    sourceMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                }
            }

            return sourceMap;
        }

        public virtual string ApplyPrecompareCommandFilter(string value)
        {
            return Regex.Replace(value, @"(\r?\n)?(\r?\n)+", "$1");
        }

        protected virtual string GenerateOptions(OptionFormat format)
        {
            return string.Empty;
        }

        protected static string EnsureTrailingSlash(string directoryName)
        {
            ErrorUtilities.VerifyThrow(directoryName != null, "InternalError");

            if (!string.IsNullOrEmpty(directoryName)) {
                char c = directoryName[directoryName.Length - 1];
                if (c != Path.DirectorySeparatorChar &&
                    c != Path.AltDirectorySeparatorChar)
                    directoryName = directoryName + Path.DirectorySeparatorChar;
            }

            return directoryName;
        }

        protected virtual List<ITaskItem> GenerateSourcesOutOfDateDueToOptions()
        {
            IDictionary<string, string> sourceMap = MapSourcesToOptions();
            var outOfDateItems = new List<ITaskItem>();
            if (sourceMap.Count == 0) {
                outOfDateItems.AddRange(TrackedInputFiles);
                return outOfDateItems;
            }

            if (MaintainCompositeRootingMarkers) {
                string currOptions = ApplyPrecompareCommandFilter(
                    GenerateOptions(OptionFormat.ForTracking));

                if (sourceMap.TryGetValue(FileTracker.FormatRootingMarker(TrackedInputFiles), out var prevOptions)) {
                    prevOptions = ApplyPrecompareCommandFilter(prevOptions);
                    if (prevOptions == null || !currOptions.Equals(prevOptions, StringComparison.Ordinal))
                        outOfDateItems.AddRange(TrackedInputFiles);
                } else {
                    outOfDateItems.AddRange(TrackedInputFiles);
                }
            } else {
                string cmdLine = GenerateOptionsExceptSources(OptionFormat.ForTracking);
                foreach (ITaskItem item in TrackedInputFiles) {
                    string currOptions = ApplyPrecompareCommandFilter(
                        cmdLine + " " + item.GetMetadata("FullPath").ToUpperInvariant());
                    if (sourceMap.TryGetValue(FileTracker.FormatRootingMarker(item), out var prevOptions)) {
                        prevOptions = ApplyPrecompareCommandFilter(prevOptions);
                        if (prevOptions == null || !currOptions.Equals(prevOptions, StringComparison.Ordinal))
                            outOfDateItems.Add(item);
                    } else {
                        outOfDateItems.Add(item);
                    }
                }
            }
            return outOfDateItems;
        }

        protected abstract string GenerateOptionsExceptSources(OptionFormat format);

        private TaskItem CreateIntermediateTaskItem(string fileName)
        {
            return new TaskItem(Path.Combine(TrackerIntermediateDirectory, fileName));
        }

        private StreamWriter CreateUnicodeWriter(string path)
        {
            return new StreamWriter(path, false, Encoding.Unicode);
        }
    }
}
