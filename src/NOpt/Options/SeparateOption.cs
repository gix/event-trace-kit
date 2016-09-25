namespace NOpt
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;

    /// <summary>
    ///   An option with a prefix and a value. The prefix and value are separated.
    ///   This kind is used for options like <c>-I value</c>, <c>--opt value</c>,
    ///   <c>/out value</c>.
    /// </summary>
    public class SeparateOption : Option
    {
        public SeparateOption(
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

        public SeparateOption(
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

                return OptionRenderStyle.Separate;
            }
        }

        protected override Arg AcceptCore(
            IReadOnlyList<string> args, ref int argIndex, int argLen)
        {
            string argStr = args[argIndex];

            Option unaliasedOption = UnaliasedOption;
            string spelling = (Id == unaliasedOption.Id)
                ? argStr.Substring(0, argLen)
                : unaliasedOption.PrefixedName;

            // Require exact match.
            if (argLen != argStr.Length)
                return null;

            argIndex += 2;
            if (argIndex > args.Count)
                return null;

            return new Arg(unaliasedOption, spelling, argIndex - 2, args[argIndex - 1]);
        }

        public override string GetHelpName(string defaultMetaVar)
        {
            return PrefixedName + ' ' + (MetaVar ?? defaultMetaVar);
        }
    }
}
