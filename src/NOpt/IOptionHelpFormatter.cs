namespace NOpt
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;

    [ContractClass(typeof(IOptionHelpFormatterContract))]
    public interface IOptionHelpFormatter
    {
        void WriteHelp(IEnumerable<Option> options);
    }

    [ContractClassFor(typeof(IOptionHelpFormatter))]
    internal abstract class IOptionHelpFormatterContract : IOptionHelpFormatter
    {
        void IOptionHelpFormatter.WriteHelp(IEnumerable<Option> options)
        {
            Contract.Requires<ArgumentNullException>(options != null);
        }
    }
}
