namespace InstrManifestCompiler.EventManifestSchema.Base
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Xml;
    using System.Xml.Linq;

    public sealed class QName : IEquatable<QName>
    {
        public QName(string name, string prefix = null, XNamespace ns = null)
        {
            LocalName = name;
            Prefix = prefix;
            Namespace = ns ?? XNamespace.None;
        }

        public string LocalName { get; private set; }
        public string Prefix { get; private set; }
        public XNamespace Namespace { get; private set; }

        /// <summary>
        ///   Indicates whether this instance and a specified <see cref="QName"/>
        ///   are equal.
        /// </summary>
        /// <param name="other">
        ///   An <see cref="QName"/> to compare with this instance or
        ///   <see langword="null"/>.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if <paramref name="other"/> is an instance
        ///   of <see cref="QName"/> and equals the value of this
        ///   instance; otherwise, <see langword="false"/>.
        /// </returns>
        public bool Equals(QName other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return LocalName == other.LocalName && Namespace == other.Namespace;
        }

        /// <summary>
        ///   Indicates whether this instance and a specified object are equal.
        /// </summary>
        /// <param name="obj">
        ///   An object to compare with this instance or <see langword="null"/>.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if <paramref name="obj"/> is an instance of
        ///   <see cref="QName"/> and equals the value of this instance;
        ///   otherwise, <see langword="false"/>.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            return obj is QName && Equals((QName)obj);
        }

        /// <summary>
        ///   Returns the hash code for this instance.
        /// </summary>
        /// <returns>
        ///   A 32-bit signed integer hash code.
        /// </returns>
        public override int GetHashCode()
        {
            unchecked {
                return
                    ((LocalName?.GetHashCode() ?? 0) * 397) ^
                    (Namespace != null ? Namespace.GetHashCode() : 0);
            }
        }

        /// <summary>
        ///   Returns a <see cref="string"/> that represents the current
        ///   <see cref="object"/>.
        /// </summary>
        /// <returns>
        ///   A <see cref="string"/> that represents the current <see cref="QName"/>.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override string ToString()
        {
            if (Namespace == null || string.IsNullOrEmpty(Namespace.NamespaceName))
                return LocalName;
            return $"{Namespace.NamespaceName}:{LocalName}";
        }

        public string ToPrefixedString()
        {
            if (string.IsNullOrEmpty(Prefix))
                return LocalName;
            return $"{Prefix}:{LocalName}";
        }

        /// <summary>
        ///   Determines whether two instances of <see cref="QName"/> are
        ///   equal.
        /// </summary>
        /// <param name="left">
        ///   The first <see cref="QName"/> to compare.
        /// </param>
        /// <param name="right">
        ///   The second <see cref="QName"/> to compare.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if <paramref name="left"/> is equal to
        ///   <paramref name="right"/>; <see langword="false"/> otherwise.
        /// </returns>
        public static bool operator ==(QName left, QName right)
        {
            if (ReferenceEquals(left, right))
                return true;
            if (ReferenceEquals(left, null))
                return false;
            return left.Equals(right);
        }

        /// <summary>
        ///   Determines whether two instances of <see cref="QName"/> are
        ///   <b>not</b> equal.
        /// </summary>
        /// <param name="left">
        ///   The first <see cref="QName"/> to compare.
        /// </param>
        /// <param name="right">
        ///   The second <see cref="QName"/> to compare.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if <paramref name="left"/> is <b>not</b>
        ///   equal to <paramref name="right"/>; <see langword="false"/> otherwise.
        /// </returns>
        public static bool operator !=(QName left, QName right)
        {
            return !(left == right);
        }

        public static QName Parse(string str, IXmlNamespaceResolver xmlns)
        {
            Contract.Requires<ArgumentNullException>(str != null);
            Contract.Requires<ArgumentNullException>(xmlns != null);

            int index = str.IndexOf(':');
            if (index == -1)
                return new QName(str);

            string prefix = str.Substring(0, index);
            string name = str.Substring(index + 1);

            string ns = xmlns.LookupNamespace(prefix);
            return new QName(name, prefix, ns);
        }
    }
}
