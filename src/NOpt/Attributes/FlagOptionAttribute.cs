namespace NOpt.Attributes
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;
    using System.Security;

    /// <summary>
    ///   The exception thrown when option parsing fails.
    /// </summary>
    [Serializable]
    public sealed class OptionException : Exception
    {
        /// <overloads>
        ///   Initializes a new instance of the <see cref="OptionException"/> class.
        /// </overloads>
        /// <summary>
        ///   Initializes a new instance of the <see cref="OptionException"/> class
        ///   with default properties.
        /// </summary>
        public OptionException()
        {
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="OptionException"/> class
        ///   with a specified error message.
        /// </summary>
        /// <param name="message">
        ///   The error message that explains the reason for this exception.
        /// </param>
        public OptionException(string message)
            : base(message)
        {
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="OptionException"/> class
        ///   with a specified error message and the exception that is the cause
        ///   of this exception.
        /// </summary>
        /// <param name="message">
        ///   The error message that explains the reason for this exception.
        /// </param>
        /// <param name="innerException">
        ///   The exception that is the cause of the current exception, or a
        ///   <see langword="null"/> reference (<c>Nothing</c> in Visual Basic)
        ///   if no inner exception is specified.
        /// </param>
        public OptionException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="OptionException"/> class
        ///   with serialized data.
        /// </summary>
        /// <param name="info">
        ///   The serialization information object holding the serialized
        ///   object data in the name-value form.
        /// </param>
        /// <param name="context">
        ///   The contextual information about the source or destination of
        ///   the exception.
        /// </param>
        private OptionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        ///   When overridden in a derived class, sets the
        ///   <see cref="SerializationInfo"/> with information about the
        ///   exception.
        /// </summary>
        /// <param name="info">
        ///   The <see cref="SerializationInfo"/> that holds the serialized
        ///   object data about the exception being thrown.
        /// </param>
        /// <param name="context">
        ///   The <see cref="StreamingContext"/> that contains contextual
        ///   information about the source or destination.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="info"/> is <see langword="null"/>.
        /// </exception>
        [SecurityCritical]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            Contract.Requires<ArgumentNullException>(info != null);

            base.GetObjectData(info, context);
        }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true)]
    public abstract class OptionAttribute : Attribute
    {
        private string name;

        public string Name
        {
            get
            {
                Contract.Ensures(Contract.Result<string>() != null);
                return name;
            }
            set
            {
                Contract.Requires<ArgumentNullException>(value != null);
                Contract.Requires<ArgumentException>(IsValidName(value));
                name = value;
            }
        }

        public int Id { get; set; }

        public string HelpText { get; set; }

        [Pure]
        public static bool IsValidName(string name)
        {
            return !string.IsNullOrEmpty(name) && !name.Any(char.IsWhiteSpace);
        }

        protected static bool TryParse(string prefixedName, out string prefix, out string name)
        {
            foreach (var p in new[] { "--", "-", "/" }) {
                if (prefixedName.Length <= p.Length ||
                    !prefixedName.StartsWith(p))
                    continue;

                prefix = p;
                name = prefixedName.Substring(p.Length);
                if (!IsValidName(name))
                    return false;
                return true;
            }

            prefix = null;
            name = null;
            return false;
        }

        public abstract bool AcceptsMember(MemberInfo member);

        public abstract void AddOption(int optionId, OptTableBuilder builder);

        public virtual object GetValue(MemberInfo member, IArgumentList args, int optionId)
        {
            Type type;
            if (member.MemberType == MemberTypes.Property)
                type = ((PropertyInfo)member).PropertyType;
            else if (member.MemberType == MemberTypes.Field)
                type = ((FieldInfo)member).FieldType;
            else
                throw new ArgumentException("member");

            return GetValue(type, args, optionId);
        }

        public abstract object GetValue(Type type, IArgumentList args, int optionId);

        public virtual void SetValue<T>(T obj, MemberInfo member, object value)
        {
            if (member.MemberType == MemberTypes.Property)
                ((PropertyInfo)member).SetValue(obj, value);
            else if (member.MemberType == MemberTypes.Field)
                ((FieldInfo)member).SetValue(obj, value);
            else
                throw new ArgumentException("member");
        }
    }

    public class InputOptionAttribute : OptionAttribute
    {
        public InputOptionAttribute()
        {
            Name = "<input>";
        }

        public override bool AcceptsMember(MemberInfo member)
        {
            Type type;
            if (member.MemberType == MemberTypes.Property)
                type = ((PropertyInfo)member).PropertyType;
            else if (member.MemberType == MemberTypes.Field)
                type = ((FieldInfo)member).FieldType;
            else
                return false;

            return
                typeof(ICollection<string>).IsAssignableFrom(type) ||
                typeof(ICollection).IsAssignableFrom(type) ||
                typeof(string).IsAssignableFrom(type) ||
                typeof(object).IsAssignableFrom(type);
        }

        public override void AddOption(int optionId, OptTableBuilder builder)
        {
            builder.AddInput(optionId);
        }

        public override object GetValue(Type type, IArgumentList args, int optionId)
        {
            bool allowMultiple =
                typeof(ICollection<string>).IsAssignableFrom(type) ||
                typeof(ICollection).IsAssignableFrom(type);

            if (allowMultiple)
                return args.GetAllArgValues(optionId);

            if (args.Matching(optionId).Count() > 1)
                throw new OptionException();

            return args.GetLastArgValue(optionId);
        }
    }

    public class FlagOptionAttribute : OptionAttribute
    {
        private readonly string[] prefixes;

        public FlagOptionAttribute(string prefixedName)
        {
            Contract.Requires<ArgumentNullException>(prefixedName != null);

            if (!TryParse(prefixedName, out var prefix, out var name))
                throw new ArgumentException("prefixedName");

            Name = name;
            prefixes = new[] { prefix };
        }

        public FlagOptionAttribute(string prefix, string name)
        {
            Contract.Requires<ArgumentNullException>(prefix != null);
            Contract.Requires<ArgumentNullException>(name != null);
            Contract.Requires<ArgumentException>(IsValidName(name));
            Name = name;
            prefixes = new[] { prefix };
        }

        public string[] Prefixes
        {
            get
            {
                Contract.Ensures(Contract.Result<string[]>() != null);
                Contract.Ensures(Contract.Result<string[]>().Length > 0);
                return prefixes;
            }
        }

        public bool DefaultValue { get; set; }

        public override bool AcceptsMember(MemberInfo member)
        {
            Type type;
            if (member.MemberType == MemberTypes.Property)
                type = ((PropertyInfo)member).PropertyType;
            else if (member.MemberType == MemberTypes.Field)
                type = ((FieldInfo)member).FieldType;
            else
                return false;

            return type == typeof(bool) || type == typeof(object);
        }

        public override void AddOption(int optionId, OptTableBuilder builder)
        {
            builder.AddFlag(optionId, Prefixes, Name, HelpText);
        }

        public override object GetValue(Type type, IArgumentList args, int optionId)
        {
            bool value = args.GetFlag(optionId, DefaultValue);
            return value;
        }
    }

    public class DeclarativeCommandLineParser
    {
        private sealed class Info
        {
            public int OptionId;
            public MemberInfo Member;
        }

        private readonly List<Info> infos = new List<Info>();
        private readonly object optionBag;
        private readonly OptTableBuilder optTableBuilder = new OptTableBuilder();
        private OptTable optTable;

        public DeclarativeCommandLineParser(object optionBag)
        {
            Contract.Requires<ArgumentNullException>(optionBag != null);
            this.optionBag = optionBag;
            ReflectOptTable();
        }

        public OptTable OptTable
        {
            get
            {
                SealOptTable();
                return optTable;
            }
        }

        public OptTableBuilder Builder
        {
            get
            {
                Contract.Requires<InvalidOperationException>(!IsSealed);
                return optTableBuilder;
            }
        }

        public bool IsSealed { get; private set; }

        private void ReflectOptTable()
        {
            Type type = optionBag.GetType();
            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;

            int nextOptionId = 2;
            foreach (var member in type.GetMembers(flags)) {
                var attribute = member.GetCustomAttribute<OptionAttribute>();
                if (attribute == null)
                    continue;

                if (!attribute.AcceptsMember(member))
                    throw new OptionException(member.Name + " has incompatible option attribute.");

                int id = nextOptionId++;
                attribute.AddOption(id, optTableBuilder);
                infos.Add(new Info { OptionId = id, Member = member });
            }
        }

        public void Parse(IReadOnlyList<string> args)
        {
            SealOptTable();

            IArgumentList al = OptTable.ParseArgs(args, out var _);

            foreach (var info in infos) {
                var attribute = info.Member.GetCustomAttribute<OptionAttribute>();
                if (attribute == null)
                    continue;

                object value = attribute.GetValue(info.Member, al, info.OptionId);
                attribute.SetValue(optionBag, info.Member, value);
            }
        }

        private void SealOptTable()
        {
            if (IsSealed)
                return;

            optTable = optTableBuilder.CreateTable();
            IsSealed = true;
        }
    }
}
