namespace InstrManifestCompiler.EventManifestSchema
{
    using System.Diagnostics;

    [DebuggerDisplay("{Name}(0x{Id,h}) = '{Text}'")]
    internal sealed class Message
    {
        public const uint UnusedId = uint.MaxValue;

        public Message()
        {
            Id = UnusedId;
        }

        public Message(uint id, string text)
        {
            Id = id;
            Text = text;
        }

        public Message(string name, uint id, string text)
        {
            Name = name;
            Id = id;
            Text = text;
        }

        public string Name { get; set; }
        public uint Id { get; set; }
        public string Text { get; set; }
        public bool Ansi { get; set; }

        public bool IsUsed => Id != UnusedId;
    }
}
