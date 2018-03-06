namespace EventManifestCompiler.Tests.ResGen
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using EventManifestCompiler.Tests.TestSupport;
    using EventManifestFramework.Schema;
    using Xunit.Sdk;

    public class ResGenTestProviderAttribute : DataAttribute
    {
        private readonly Type type;
        private readonly string fileExt;
        private readonly string resourcePrefix;

        public ResGenTestProviderAttribute(Type type, string fileExt)
            : this(type, null, fileExt)
        {
        }

        public ResGenTestProviderAttribute(Type type, string name, string fileExt)
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

                EventManifest manifest;
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                    manifest = TestHelper.LoadManifest(stream, testCase);

                yield return new object[] { testCase, manifest, expectedOutput };
            }
        }
    }
}
