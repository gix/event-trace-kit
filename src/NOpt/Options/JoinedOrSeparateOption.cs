namespace NOpt
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;

    /// <summary>
    ///   An option with a prefix and a value. The value and prefix are either
    ///   joined or separated. This kind is used for options that can behave
    ///   either like <see cref="JoinedOption"/> or <see cref="SeparateOption"/>
    ///   (e.g., <c>-Ipath</c> or <c>-I path</c>).
    /// </summary>
    public class JoinedOrSeparateOption : SeparateOption
    {
        public JoinedOrSeparateOption(
            OptSpecifier id,
            string prefix,
            string name,
            string helpText = null,
            OptSpecifier? aliasId = null,
            OptSpecifier? groupId = null,
            string metaVar = null)
            : base(id, prefix, name, helpText: helpText, metaVar: metaVar,
                   aliasId: aliasId, groupId: groupId)
        {
            Contract.Requires<ArgumentException>(id.IsValid);
            Contract.Requires<ArgumentNullException>(prefix != null);
            Contract.Requires<ArgumentException>(!string.IsNullOrWhiteSpace(prefix));
            Contract.Requires<ArgumentNullException>(name != null);
        }

        public JoinedOrSeparateOption(
            OptSpecifier id,
            string[] prefixes,
            string name,
            string helpText = null,
            OptSpecifier? aliasId = null,
            OptSpecifier? groupId = null,
            string metaVar = null)
            : base(id, prefixes, name, helpText: helpText, metaVar: metaVar,
                   aliasId: aliasId, groupId: groupId)
        {
            Contract.Requires<ArgumentException>(id.IsValid);
            Contract.Requires<ArgumentNullException>(prefixes != null);
            Contract.Requires<ArgumentException>(prefixes.Length > 0);
            Contract.Requires<ArgumentException>(!prefixes.Any(string.IsNullOrWhiteSpace));
            Contract.Requires<ArgumentNullException>(name != null);
        }

        protected override OptionRenderStyle RenderStyle
        {
            get
            {
                if (((OptionFlag)Flags & OptionFlag.RenderJoined) != 0)
                    return OptionRenderStyle.Joined;
                if (((OptionFlag)Flags & OptionFlag.RenderSeparate) != 0)
                    return OptionRenderStyle.Separate;

                return OptionRenderStyle.Joined;
            }
        }

        public override string GetHelpName(string defaultMetaVar)
        {
            return PrefixedName + ' ' + (MetaVar ?? defaultMetaVar);
        }

        protected override Arg AcceptCore(
            IReadOnlyList<string> args, ref int argIndex, int argLen)
        {
            string argStr = args[argIndex];
            if (argLen == argStr.Length)
                return base.AcceptCore(args, ref argIndex, argLen);

            Option unaliasedOption = UnaliasedOption;
            string spelling = (Id == unaliasedOption.Id)
                ? argStr.Substring(0, argLen)
                : unaliasedOption.PrefixedName;

            string value = argStr.Substring(argLen);
            return new Arg(unaliasedOption, spelling, argIndex++, value);
        }
    }
}
