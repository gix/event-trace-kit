namespace EventManifestCompiler.Build.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    ///   Base class for tool tasks tracked by <see cref="FileTracker"/>.
    /// </summary>
    public abstract class TrackedToolTask : ToolTask
    {
        private readonly EventWaitHandle cancelEvent;
        private readonly TaskLoggingHelper logPrivate;

        private string pathToLog;
        private string trackerLogDirectory;

        public enum CommandLineFormat
        {
            ForBuildLog,
            ForTracking
        }

        protected TrackedToolTask()
            : base(Strings.ResourceManager)
        {
            CancelEventName = "MSBuildConsole_CancelEvent" + Guid.NewGuid().ToString("N");
            cancelEvent = new EventWaitHandle(false, EventResetMode.AutoReset, CancelEventName);
            logPrivate = new TaskLoggingHelper(this);
            logPrivate.TaskResources = Strings.ResourceManager;
            logPrivate.HelpKeywordPrefix = "MSBuild.";
        }

        public bool TrackFileAccess { get; set; }
        public bool MinimalRebuildFromTracking { get; set; }
        public string ToolArchitecture { get; set; }
        public string TrackerFrameworkPath { get; set; }
        public string TrackerSdkPath { get; set; }
        public ITaskItem TLogCommandFile { get; set; }
        public ITaskItem[] TLogReadFiles { get; set; }
        public ITaskItem[] TLogWriteFiles { get; set; }
        public ITaskItem[] ExcludedInputPaths { get; set; }
        public ITaskItem[] TrackedInputFilesToIgnore { get; set; }
        public ITaskItem[] TrackedOutputFilesToIgnore { get; set; }

        [Output]
        public ITaskItem[] SourcesCompiled { get; set; }

        [Output]
        public bool SkippedExecution { get; set; }

        public virtual string[] AcceptableNonzeroExitCodes { get; set; }

        public virtual bool AttributeFileTracking
        {
            get { return false; }
        }

        public bool TrackCommandLines { get; set; } = true;

        protected abstract string[] ReadTLogNames { get; }
        protected abstract string[] WriteTLogNames { get; }
        protected abstract string CommandTLogName { get; }
        protected abstract ITaskItem[] TrackedInputFiles { get; }

        protected virtual ExecutableType? ToolType
        {
            get { return null; }
        }

        protected CanonicalTrackedInputFiles SourceDependencies { get; set; }
        protected CanonicalTrackedOutputFiles SourceOutputs { get; set; }
        protected string RootSource { get; set; }
        protected string CancelEventName { get; private set; }

        protected virtual bool UseMinimalRebuildOptimization
        {
            get { return false; }
        }

        protected virtual bool MaintainCompositeRootingMarkers
        {
            get { return false; }
        }

        protected override Encoding ResponseFileEncoding
        {
            get { return Encoding.Unicode; }
        }

        protected override MessageImportance StandardErrorLoggingImportance
        {
            get { return MessageImportance.High; }
        }

        protected override MessageImportance StandardOutputLoggingImportance
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

        public string AdditionalOptions { get; set; }

        public override bool Execute()
        {
            bool ret = base.Execute();
            cancelEvent.Close();
            return ret;
        }

        public override void Cancel()
        {
            cancelEvent.Set();
        }

        protected override bool SkipTaskExecution()
        {
            return ComputeOutOfDateSources();
        }

        protected override void LogPathToTool(string toolName, string pathToTool)
        {
            base.LogPathToTool(toolName, pathToLog);
        }

        protected override bool HandleTaskExecutionErrors()
        {
            return IsAcceptableReturnValue() || base.HandleTaskExecutionErrors();
        }

        protected override bool ValidateParameters()
        {
            return !logPrivate.HasLoggedErrors && !Log.HasLoggedErrors;
        }

        protected override int ExecuteTool(
            string toolPath, string responseFileCommands, string commandLineCommands)
        {
            int exitCode = 0;
            try {
                exitCode = TrackerExecuteTool(toolPath, responseFileCommands, commandLineCommands);
            } finally {
                exitCode = PostExecuteTool(exitCode);
            }
            return exitCode;
        }

        protected virtual int PostExecuteTool(int exitCode)
        {
            if (!MinimalRebuildFromTracking && !TrackFileAccess)
                return exitCode;

            SourceOutputs = new CanonicalTrackedOutputFiles(TLogWriteFiles);
            SourceDependencies = new CanonicalTrackedInputFiles(
                TLogReadFiles,
                TrackedInputFiles,
                ExcludedInputPaths,
                SourceOutputs,
                false,
                MaintainCompositeRootingMarkers);

            IDictionary<string, string> sourcesToCommandLines = MapSourcesToCommandLines();
            if (exitCode != 0) {
                SourceOutputs.RemoveEntriesForSource(SourcesCompiled);
                SourceOutputs.SaveTlog();
                SourceDependencies.RemoveEntriesForSource(SourcesCompiled);
                SourceDependencies.SaveTlog();
                if (TrackCommandLines) {
                    if (MaintainCompositeRootingMarkers) {
                        sourcesToCommandLines.Remove(
                            FileTracker.FormatRootingMarker(SourcesCompiled));
                    } else {
                        foreach (ITaskItem item in SourcesCompiled) {
                            sourcesToCommandLines.Remove(
                                FileTracker.FormatRootingMarker(item));
                        }
                    }
                    WriteSourcesToCommandLinesTable(sourcesToCommandLines);
                }
            } else {
                AddTaskSpecificOutputs(SourcesCompiled, SourceOutputs);
                RemoveTaskSpecificOutputs(SourceOutputs);
                SourceOutputs.RemoveDependenciesFromEntryIfMissing(SourcesCompiled);

                string[] roots = null;
                if (MaintainCompositeRootingMarkers) {
                    roots = SourceOutputs.RemoveRootsWithSharedOutputs(SourcesCompiled);
                    foreach (string marker in roots)
                        SourceDependencies.RemoveEntryForSourceRoot(marker);
                }

                if (TrackedOutputFilesToIgnore != null && (TrackedOutputFilesToIgnore.Length > 0)) {
                    var trackedOutputFilesToRemove = new Dictionary<string, ITaskItem>(
                        StringComparer.OrdinalIgnoreCase);
                    foreach (ITaskItem item in TrackedOutputFilesToIgnore)
                        trackedOutputFilesToRemove.Add(item.GetMetadata("FullPath"), item);
                    SourceOutputs.SaveTlog(
                        fullTrackedPath => !trackedOutputFilesToRemove.ContainsKey(fullTrackedPath));
                } else
                    SourceOutputs.SaveTlog();

                FileUtilities.DeleteEmptyFile(TLogWriteFiles);
                RemoveTaskSpecificInputs(SourceDependencies);
                SourceDependencies.RemoveDependenciesFromEntryIfMissing(SourcesCompiled);
                if (TrackedInputFilesToIgnore != null && TrackedInputFilesToIgnore.Length > 0) {
                    var trackedInputFilesToRemove = new Dictionary<string, ITaskItem>(
                        StringComparer.OrdinalIgnoreCase);
                    foreach (ITaskItem item in TrackedInputFilesToIgnore)
                        trackedInputFilesToRemove.Add(item.GetMetadata("FullPath"), item);
                    SourceDependencies.SaveTlog(
                        fullTrackedPath => !trackedInputFilesToRemove.ContainsKey(fullTrackedPath));
                } else
                    SourceDependencies.SaveTlog();

                FileUtilities.DeleteEmptyFile(TLogReadFiles);
                if (MaintainCompositeRootingMarkers) {
                    sourcesToCommandLines[FileTracker.FormatRootingMarker(SourcesCompiled)] =
                        GenerateCommandLine(CommandLineFormat.ForTracking);
                    if (roots != null) {
                        foreach (string root in roots)
                            sourcesToCommandLines.Remove(root);
                    }
                } else {
                    string cmdLine = GenerateCommandLineExceptSources(
                        CommandLineFormat.ForTracking);
                    foreach (ITaskItem item in SourcesCompiled) {
                        sourcesToCommandLines[FileTracker.FormatRootingMarker(item)] =
                            cmdLine + " " + item.GetMetadata("FullPath").ToUpperInvariant();
                    }
                }

                WriteSourcesToCommandLinesTable(sourcesToCommandLines);
            }

            return exitCode;
        }

        protected override string GenerateCommandLineCommands()
        {
            return GenerateCommandLineCommands(CommandLineFormat.ForBuildLog);
        }

        protected override string GenerateResponseFileCommands()
        {
            return GenerateResponseFileCommands(CommandLineFormat.ForBuildLog);
        }

        protected override string GenerateFullPathToTool()
        {
            if (ToolName == null)
                throw new InvalidOperationException("ToolName must not be null.");
            return ToolName;
        }

        protected bool IsAcceptableReturnValue()
        {
            if (AcceptableNonzeroExitCodes == null)
                return false;

            return AcceptableNonzeroExitCodes.Any(
                str => ExitCode == Convert.ToInt32(str, CultureInfo.InvariantCulture));
        }

        protected int TrackerExecuteTool(
            string toolPath, string responseFileCommands, string commandLineCommands)
        {
            bool trackFileAccess = TrackFileAccess;
            string command = Environment.ExpandEnvironmentVariables(toolPath);
            string rspFileCommands = responseFileCommands;
            string arguments = Environment.ExpandEnvironmentVariables(commandLineCommands);

            string rspFile = null;
            try {
                string trackerPath;
                string dllName = null;
                pathToLog = command;
                if (trackFileAccess) {
                    var toolType = ExecutableType.SameAsCurrentProcess;
                    if (!string.IsNullOrEmpty(ToolArchitecture)) {
                        if (!Enum.TryParse(ToolArchitecture, out toolType)) {
                            Log.LogErrorWithCodeFromResources(
                                nameof(Strings.General_InvalidValue),
                                "ToolArchitecture",
                                GetType().Name);
                            return -1;
                        }
                    } else if (ToolType.HasValue) {
                        toolType = ToolType.Value;
                    }

                    try {
                        trackerPath = FileTracker.GetTrackerPath(toolType, TrackerSdkPath);
                        if (trackerPath == null)
                            Log.LogErrorFromResources(
                                nameof(Strings.Error_MissingFile),
                                "tracker.exe");
                    } catch (Exception ex) when (!ExceptionHandling.NotExpectedException(ex)) {
                        Log.LogErrorWithCodeFromResources(
                            nameof(Strings.General_InvalidValue),
                            "TrackerSdkPath",
                            GetType().Name);
                        return -1;
                    }

                    try {
                        dllName = FileTracker.GetFileTrackerPath(
                            toolType, TrackerFrameworkPath);
                    } catch (Exception ex) when (!ExceptionHandling.NotExpectedException(ex)) {
                        Log.LogErrorWithCodeFromResources(
                            nameof(Strings.General_InvalidValue),
                            "TrackerFrameworkPath",
                            GetType().Name);
                        return -1;
                    }
                } else {
                    trackerPath = command;
                }

                if (string.IsNullOrEmpty(trackerPath))
                    return -1;

                ErrorUtilities.VerifyThrowInternalRooted(trackerPath);
                string cmdLineCommands;
                if (trackFileAccess) {
                    string trackerArgs = FileTracker.TrackerArguments(
                        command, arguments, dllName,
                        TrackerIntermediateDirectory, RootSource,
                        CancelEventName);

                    Log.LogMessageFromResources(
                        MessageImportance.Low,
                        nameof(Strings.Native_TrackingCommandMessage));
                    Log.LogMessage(
                        MessageImportance.Low,
                        trackerPath + (AttributeFileTracking ? " /a " : " ") +
                            trackerArgs + " " + rspFileCommands);

                    rspFile = FileUtilities.GetTemporaryFile();
                    using (var writer = CreateUnicodeWriter(rspFile)) {
                        writer.Write(FileTracker.TrackerResponseFileArguments(
                            dllName, TrackerIntermediateDirectory, RootSource, CancelEventName));
                    }

                    cmdLineCommands =
                        (AttributeFileTracking ? "/a @\"" : "@\"") + rspFile +
                        "\"" + FileTracker.TrackerCommandArguments(command, arguments);
                } else {
                    cmdLineCommands = arguments;
                }

                return base.ExecuteTool(trackerPath, rspFileCommands, cmdLineCommands);
            } finally {
                if (rspFile != null)
                    DeleteTempFile(rspFile);
            }
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

        protected void WriteSourcesToCommandLinesTable(
            IDictionary<string, string> sourcesToCommandLines)
        {
            using var writer = CreateUnicodeWriter(TLogCommandFile.GetMetadata("FullPath"));
            foreach (KeyValuePair<string, string> pair in sourcesToCommandLines) {
                writer.WriteLine("^" + pair.Key);
                writer.WriteLine(ApplyPrecompareCommandFilter(pair.Value));
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
                    nameof(Strings.TrackedToolTask_RebuildingDueToInvalidTLog), ex.Message);

                return true;
            }

            if (!File.Exists(path)) {
                Log.LogMessageFromResources(
                    MessageImportance.Low,
                    nameof(Strings.TrackedToolTask_RebuildingNoCommandTLog),
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
                        nameof(Strings.TrackedToolTask_RebuildingAllSourcesCommandLineChanged));
                } else {
                    foreach (ITaskItem item in sourcesWithChangedCommandLines) {
                        Log.LogMessageFromResources(
                            MessageImportance.Low,
                            nameof(Strings.TrackedToolTask_RebuildingSourceCommandLineChanged),
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
                    nameof(Strings.TrackedToolTask_RebuildingAllSourcesCommandLineChanged));
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
                if (!itemMap.TryGetValue(item, out var commandLineChanged))
                    continue;

                outOfDateSources.Add(item);
                if (commandLineChanged)
                    Log.LogMessageFromResources(
                        MessageImportance.Low,
                        nameof(Strings.TrackedToolTask_RebuildingSourceCommandLineChanged),
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
                SourceOutputs = new CanonicalTrackedOutputFiles(this, TLogWriteFiles);
                SourceDependencies = new CanonicalTrackedInputFiles(
                    this,
                    TLogReadFiles,
                    TrackedInputFiles,
                    ExcludedInputPaths,
                    SourceOutputs,
                    UseMinimalRebuildOptimization,
                    MaintainCompositeRootingMarkers);

                ITaskItem[] sourcesOutOfDateThroughTracking =
                    SourceDependencies.ComputeSourcesNeedingCompilation(false);
                List<ITaskItem> sourcesWithChangedCommandLines =
                    GenerateSourcesOutOfDateDueToCommandLine();

                SourcesCompiled = MergeOutOfDateSourceLists(
                    sourcesOutOfDateThroughTracking,
                    sourcesWithChangedCommandLines);

                if (SourcesCompiled.Length == 0) {
                    SkippedExecution = true;
                    return SkippedExecution;
                }

                SourcesCompiled = AssignOutOfDateSources(SourcesCompiled);
                SourceDependencies.RemoveEntriesForSource(SourcesCompiled);
                SourceDependencies.SaveTlog();
                SourceOutputs.RemoveEntriesForSource(SourcesCompiled);
                SourceOutputs.SaveTlog();
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

        protected IDictionary<string, string> MapSourcesToCommandLines()
        {
            var sourceMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            string metadata = TLogCommandFile.GetMetadata("FullPath");
            if (!File.Exists(metadata))
                return sourceMap;

            using var reader = File.OpenText(metadata);
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
                    nameof(Strings.TrackedToolTask_RebuildingDueToInvalidTLogContents),
                    metadata);
                sourceMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            return sourceMap;
        }

        public virtual string ApplyPrecompareCommandFilter(string value)
        {
            return Regex.Replace(value, @"(\r?\n)?(\r?\n)+", "$1");
        }

        protected virtual string GenerateCommandLineCommands(CommandLineFormat format)
        {
            return string.Empty;
        }

        protected abstract string GenerateResponseFileCommands(CommandLineFormat format);

        protected internal string GenerateCommandLine(CommandLineFormat format = 0)
        {
            string cmdLineCommands = GenerateCommandLineCommands(format);
            string rspFileCommands = GenerateResponseFileCommands(format);
            if (!string.IsNullOrEmpty(cmdLineCommands))
                return (cmdLineCommands + " " + rspFileCommands);
            return rspFileCommands;
        }

        protected static string EnsureTrailingSlash(string directoryName)
        {
            ErrorUtilities.VerifyThrow(directoryName != null, "InternalError");

            if (!string.IsNullOrEmpty(directoryName)) {
                char c = directoryName[directoryName.Length - 1];
                if (c != Path.DirectorySeparatorChar &&
                    c != Path.AltDirectorySeparatorChar)
                    directoryName += Path.DirectorySeparatorChar;
            }

            return directoryName;
        }

        protected virtual List<ITaskItem> GenerateSourcesOutOfDateDueToCommandLine()
        {
            IDictionary<string, string> sourceMap = MapSourcesToCommandLines();
            var outOfDateItems = new List<ITaskItem>();
            if (sourceMap.Count == 0) {
                outOfDateItems.AddRange(TrackedInputFiles);
                return outOfDateItems;
            }

            if (MaintainCompositeRootingMarkers) {
                string currCmdLine = ApplyPrecompareCommandFilter(
                    GenerateCommandLine(CommandLineFormat.ForTracking));

                if (sourceMap.TryGetValue(FileTracker.FormatRootingMarker(TrackedInputFiles), out var prevCmdLine)) {
                    prevCmdLine = ApplyPrecompareCommandFilter(prevCmdLine);
                    if (prevCmdLine == null || !currCmdLine.Equals(prevCmdLine, StringComparison.Ordinal))
                        outOfDateItems.AddRange(TrackedInputFiles);
                } else {
                    outOfDateItems.AddRange(TrackedInputFiles);
                }
            } else {
                string cmdLine = GenerateCommandLineExceptSources(CommandLineFormat.ForTracking);
                foreach (ITaskItem item in TrackedInputFiles) {
                    string currCmdLine = ApplyPrecompareCommandFilter(
                        cmdLine + " " + item.GetMetadata("FullPath").ToUpperInvariant());
                    if (sourceMap.TryGetValue(FileTracker.FormatRootingMarker(item), out var prevCmdLine)) {
                        prevCmdLine = ApplyPrecompareCommandFilter(prevCmdLine);
                        if (prevCmdLine == null || !currCmdLine.Equals(prevCmdLine, StringComparison.Ordinal))
                            outOfDateItems.Add(item);
                    } else {
                        outOfDateItems.Add(item);
                    }
                }
            }
            return outOfDateItems;
        }

        protected abstract string GenerateCommandLineExceptSources(CommandLineFormat format);

        protected void BuildAdditionalArgs(CommandLineBuilder cmdLine)
        {
            if (cmdLine != null && !string.IsNullOrEmpty(AdditionalOptions))
                cmdLine.AppendSwitch(
                    Environment.ExpandEnvironmentVariables(AdditionalOptions));
        }

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
