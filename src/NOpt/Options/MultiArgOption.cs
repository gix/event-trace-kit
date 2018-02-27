namespace NOpt
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;

    /// <summary>
    ///   An option with a prefix and a fixed number of values. The values and
    ///   prefix are separated. This kind is used for options like <c>--opt value1
    ///   value2 value3</c>.
    /// </summary>
    public class MultiArgOption : Option
    {
        public MultiArgOption(
            OptSpecifier id,
            string prefix,
            string name,
            int argCount,
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
            Contract.Requires<ArgumentOutOfRangeException>(argCount > 0);
            ArgCount = argCount;
        }

        public MultiArgOption(
            OptSpecifier id,
            string[] prefixes,
            string name,
            int argCount,
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
            Contract.Requires<ArgumentOutOfRangeException>(argCount > 0);
            ArgCount = argCount;
        }

        public int ArgCount { get; }

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

        public override string GetHelpName(string defaultMetaVar)
        {
            throw new Exception("Cannot print metavar for this kind of option.");
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

            argIndex += 1 + ArgCount;
            if (argIndex > args.Count)
                return null;

            return new Arg(
                unaliasedOption,
                spelling,
                argIndex - 1 - ArgCount,
                args.Skip(argIndex - ArgCount).Take(ArgCount));
        }
    }
}
