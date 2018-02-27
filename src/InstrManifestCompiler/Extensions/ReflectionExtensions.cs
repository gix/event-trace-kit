namespace InstrManifestCompiler.Extensions
{
    using System.Linq;
    using System.Reflection;

    /// <summary>Reflection extensions.</summary>
    internal static class ReflectionExtensions
    {
        /// <summary>
        ///   Returns all custom attributes of type <typeparamref name="T"/>,
        ///   optionally searching the providers's inheritance chain.
        /// </summary>
        /// <typeparam name="T">
        ///   The type of attribute to search for. Only attributes that are assignable
        ///   to this type are returned.
        /// </typeparam>
        /// <param name="provider">
        ///   The <see cref="ICustomAttributeProvider"/> whose attributes are
        ///   searched.
        /// </param>
        /// <param name="inherit">
        ///   When <see langword="true"/>, look up the hierarchy chain for the
        ///   inherited custom attribute. Defaults to <see langword="false"/>.
        /// </param>
        /// <returns>
        ///   An array of custom attribute of type <typeparamref name="T"/> defined
        ///   on this provider, or an empty array if no such attributes are defined.
        /// </returns>
        /// <exception cref="T:System.TypeLoadException">
        ///   The custom attribute type cannot be loaded.
        /// </exception>
        public static T[] GetCustomAttributes<T>(
            this ICustomAttributeProvider provider, bool inherit = false)
        {
            return provider.GetCustomAttributes(typeof(T), inherit) as T[];
        }

        /// <summary>
        ///   Returns the first custom attribute of type <typeparamref name="T"/>,
        ///   optionally searching the providers's inheritance chain.
        /// </summary>
        /// <typeparam name="T">
        ///   The type of attribute to search for. Only attributes that are assignable
        ///   to this type are returned.
        /// </typeparam>
        /// <param name="provider">
        ///   The <see cref="ICustomAttributeProvider"/> whose attributes are
        ///   searched.
        /// </param>
        /// <param name="inherit">
        ///   When <see langword="true"/>, look up the hierarchy chain for the
        ///   inherited custom attribute. Defaults to <see langword="false"/>.
        /// </param>
        /// <returns>
        ///   A custom attribute of type <typeparamref name="T"/> defined on
        ///   this provider, or <see langword="null"/> if no such attribute is
        ///   defined.
        /// </returns>
        /// <exception cref="T:System.TypeLoadException">
        ///   The custom attribute type cannot be loaded.
        /// </exception>
        public static T GetCustomAttribute<T>(
            this ICustomAttributeProvider provider, bool inherit = false)
        {
            return provider.GetCustomAttributes<T>(inherit).FirstOrDefault();
        }

        /// <summary>
        ///   Determines whether a custom attribute of type <typeparamref name="T"/>
        ///   is defined on the specified attribute provider, optionally searching
        ///   the provider's inheritance chain.
        /// </summary>
        /// <typeparam name="T">
        ///   The type of attribute to search for. Only attributes that are assignable
        ///   to this type are returned.
        /// </typeparam>
        /// <param name="provider">
        ///   The <see cref="ICustomAttributeProvider"/> whose attributes are
        ///   searched.
        /// </param>
        /// <param name="inherit">
        ///   When <see langword="true"/>, look up the hierarchy chain for the
        ///   inherited custom attribute. Defaults to <see langword="false"/>.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if a custom attribute of type <typeparamref name="T"/>
        ///   is defined on the provider, or <see langword="false"/> otherwise.
        /// </returns>
        /// <exception cref="T:System.TypeLoadException">
        ///   The custom attribute type cannot be loaded.
        /// </exception>
        public static bool HasCustomAttribute<T>(
            this ICustomAttributeProvider provider, bool inherit = false)
        {
            return provider.GetCustomAttributes(typeof(T), inherit).Length > 0;
        }
    }
}
