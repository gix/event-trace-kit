namespace EventTraceKit.VsExtension.Windows
{
    using System.Windows;

    /// <summary>
    ///   <see cref="DependencyProperty"/> extensions.
    /// </summary>
    public static class DependencyPropertyExtensions
    {
        /// <summary>
        ///   Overrides the metadata that existed for the dependency property
        ///   as it was inherited from base types with a default instance of
        ///   <see cref="FrameworkPropertyMetadata"/> for the specified type
        ///   <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">
        ///   The type where this dependency property is inherited and where
        ///   the provided alternate metadata will be applied.
        /// </typeparam>
        /// <param name="d">
        ///   The <see cref="DependencyProperty"/> whose metadata should be
        ///   overridden.
        /// </param>
        /// <exception cref="System.InvalidOperationException">
        ///   An attempt was made to override metadata on a read-only
        ///   dependency property (that operation cannot be done using this
        ///   signature).
        /// </exception>
        /// <exception cref="System.ArgumentException">
        ///   Metadata was already established for the dependency property
        ///   as it exists on the provided type.
        /// </exception>
        public static void OverrideMetadata<T>(this DependencyProperty d)
        {
            d.OverrideMetadata(typeof(T), new FrameworkPropertyMetadata(typeof(T)));
        }
    }
}
