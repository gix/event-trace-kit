namespace InstrManifestCompiler.Collections
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics.Contracts;
    using System.Linq;

    public abstract class ConstrainedEntityCollection<T>
        : Collection<T>, IUniqueEntityList<T>
    {
        private readonly List<IUniqueConstraint<T>> uniqueConstraints =
            new List<IUniqueConstraint<T>>();

        public bool IsUnique(T entity)
        {
            return uniqueConstraints.All(c => c.IsSatisfiedBy(entity));
        }

        public bool IsUnique(T entity, IDiagnostics diags)
        {
            bool unique = true;
            foreach (var constraint in uniqueConstraints) {
                if (!constraint.IsSatisfiedBy(entity, diags))
                    unique = false;
            }

            return unique;
        }

        public bool TryAdd(T entity)
        {
            if (!IsUnique(entity))
                return false;
            Add(entity);
            return true;
        }

        public bool TryAdd(T entity, IDiagnostics diags)
        {
            if (!IsUnique(entity, diags))
                return false;
            Add(entity);
            return true;
        }

        protected IUniqueConstraintOptions<T, TProperty>
            UniqueConstraintFor<TProperty>(Func<T, TProperty> selector)
        {
            Contract.Requires<ArgumentNullException>(selector != null);

            var constraint = new UniqueConstraint<T, TProperty>(selector);
            uniqueConstraints.Add(constraint);
            return constraint;
        }

        protected override void InsertItem(int index, T item)
        {
            foreach (var constraint in uniqueConstraints) {
                if (!constraint.IsSatisfiedBy(item))
                    throw CreateDuplicateEntityException(item, constraint);
            }

            foreach (var constraint in uniqueConstraints)
                constraint.NotifyAdd(item);

            base.InsertItem(index, item);
        }

        protected override void SetItem(int index, T newEntity)
        {
            T oldEntity = this[index];
            foreach (var constraint in uniqueConstraints) {
                if (constraint.Changed(oldEntity, newEntity) &&
                    !constraint.IsSatisfiedBy(newEntity))
                    throw CreateDuplicateEntityException(newEntity, constraint);
            }

            foreach (var constraint in uniqueConstraints)
                constraint.NotifyAdd(newEntity);

            base.SetItem(index, newEntity);
        }

        protected override void RemoveItem(int index)
        {
            T entity = this[index];
            foreach (var constraint in uniqueConstraints)
                constraint.NotifyRemove(entity);

            base.RemoveItem(index);
        }

        private Exception CreateDuplicateEntityException(T entity, IUniqueConstraint<T> constraint)
        {
            string message = constraint.FormatMessage(entity);
            return new DuplicateEntityException(message);
        }
    }
}
