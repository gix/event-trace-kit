namespace EventTraceKit.VsExtension.Filtering
{
    using System;

    public class FilterRelation : IEquatable<FilterRelation>
    {
        public FilterRelation(string displayName, FilterRelationKind kind)
        {
            DisplayName = displayName;
            Kind = kind;
        }

        public string DisplayName { get; }
        public FilterRelationKind Kind { get; }

        public bool Equals(FilterRelation other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(DisplayName, other.DisplayName) && Kind == other.Kind;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as FilterRelation);
        }

        public override int GetHashCode()
        {
            unchecked {
                return ((DisplayName?.GetHashCode() ?? 0) * 397) ^ (int)Kind;
            }
        }

        public static bool operator ==(FilterRelation left, FilterRelation right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(FilterRelation left, FilterRelation right)
        {
            return !Equals(left, right);
        }
    }
}
