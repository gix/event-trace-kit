namespace NOpt
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.Linq;

    /// <summary>Represents a parsed commandline argument.</summary>
    public sealed class Arg
    {
        private readonly Option option;
        private readonly string spelling;
        private readonly int index;
        private readonly List<string> values;

        public Arg(Option option, string spelling, int index)
        {
            Contract.Requires<ArgumentNullException>(option != null);
            Contract.Requires<ArgumentNullException>(spelling != null);
            Contract.Requires<ArgumentOutOfRangeException>(index >= 0);

            this.option = option;
            this.spelling = spelling;
            this.index = index;
            values = new List<string>();
        }

        public Arg(Option option, string spelling, int index, string value)
            : this(option, spelling, index)
        {
            Contract.Requires<ArgumentNullException>(option != null);
            Contract.Requires<ArgumentNullException>(spelling != null);
            Contract.Requires<ArgumentOutOfRangeException>(index >= 0);

            values = new List<string>(1) { value };
        }

        public Arg(Option option, string spelling, int index, IEnumerable<string> values)
            : this(option, spelling, index)
        {
            Contract.Requires<ArgumentNullException>(option != null);
            Contract.Requires<ArgumentNullException>(spelling != null);
            Contract.Requires<ArgumentOutOfRangeException>(index >= 0);
            Contract.Requires<ArgumentNullException>(values != null);

            this.values = new List<string>(values);
        }

        /// <summary>Gets the commandline option for the argument.</summary>
        public Option Option
        {
            get
            {
                Contract.Ensures(Contract.Result<Option>() != null);
                return option;
            }
        }

        /// <summary>Gets the index of the argument.</summary>
        public int Index
        {
            get
            {
                Contract.Ensures(Contract.Result<int>() >= 0);
                return index;
            }
        }

        /// <summary>
        ///   Gets the spelling of the argument. The spelling includes the actual
        ///   used prefix and name of the corresponding <see cref="Option"/>.
        /// </summary>
        public string Spelling
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);
                return spelling;
            }
        }

        /// <summary>
        ///   Gets a value indicating whether the argument has been claimed.
        /// </summary>
        /// <remarks>
        ///   Claiming can be used to identify unused arguments. Arguments are
        ///   claimed either by directly calling <see cref="Claim"/> or indirectly
        ///   when querying an <see cref="IArgumentList"/>.
        /// </remarks>
        public bool IsClaimed { get; private set; }

        public string Value
        {
            get
            {
                if (values.Count == 0)
                    return null;
                return values[0];
            }
        }

        public IReadOnlyList<string> Values
        {
            get
            {
                Contract.Ensures(Contract.Result<IReadOnlyList<string>>() != null);
                return values;
            }
        }

        public string GetValue(int index = 0)
        {
            Contract.Requires<ArgumentOutOfRangeException>(index >= 0);
            if (values.Count == 0)
                return null;
            return values[index];
        }

        /// <summary>
        ///   Claims the argument.
        /// </summary>
        /// <remarks>
        ///   Claiming can be used to identify unused arguments. Arguments are
        ///   claimed either by directly calling <see cref="Claim"/> or indirectly
        ///   when querying an <see cref="IArgumentList"/>.
        /// </remarks>
        public void Claim()
        {
            IsClaimed = true;
        }

        public string GetAsString()
        {
            var parts = new List<string>();
            Option.RenderArg(this, parts);
            return string.Join(" ", parts);
        }

        public void RenderAsInput(List<string> output)
        {
            Option.RenderArg(this, output);
            //if (!Option.HasNoOptAsInput()) {
            //    Option.RenderArg(this, output);
            //    return;
            //}

            //foreach (var value in Values)
            //    output.Add(value);
        }

        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0} (Index={1}, OptionId={2}, Spelling={3}, Values={4})",
                GetAsString(), Index, Option.Id, Spelling,
                string.Join(";", Values.Select(s => s ?? "<null>")));
        }
    }
}
