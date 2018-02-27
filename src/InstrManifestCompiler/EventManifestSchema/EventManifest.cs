namespace InstrManifestCompiler.EventManifestSchema
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.Linq;
    using InstrManifestCompiler.Collections;
    using InstrManifestCompiler.Support;

    public sealed class EventManifest : SourceItem
    {
        private readonly List<LocalizedResourceSet> resourceSets;

        public EventManifest()
        {
            Providers = new ProviderCollection(this);
            resourceSets = new List<LocalizedResourceSet>();
        }

        public IUniqueEntityList<Provider> Providers { get; private set; }

        public IReadOnlyList<LocalizedResourceSet> Resources
        {
            get { return resourceSets; }
        }

        public LocalizedResourceSet PrimaryResourceSet
        {
            get { return resourceSets.Count >= 1 ? resourceSets[0] : null; }
        }

        public void AddResourceSet(LocalizedResourceSet resourceSet)
        {
            resourceSets.Add(resourceSet);
        }

        public LocalizedString GetString(string stringRef)
        {
            if (resourceSets.Count < 1)
                return null;

            return GetString(stringRef, resourceSets[0]);
        }

        public LocalizedString GetString(string stringRef, LocalizedResourceSet resourceSet)
        {
            Contract.Requires<ArgumentNullException>(stringRef != null);
            Contract.Requires<ArgumentNullException>(resourceSet != null);
            Contract.Requires<ArgumentException>(Resources.Contains(resourceSet));

            if (IsStringTableRef(stringRef)) {
                string name = stringRef.Substring(9, stringRef.Length - 10);
                return resourceSet.Strings.GetByName(name);
            }

            if (IsMessageRef(stringRef)) {
                // FIXME
                string symbolId = stringRef.Substring(5, stringRef.Length - 6);
                throw new NotImplementedException("$(mc.symbolid) references are not implemented yet.");
            }

            throw new ArgumentException("Invalid message ref.");
        }

        public LocalizedString ImportString(LocalizedString str)
        {
            if (resourceSets.Count < 1)
                resourceSets.Add(new LocalizedResourceSet(CultureInfo.GetCultureInfo("en-US")));

            return resourceSets[0].Strings.Import(str);
        }

        private static bool IsStringTableRef(string stringRef)
        {
            return stringRef.StartsWith("$(string.") && stringRef.EndsWith(")");
        }

        private static bool IsMessageRef(string stringRef)
        {
            return stringRef.StartsWith("$(mc.") && stringRef.EndsWith(")");
        }
    }
}
