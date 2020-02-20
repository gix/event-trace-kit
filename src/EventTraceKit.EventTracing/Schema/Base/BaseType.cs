namespace EventTraceKit.EventTracing.Schema.Base
{
    public class BaseType
    {
        protected BaseType(QName name, uint value, string symbol)
        {
            Name = name;
            Value = value;
            Symbol = symbol;
        }

        public QName Name { get; }
        public string Symbol { get; }
        public uint Value { get; }

        public override string ToString()
        {
            return Name.ToString();
        }
    }
}
