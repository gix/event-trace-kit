namespace EventTraceKit.EventTracing.Schema
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
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

        public static EventManifest Combine(IEnumerable<EventManifest> manifests)
        {
            var combined = new EventManifest();

            int index = 0;
            var stringMap = new Dictionary<LocalizedString, LocalizedString>();
            foreach (var manifest in manifests) {
                ++index;

                foreach (var provider in manifest.Providers)
                    combined.Providers.Add(provider);

                string manifestPrefix = "manifest" + index;

                foreach (var sourceResourceSet in manifest.Resources) {
                    var targetResourceSet = combined.Resources.FirstOrDefault(x => x.Culture == sourceResourceSet.Culture);
                    if (targetResourceSet == null) {
                        targetResourceSet = new LocalizedResourceSet(sourceResourceSet.Culture);
                        combined.AddResourceSet(targetResourceSet);
                    }

                    foreach (var sourceString in sourceResourceSet.Strings) {
                        // Just add the source string if its name is still unused.
                        var existingEntry = targetResourceSet.Strings.GetByName(sourceString.Name);
                        if (existingEntry == null) {
                            targetResourceSet.Strings.Add(sourceString);
                            continue;
                        }

                        // Otherwise, try to find a matching entry to avoid duplicates.
                        var matchingEntry = targetResourceSet.Strings.FirstOrDefault(
                            x => x.Id == sourceString.Id &&
                                Equals(x.Symbol, sourceString.Symbol) &&
                                Equals(x.Value, sourceString.Value));

                        if (matchingEntry == null) {
                            // The name is already used, and no matching entry
                            // exists. Add the string with a different, unique
                            // name to the resource set.
                            string uniqueName = manifestPrefix + "." + sourceString.Name;
                            matchingEntry = new LocalizedString(uniqueName, sourceString.Value) {
                                Location = sourceString.Location
                            };
                            targetResourceSet.Strings.Add(matchingEntry);
                        }

                        stringMap.Add(sourceString, matchingEntry);
                    }
                }
            }

            // Replace any messages which could not be used as-is due to name
            // conflicts.
            foreach (var provider in combined.Providers) {
                {
                    if (provider.Message != null && stringMap.TryGetValue(provider.Message, out var replacement))
                        provider.Message = replacement;
                }
                foreach (var item in provider.Channels) {
                    if (item.Message != null && stringMap.TryGetValue(item.Message, out var replacement))
                        item.Message = replacement;
                }
                foreach (var item in provider.Levels) {
                    if (item.Message != null && stringMap.TryGetValue(item.Message, out var replacement))
                        item.Message = replacement;
                }
                foreach (var item in provider.Opcodes) {
                    if (item.Message != null && stringMap.TryGetValue(item.Message, out var replacement))
                        item.Message = replacement;
                }
                foreach (var item in provider.Tasks) {
                    if (item.Message != null && stringMap.TryGetValue(item.Message, out var replacement))
                        item.Message = replacement;
                }
                foreach (var item in provider.Keywords) {
                    if (item.Message != null && stringMap.TryGetValue(item.Message, out var replacement))
                        item.Message = replacement;
                }
                foreach (var item in provider.Maps.SelectMany(x => x.Items)) {
                    if (provider.Message != null && stringMap.TryGetValue(item.Message, out var replacement))
                        item.Message = replacement;
                }
                foreach (var item in provider.Filters) {
                    if (item.Message != null && stringMap.TryGetValue(item.Message, out var replacement))
                        item.Message = replacement;
                }
                foreach (var item in provider.Events) {
                    if (item.Message != null && stringMap.TryGetValue(item.Message, out var replacement))
                        item.Message = replacement;
                }
            }

            return combined;
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
