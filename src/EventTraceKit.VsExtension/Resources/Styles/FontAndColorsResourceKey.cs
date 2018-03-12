namespace EventTraceKit.VsExtension.Styles
{
    using System;

    public enum FontAndColorsResourceKeyType
    {
        ForegroundColor,
        BackgroundColor,
        ForegroundBrush,
        BackgroundBrush,
        FontFamily,
        FontSize
    }

    public sealed class FontAndColorsResourceKey
    {
        public FontAndColorsResourceKey(
            Guid category, string name, FontAndColorsResourceKeyType keyType)
        {
            var isFontKey = keyType == FontAndColorsResourceKeyType.FontFamily ||
                            keyType == FontAndColorsResourceKeyType.FontSize;
            if (!isFontKey && name == null)
                throw new ArgumentNullException(nameof(name));

            Category = category;
            Name = name;
            KeyType = keyType;
        }

        public Guid Category { get; }

        public FontAndColorsResourceKeyType KeyType { get; }

        public string Name { get; }

        public override bool Equals(object obj)
        {
            if (!(obj is FontAndColorsResourceKey key))
                return false;

            return
                Name == key.Name &&
                Category == key.Category &&
                KeyType == key.KeyType;
        }

        public override int GetHashCode()
        {
            return (Name?.GetHashCode() ?? Category.GetHashCode()) ^ (int)KeyType;
        }
    }
}
