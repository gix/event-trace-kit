namespace EventManifestFramework.Collections
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using EventManifestFramework.Support;

    public abstract class UniqueCollection<T>
        : Collection<T>, IUniqueList<T>
    {
        private readonly List<IUniqueConstraint<T>> uniqueConstraints =
            new List<IUniqueConstraint<T>>();

        public bool IsUnique(T item)
        {
            if (default(T) == null && item == null)
                throw new ArgumentNullException(nameof(item));

            return uniqueConstraints.All(c => c.IsSatisfiedBy(this, item));
        }

        public bool IsUnique(T item, IDiagnostics diags)
        {
            if (default(T) == null && item == null)
                throw new ArgumentNullException(nameof(item));
            if (diags == null)
                throw new ArgumentNullException(nameof(diags));

            bool unique = true;
            foreach (var constraint in uniqueConstraints) {
                if (!constraint.IsSatisfiedBy(this, item, diags))
                    unique = false;
            }

            return unique;
        }

        public bool TryAdd(T item)
        {
            if (!IsUnique(item))
                return false;
            Add(item);
            return true;
        }

        public bool TryAdd(T item, IDiagnostics diags)
        {
            if (!IsUnique(item, diags))
                return false;
            Add(item);
            return true;
        }

        public void AddConstraint(IUniqueConstraint<T> constraint, IDiagnostics diags = null)
        {
            if (constraint == null)
                throw new ArgumentNullException(nameof(constraint));

            if (Count != 0 && !Items.All(x => constraint.IsSatisfiedBy(this, x, diags))) {
                throw new InvalidOperationException(
                    "Cannot add new constraint because it is violated by existing items.");
            }

            uniqueConstraints.Add(constraint);
        }

        protected override void InsertItem(int index, T item)
        {
            foreach (var constraint in uniqueConstraints) {
                if (!constraint.IsSatisfiedBy(this, item))
                    throw CreateDuplicateItemException(item, constraint);
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
                    !constraint.IsSatisfiedBy(this, newEntity))
                    throw CreateDuplicateItemException(newEntity, constraint);
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

        private Exception CreateDuplicateItemException(
            T entity, IUniqueConstraint<T> constraint)
        {
            string message = constraint.FormatMessage(entity);
            return new DuplicateItemException(message);
        }
    }
}
