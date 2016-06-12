namespace EventTraceKit.VsExtension
{
    using System;
    using System.Windows;

    public static class FreezableExtensions
    {
        public static T EnsureFrozen<T>(this T freezable)
            where T : Freezable
        {
            if (freezable == null)
                throw new ArgumentNullException(nameof(freezable));

            if (freezable.IsFrozen)
                return freezable;

            if (!freezable.CanFreeze)
                freezable = (T)freezable.CloneCurrentValue();

            freezable.Freeze();
            return freezable;
        }
    }
}
