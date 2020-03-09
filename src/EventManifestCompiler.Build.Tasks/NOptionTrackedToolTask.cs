namespace EventManifestCompiler.Build.Tasks
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using NOption;

    /// <summary>Base class for NOption-based tool tasks.</summary>
    public abstract class NOptionTrackedToolTask : TrackedToolTask
    {
        private readonly OptTable optTable;
        private readonly Dictionary<int, Arg> activeArgs = new Dictionary<int, Arg>();

        /// <summary>
        ///   Initializes a new instance of the <see cref="NOptionTrackedToolTask"/> class.
        /// </summary>
        protected NOptionTrackedToolTask(OptTable optTable)
        {
            this.optTable = optTable;
        }

        protected abstract List<OptSpecifier> OptionOrder { get; }

        protected virtual OptSpecifier? SourcesOption => null;

        protected override string GenerateResponseFileCommands(CommandLineFormat format)
        {
            return GenerateResponseFileCommandsExceptSwitches(new OptSpecifier[0], format);
        }

        protected string GenerateCommandLineExcept(
            OptSpecifier[] excludeOpts, CommandLineFormat format = 0)
        {
            string cmdLineCommands = GenerateCommandLineCommands(format);
            string rspFileCommands = GenerateResponseFileCommandsExceptSwitches(excludeOpts, format);
            if (!string.IsNullOrEmpty(cmdLineCommands))
                return cmdLineCommands + " " + rspFileCommands;
            return rspFileCommands;
        }

        protected override string GenerateCommandLineExceptSources(CommandLineFormat format)
        {
            OptSpecifier[] excludeOpts;
            if (SourcesOption != null)
                excludeOpts = new[] { SourcesOption.Value };
            else
                excludeOpts = new OptSpecifier[0];

            return GenerateCommandLineExcept(excludeOpts, format);
        }

        protected virtual string GenerateResponseFileCommandsExceptSwitches(
            OptSpecifier[] switchesToRemove, CommandLineFormat format = 0)
        {
            PostProcessOptions();

            var builder = new CommandLineBuilder(true);
            foreach (OptSpecifier opt in OptionOrder) {
                if (IsOptionSet(opt)) {
                    Arg arg = activeArgs[opt.Id];
                    if (switchesToRemove == null || !switchesToRemove.Any(o => opt.Id == o.Id))
                        GenerateCommandsAccordingToType(builder, arg);
                }
            }
            BuildAdditionalArgs(builder);
            return builder.ToString();
        }

        private void GenerateCommandsAccordingToType(
            CommandLineBuilder builder, Arg arg)
        {
            var list = new List<string>();
            arg.RenderAsInput(list);
            foreach (var str in list)
                builder.AppendSwitch(str);
        }

        protected virtual void PostProcessOptions()
        {
        }

        protected bool IsOptionSet(OptSpecifier opt)
        {
            return activeArgs.ContainsKey(opt.Id);
        }

        protected bool GetBool(OptSpecifier pos, OptSpecifier neg)
        {
            if (IsOptionSet(pos))
                return true;
            if (IsOptionSet(neg))
                return false;
            return false;
        }

        protected bool GetBool(OptSpecifier pos)
        {
            return IsOptionSet(pos);
        }

        protected void SetBool(OptSpecifier pos, OptSpecifier neg, bool value)
        {
            activeArgs.Remove(pos.Id);
            activeArgs.Remove(neg.Id);
            AddActiveArg(CreateArg(pos, neg, value));
        }

        protected void SetBool(OptSpecifier pos, bool value)
        {
            activeArgs.Remove(pos.Id);
            if (value)
                AddActiveArg(CreateArg(pos));
        }

        protected string GetString(OptSpecifier opt)
        {
            if (!IsOptionSet(opt))
                return null;
            return activeArgs[opt.Id].Value;
        }

        protected void SetString(OptSpecifier opt, string value)
        {
            activeArgs.Remove(opt.Id);
            AddActiveArg(CreateArg(opt, value));
        }

        protected string[] GetStringList(OptSpecifier opt)
        {
            if (!IsOptionSet(opt))
                return null;
            return activeArgs[opt.Id].Values.ToArray();
        }

        protected void SetStringList(OptSpecifier opt, string[] value)
        {
            activeArgs.Remove(opt.Id);
            AddActiveArg(CreateArg(opt, value));
        }

        protected ITaskItem GetTaskItem(OptSpecifier opt)
        {
            if (!IsOptionSet(opt))
                return null;
            return new TaskItem(activeArgs[opt.Id].Value);
        }

        protected void SetTaskItem(OptSpecifier opt, ITaskItem value)
        {
            activeArgs.Remove(opt.Id);
            AddActiveArg(CreateArg(opt, value?.ItemSpec));
        }

        private void AddActiveArg(Arg arg)
        {
            activeArgs.Add(arg.Option.Id, arg);
        }

        private Arg CreateArg(OptSpecifier pos)
        {
            var option = optTable.GetOption(pos);
            return new Arg(option, option.PrefixedName, 0);
        }

        private Arg CreateArg(OptSpecifier opt, string value)
        {
            var option = optTable.GetOption(opt);
            return new Arg(option, option.PrefixedName, 0, value);
        }

        private Arg CreateArg(OptSpecifier opt, string[] value)
        {
            var option = optTable.GetOption(opt);
            return new Arg(option, option.PrefixedName, 0, value ?? new string[0]);
        }

        private Arg CreateArg(OptSpecifier pos, OptSpecifier neg, bool value)
        {
            var option = optTable.GetOption(value ? pos : neg);
            return new Arg(option, option.PrefixedName, 0);
        }
    }
}
