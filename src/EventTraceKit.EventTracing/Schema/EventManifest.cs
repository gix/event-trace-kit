namespace EventTraceKit.EventTracing.Schema
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using EventTraceKit.EventTracing.Support;

    public sealed class EventManifest : SourceItem
    {
        private readonly List<LocalizedResourceSet> resourceSets =
            new List<LocalizedResourceSet>();

        public EventManifest()
        {
            Providers = new ProviderCollection(this);
        }

        public ProviderCollection Providers { get; }

        public IReadOnlyList<LocalizedResourceSet> Resources => resourceSets;

        public LocalizedResourceSet PrimaryResourceSet =>
            resourceSets.Count >= 1 ? resourceSets[0] : null;

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
            if (stringRef == null)
                throw new ArgumentNullException(nameof(stringRef));
            if (resourceSet == null)
                throw new ArgumentNullException(nameof(resourceSet));

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
