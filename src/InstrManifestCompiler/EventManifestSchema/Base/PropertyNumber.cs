namespace InstrManifestCompiler.EventManifestSchema.Base
{
    public interface IPropertyNumber
    {
        string DataPropertyRef { get; set; }
        int DataPropertyIndex { get; set; }
        DataProperty DataProperty { get; set; }
        ushort? Value { get; set; }

        bool IsFixed { get; }
        bool IsVariable { get; }
        bool IsSpecified { get; }
    }

    internal sealed class Count : PropertyNumber
    {
        public override bool IsFixed
        {
            get { return Value.GetValueOrDefault(1) > 1; }
        }
    }

    internal sealed class Length : PropertyNumber
    {
        public override bool IsFixed
        {
            get { return Value.GetValueOrDefault() > 0; }
        }
    }

    internal abstract class PropertyNumber : IPropertyNumber
    {
        public string DataPropertyRef { get; set; }
        public int DataPropertyIndex { get; set; }
        public DataProperty DataProperty { get; set; }
        public ushort? Value { get; set; }

        public virtual bool IsFixed
        {
            get { return Value.HasValue; }
        }

        public bool IsVariable
        {
            get { return DataPropertyRef != null; }
        }

        public bool IsSpecified
        {
            get { return IsFixed || IsVariable; }
        }

        public override string ToString()
        {
            if (IsVariable)
                return DataPropertyRef;
            if (Value.HasValue)
                return Value.Value.ToString();
            return string.Empty;
        }
    }
}
