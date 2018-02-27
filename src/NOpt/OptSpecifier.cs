namespace NOpt
{
    using System;
    using System.Diagnostics;

    [DebuggerDisplay("Opt({" + nameof(Id) + "})")]
    public struct OptSpecifier
    {
        private OptSpecifier(int id)
        {
            Id = id;
        }

        public int Id { get; }

        public bool IsValid => Id > 0;

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
