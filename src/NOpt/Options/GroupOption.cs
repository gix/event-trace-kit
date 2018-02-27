namespace NOpt
{
    using System;
    using System.Diagnostics.Contracts;

    /// <summary>An option group.</summary>
    public sealed class GroupOption : Option
    {
        public GroupOption(
            OptSpecifier id,
            string name,
            string helpText = null,
            string metaVar = null)
            : base(id, name, helpText: helpText, metaVar: metaVar)
        {
            Contract.Requires<ArgumentException>(id.IsValid);
            Contract.Requires<ArgumentNullException>(name != null);
            Kind = OptionKind.Group;
        }

        public override string GetHelpName(string defaultMetaVar)
        {
            throw new Exception("Invalid option with help text.");
        }
    }
}
