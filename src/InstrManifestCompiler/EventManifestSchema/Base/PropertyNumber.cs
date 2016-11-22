namespace InstrManifestCompiler.EventManifestSchema.Base
{
    public interface IPropertyNumber
    {
        string DataPropertyRef { get; set; }
        int DataPropertyIndex { get; set; }
        DataProperty DataProperty { get; set; }
        ushort? Value { get; set; }
        string Name { get; }

        bool IsFixed { get; }
        bool IsVariable { get; }
        bool IsSpecified { get; }
    }

    internal abstract class PropertyNumber : IPropertyNumber
    {
        public string DataPropertyRef { get; set; }
        public int DataPropertyIndex { get; set; } = -1;
        public DataProperty DataProperty { get; set; }
        public ushort? Value { get; set; }
        public abstract string Name { get; }

        public virtual bool IsFixed => Value.HasValue;

        public bool IsVariable => DataPropertyRef != null || DataPropertyIndex != -1;

        public bool IsSpecified => IsFixed || IsVariable;

        public override string ToString()
        {
            if (IsVariable)
                return DataPropertyRef;
            if (Value.HasValue)
                return Value.Value.ToString();
            return string.Empty;
        }
    }

    internal sealed class Count : PropertyNumber
    {
        public override string Name => "count";

        public override bool IsFixed
        {
            get { return Value.GetValueOrDefault(1) > 1; }
        }
    }

    internal sealed class Length : PropertyNumber
    {
        public override string Name => "length";

        public override bool IsFixed
        {
            get { return Value.GetValueOrDefault() > 0; }
        }
    }
}
