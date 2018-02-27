namespace NOpt
{
    using System;
    using System.Diagnostics.Contracts;

    public sealed class WriteHelpSettings
    {
        private string indentChars;
        private string defaultMetaVarName;
        private string defaultHelpGroup;
        private int maxLineLength;
        private int nameColumnWidth;

        public WriteHelpSettings()
        {
            IndentChars = "  ";
            NameColumnWidth = 30;
            MaxLineLength = 80;
            DefaultMetaVar = "<value>";
            DefaultHelpGroup = "Options";
        }

        public string IndentChars
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);
                return indentChars;
            }
            set
            {
                Contract.Requires<ArgumentNullException>(value != null);
                Contract.Requires<ArgumentException>(value.IndexOf('\r') == -1);
                Contract.Requires<ArgumentException>(value.IndexOf('\n') == -1);
                indentChars = value;
            }
        }

        public int NameColumnWidth
        {
            get
            {
                Contract.Ensures(Contract.Result<int>() >= 0);
                return nameColumnWidth;
            }
            set
            {
                Contract.Requires<ArgumentOutOfRangeException>(value >= 0);
                nameColumnWidth = value;
            }
        }

        public int MaxLineLength
        {
            get
            {
                Contract.Ensures(Contract.Result<int>() >= 0);
                return maxLineLength;
            }
            set
            {
                Contract.Requires<ArgumentOutOfRangeException>(value >= 0);
                maxLineLength = value;
            }
        }

        public string DefaultMetaVar
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);
                return defaultMetaVarName;
            }
            set
            {
                Contract.Requires<ArgumentNullException>(value != null);
                defaultMetaVarName = value;
            }
        }

        public int FlagsToInclude { get; set; }
        public int FlagsToExclude { get; set; }

        public string DefaultHelpGroup
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);
                return defaultHelpGroup;
            }
            set
            {
                Contract.Requires<ArgumentNullException>(value != null);
                defaultHelpGroup = value;
            }
        }
    }
}
