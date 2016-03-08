namespace NOpt
{
    /// <summary>
    ///   Specifies the kind of an <see cref="Option"/>.
    /// </summary>
    public enum OptionKind
    {
        /// <summary>Not a special option.</summary>
        None = 0,

        /// <summary>An unknown option not matched by anything.</summary>
        Unknown,

        /// <summary>An option group.</summary>
        Group,

        /// <summary>An input option with a value but no prefix.</summary>
        Input,
    }
}
