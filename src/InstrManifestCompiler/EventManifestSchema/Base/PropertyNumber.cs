namespace InstrManifestCompiler.EventManifestSchema.Base
{
    public interface IPropertyNumber
    {
        string DataPropertyRef { get; }
        int DataPropertyIndex { get; }
        DataProperty DataProperty { get; }
        ushort? Value { get; }
        string Name { get; }

        void SetFixed(ushort value);
        void SetVariable(int? refPropertyIndex = null, string refPropertyName = null, DataProperty refProperty = null);

        bool IsFixed { get; }
        bool IsVariable { get; }
        bool IsSpecified { get; }
    }

    internal abstract class PropertyNumber : IPropertyNumber
    {
        public string DataPropertyRef { get; private set; }
        public int DataPropertyIndex { get; private set; } = -1;
        public DataProperty DataProperty { get; private set; }
        public ushort? Value { get; private set; }
        public abstract string Name { get; }

        public virtual bool IsFixed => Value.HasValue;
        public bool IsVariable => DataPropertyRef != null || DataPropertyIndex != -1;
        public bool IsSpecified => IsFixed || IsVariable;

        public void SetFixed(ushort value)
        {
            Reset();
            Value = value;
        }

        public void SetVariable(int? refPropertyIndex, string refPropertyName, DataProperty refProperty)
        {
            Reset();
            DataPropertyIndex = refPropertyIndex ?? -1;
            DataPropertyRef = refPropertyName;
            DataProperty = refProperty;
        }

        private void Reset()
        {
            Value = null;
            DataPropertyRef = null;
            DataPropertyIndex = -1;
            DataProperty = null;
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

    internal sealed class Count : PropertyNumber
    {
        public override string Name => "count";

        public override bool IsFixed => base.IsFixed && Value.GetValueOrDefault(1) > 1;
    }

    internal sealed class Length : PropertyNumber
    {
        public override string Name => "length";

        public override bool IsFixed => base.IsFixed && Value.GetValueOrDefault() > 0;
    }
}
