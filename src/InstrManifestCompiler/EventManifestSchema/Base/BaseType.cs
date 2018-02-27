namespace InstrManifestCompiler.EventManifestSchema.Base
{
    public class BaseType
    {
        protected BaseType(QName name, uint value, string symbol)
        {
            Name = name;
            Value = value;
            Symbol = symbol;
        }

        public QName Name { get; private set; }
        public string Symbol { get; private set; }
        public uint Value { get; private set; }

        public override string ToString()
        {
            return Name.ToString();
        }
    }
}
