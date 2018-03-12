namespace EventTraceKit.VsExtension.Filtering
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;

    public interface IModelProperty
    {
        string Name { get; }
        IEnumerable<FilterRelation> Relations { get; }
        Expression Expression { get; }
        ValueHolder CreateValue();
    }

    public abstract class ExpressionProperty<T> : IModelProperty
    {
        protected ExpressionProperty(
            string name, IEnumerable<FilterRelation> relations, Expression expression)
        {
            Name = name;
            Expression = expression;
            Relations = relations;
        }

        public string Name { get; }
        public IEnumerable<FilterRelation> Relations { get; }
        public Expression Expression { get; }

        public ValueHolder CreateValue()
        {
            return CreateValueCore();
        }

        protected virtual ValueHolder<T> CreateValueCore()
        {
            return new ValueHolder<T>();
        }
    }

    public class NumericProperty<T> : ExpressionProperty<T>
    {
        public NumericProperty(
            string name, IEnumerable<FilterRelation> relations, Expression expression)
            : base(name, relations, expression)
        {
        }
    }

    public class GuidProperty : ExpressionProperty<Guid>
    {
        public GuidProperty(
            string name, IEnumerable<FilterRelation> relations, Expression expression)
            : base(name, relations, expression)
        {
        }
    }

    public class EnumProperty<T> : ExpressionProperty<T>
    {
        public EnumProperty(
            string name, IEnumerable<FilterRelation> relations, Expression expression)
            : base(name, relations, expression)
        {
        }
    }

    public abstract class ValueHolder
    {
        public abstract object RawValue { get; set; }
    }

    public class ValueHolder<T> : ValueHolder
    {
        public T Value { get; set; }
        public override object RawValue
        {
            get => Value;
            set => Value = (T)value;
        }
    }
}
