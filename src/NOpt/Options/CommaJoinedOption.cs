namespace NOpt
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Linq;

    /// <summary>
    ///   An option with a prefix and a variable number of values. The first
    ///   value and prefix are joined and each value is separated with a comma.
    ///   This kind is used for options like <c>--opt=value1,value2,value3</c>.
    /// </summary>
    public class CommaJoinedOption : Option
    {
        public CommaJoinedOption(
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

        public CommaJoinedOption(
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

        public override string GetHelpName(string defaultMetaVar)
        {
            return PrefixedName + (MetaVar ?? defaultMetaVar);
        }
    }
}
