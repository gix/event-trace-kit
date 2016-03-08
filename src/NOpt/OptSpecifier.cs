namespace NOpt
{
    using System;
    using System.Diagnostics;

    [DebuggerDisplay("Opt({id})")]
    public struct OptSpecifier
    {
        private readonly int id;

        private OptSpecifier(int id)
        {
            this.id = id;
        }

        public int Id
        {
            get { return id; }
        }

        public bool IsValid
        {
            get { return id > 0; }
        }

        public static implicit operator OptSpecifier(int id)
        {
            return new OptSpecifier(id);
        }

        public static implicit operator OptSpecifier(Enum id)
        {
            return new OptSpecifier(Convert.ToInt32(id));
        }
    }
}
