namespace EventTraceKit.EventTracing.Tests.Compilation.ResGen
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using EventTraceKit.EventTracing.Tests.Compilation.TestSupport;
    using Xunit.Sdk;

    public class ResGenTestDataAttribute : DataAttribute
    {
        private readonly Type type;
        private readonly string fileExt;
        private readonly string resourcePrefix;

        public ResGenTestDataAttribute(Type type, string fileExt)
            : this(type, null, fileExt)
        {
        }

        public ResGenTestDataAttribute(Type type, string name, string fileExt)
        {
            this.type = type;
            this.fileExt = fileExt;
            resourcePrefix = type.Namespace + (name != null ? "." + name : null) + ".";
        }

        public override IEnumerable<object[]> GetData(MethodInfo testMethod)
        {
            var assembly = type.Assembly;
            var resourceNames = assembly.GetManifestResourceNames();

            foreach (var resourceName in resourceNames) {
                if (!resourceName.StartsWith(resourcePrefix))
                    continue;

                if (!resourceName.EndsWith(".man"))
                    continue;

                var testCase = resourceName.Substring(
                    resourcePrefix.Length, resourceName.Length - resourcePrefix.Length - 4);

                var expectedOutput = assembly.GetManifestResourceStream(
                    Path.ChangeExtension(resourceName, fileExt));

                if (expectedOutput == null)
                    continue;

                yield return new object[] { resourceName.Substring(resourcePrefix.Length), type, expectedOutput.ReadAllBytes() };
            }
        }
    }
}
