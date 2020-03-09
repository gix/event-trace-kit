namespace EventTraceKit.EventTracing.Compilation.CodeGen
{
    using System;
    using System.ComponentModel.Composition;
    using System.IO;
    using EventTraceKit.EventTracing.Schema;

    public interface ICodeGeneratorProvider
    {
        string Name { get; }
        object CreateOptions();
        ICodeGenerator CreateGenerator(object options);
    }

    public interface ICodeGenerator
    {
        void Generate(EventManifest manifest, Stream output);
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public sealed class CodeGeneratorAttribute : ExportAttribute
    {
        public CodeGeneratorAttribute()
            : base(typeof(ICodeGeneratorProvider))
        {
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = true, AllowMultiple = true)]
    public abstract class OptionAttribute : Attribute
    {
        protected OptionAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; }
        public string HelpText { get; set; }
    }

    public sealed class JoinedOptionAttribute : OptionAttribute
    {
        public JoinedOptionAttribute(string name) : base(name) { }
        public string DefaultValue { get; set; }
    }

    public sealed class FlagOptionAttribute : OptionAttribute
    {
        private bool? defaultValue;

        public FlagOptionAttribute(string name) : base(name) { }

        public bool HasDefaultValue => defaultValue.HasValue;

        public bool DefaultValue
        {
            get => defaultValue.GetValueOrDefault();
            set => defaultValue = value;
        }
    }
}
