namespace NOpt
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using NOpt.Extensions;

    [DebuggerDisplay("[{Id}] {Name} ({Kind})")]
    public abstract class Option
    {
        private readonly string[] prefixes;
        private readonly string name;
        private readonly int groupId;
        private readonly int aliasId;

        private OptTable owner;

        protected Option(
            OptSpecifier id,
            string prefix,
            string name,
            OptSpecifier? aliasId = null,
            OptSpecifier? groupId = null,
            string helpText = null,
            string metaVar = null,
            int flags = 0)
        {
            Contract.Requires<ArgumentException>(id.IsValid);
            Contract.Requires<ArgumentNullException>(prefix != null);
            Contract.Requires<ArgumentException>(!string.IsNullOrWhiteSpace(prefix));
            Contract.Requires<ArgumentNullException>(name != null);

            Id = id.Id;
            prefixes = new[] { prefix };
            this.name = name;
            HelpText = helpText;
            MetaVar = metaVar;
            this.aliasId = aliasId.GetValueOrDefault().Id;
            this.groupId = groupId.GetValueOrDefault().Id;
            Flags = flags;
        }

        protected Option(
            OptSpecifier id,
            string[] prefixes,
            string name,
            OptSpecifier? aliasId = null,
            OptSpecifier? groupId = null,
            string helpText = null,
            string metaVar = null,
            int flags = 0)
        {
            Contract.Requires<ArgumentException>(id.IsValid);
            Contract.Requires<ArgumentNullException>(prefixes != null);
            Contract.Requires<ArgumentException>(prefixes.Length > 0);
            Contract.Requires<ArgumentException>(!prefixes.Any(string.IsNullOrWhiteSpace));
            Contract.Requires<ArgumentNullException>(name != null);

            Id = id.Id;
            this.prefixes = (string[])prefixes.Clone();
            this.name = name;
            this.aliasId = aliasId.GetValueOrDefault().Id;
            this.groupId = groupId.GetValueOrDefault().Id;
            HelpText = helpText;
            MetaVar = metaVar;
            Flags = flags;
        }

        internal Option(
            OptSpecifier id,
            string name,
            OptSpecifier? aliasId = null,
            OptSpecifier? groupId = null,
            string helpText = null,
            string metaVar = null)
        {
            Contract.Requires<ArgumentException>(id.IsValid);
            Contract.Requires<ArgumentNullException>(name != null);

            Id = id.Id;
            prefixes = new string[0];
            this.name = name;
            this.aliasId = aliasId.GetValueOrDefault().Id;
            this.groupId = groupId.GetValueOrDefault().Id;
            HelpText = helpText;
            MetaVar = metaVar;
        }

        internal void InitializeOwner(OptTable newOwner)
        {
            Contract.Requires<ArgumentNullException>(newOwner != null);
            if (owner != null)
                throw new InvalidOperationException("Option already has an owning OptTable.");
            owner = newOwner;
        }

        /// <summary>Gets the unique option id.</summary>
        public int Id { get; }

        public OptionKind Kind { get; internal set; }

        /// <summary>
        ///   Gets the main prefix of the option, or an empty <see cref="string"/>
        ///   if the option has no prefixes.
        /// </summary>
        public string Prefix
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);
                return prefixes.Length != 0 ? prefixes[0] : string.Empty;
            }
        }

        /// <summary>Gets the name of the option.</summary>
        public string Name
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);
                return name;
            }
        }

        /// <summary>
        ///   Gets the prefixed name.
        /// </summary>
        /// <remarks>
        ///   This is a convenience property that combines <see cref="Prefix"/>
        ///   and <see cref="Name"/>.
        /// </remarks>
        public string PrefixedName
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);
                return Prefix + Name;
            }
        }

        /// <summary>
        ///   Gets the option flags. These may include flags defined by
        ///   <see cref="OptionFlag"/> or custom flags.
        /// </summary>
        public int Flags { get; }

        /// <summary>
        ///   Gets the optional help text. May be <see langword="null"/>.
        /// </summary>
        public string HelpText { get; }

        /// <summary>
        ///   Gets the optional meta variable. May be <see langword="null"/>.
        /// </summary>
        public string MetaVar { get; }

        /// <summary>
        ///   Gets the list of prefixes. Returns an empty list if the option has
        ///   no prefixes.
        /// </summary>
        public IReadOnlyList<string> Prefixes
        {
            get
            {
                Contract.Ensures(Contract.Result<IReadOnlyList<string>>() != null);
                return prefixes;
            }
        }

        public Option Alias => owner.GetOption(aliasId);

        public Option Group => owner.GetOption(groupId);

        public Option UnaliasedOption
        {
            get
            {
                Contract.Ensures(Contract.Result<Option>() != null);
                Option alias = Alias;
                if (alias != null)
                    return alias.UnaliasedOption;
                return this;
            }
        }

        protected virtual OptionRenderStyle RenderStyle
        {
            get
            {
                if (((OptionFlag)Flags & OptionFlag.RenderJoined) != 0)
                    return OptionRenderStyle.Joined;
                if (((OptionFlag)Flags & OptionFlag.RenderSeparate) != 0)
                    return OptionRenderStyle.Separate;

                return OptionRenderStyle.Values;
            }
        }

        public virtual bool Matches(OptSpecifier opt)
        {
            Option alias = Alias;
            if (alias != null)
                return alias.Matches(opt);

            if (opt.Id == Id)
                return true;

            Option group = Group;
            if (group != null)
                return group.Matches(opt);

            return false;
        }

        internal Arg Accept(IReadOnlyList<string> args, ref int argIndex)
        {
            Contract.Requires(argIndex >= 0 && argIndex < args.Count);
            Contract.Ensures(argIndex >= Contract.OldValue(argIndex));

            string argStr = args[argIndex];
            int argLen = 0;
            foreach (var prefix in prefixes) {
                if (argStr.StartsWith(prefix, StringComparison.Ordinal) &&
                    argStr.StartsWith(name, prefix.Length, StringComparison.Ordinal)) {
                    argLen = prefix.Length + name.Length;
                    break;
                }
            }

            if (argLen == 0)
                return null;

            return AcceptCore(args, ref argIndex, argLen);
        }

        public virtual string GetHelpName(string defaultMetaVar)
        {
            return PrefixedName;
        }

        protected virtual Arg AcceptCore(
            IReadOnlyList<string> args,
            ref int argIndex,
            int argLen)
        {
            Contract.Requires(args != null);
            Contract.Requires(argIndex >= 0 && argIndex < args.Count);
            Contract.Ensures(argIndex >= Contract.OldValue(argIndex));

            string argStr = args[argIndex];
            return new Arg(this, argStr, argIndex++, argStr);
        }

        protected internal virtual void RenderArg(Arg arg, IList<string> output)
        {
            switch (RenderStyle) {
                case OptionRenderStyle.Values:
                    output.AddRange(arg.Values);
                    break;

                case OptionRenderStyle.CommaJoined: {
                        var builder = new StringBuilder(256);
                        builder.Append(arg.Spelling);
                        for (int i = 0; i < arg.Values.Count; ++i) {
                            if (i != 0) builder.Append(',');
                            builder.Append(arg.GetValue(i));
                        }
                        output.Add(builder.ToString());
                        break;
                    }

                case OptionRenderStyle.Joined:
                    output.Add(arg.Spelling + arg.GetValue());
                    for (int i = 1; i < arg.Values.Count; ++i)
                        output.Add(arg.GetValue(i));
                    break;

                case OptionRenderStyle.Separate:
                    output.Add(arg.Spelling);
                    for (int i = 0, e = arg.Values.Count; i != e; ++i)
                        output.Add(arg.GetValue(i));
                    break;
            }
        }

        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture, "[{0}] {1}", Id, GetHelpName("<value>"));
        }
    }
}
