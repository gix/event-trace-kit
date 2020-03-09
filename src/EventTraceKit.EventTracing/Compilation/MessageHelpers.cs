namespace EventTraceKit.EventTracing.Compilation
{
    using System;
    using System.Linq;
    using EventTraceKit.EventTracing.Compilation.ResGen;
    using EventTraceKit.EventTracing.Schema;
    using EventTraceKit.EventTracing.Support;

    internal static class MessageHelpers
    {
        public static void AssignMessageIds(
            IDiagnostics diags, EventManifest manifest,
            Func<IMessageIdGenerator> generatorFactory)
        {
            foreach (var provider in manifest.Providers) {
                var msgIdGen = generatorFactory();
                if (NeedsId(provider.Message))
                    provider.Message.Id = msgIdGen.CreateId(provider);

                foreach (var obj in provider.Channels.Where(e => NeedsId(e.Message)))
                    obj.Message.Id = msgIdGen.CreateId(obj, provider);
                foreach (var obj in provider.Levels.Where(e => NeedsId(e.Message)))
                    obj.Message.Id = msgIdGen.CreateId(obj, provider);
                foreach (var obj in provider.Tasks.Where(e => NeedsId(e.Message)))
                    obj.Message.Id = msgIdGen.CreateId(obj, provider);
                foreach (var obj in provider.GetAllOpcodes().Where(e => NeedsId(e.Message)))
                    obj.Message.Id = msgIdGen.CreateId(obj, provider);
                foreach (var obj in provider.Keywords.Where(e => NeedsId(e.Message)))
                    obj.Message.Id = msgIdGen.CreateId(obj, provider);
                foreach (var obj in provider.Events.Where(e => NeedsId(e.Message)))
                    obj.Message.Id = msgIdGen.CreateId(obj, provider);
                foreach (var map in provider.Maps)
                    foreach (var item in map.Items.Where(e => NeedsId(e.Message)))
                        item.Message.Id = msgIdGen.CreateId(item, map, provider);
                foreach (var obj in provider.Filters.Where(e => NeedsId(e.Message)))
                    obj.Message.Id = msgIdGen.CreateId(obj, provider);
            }

            var primaryResourceSet = manifest.PrimaryResourceSet;
            if (primaryResourceSet == null)
                return;

            foreach (var @string in primaryResourceSet.Strings.Used()) {
                foreach (var resourceSet in manifest.Resources) {
                    if (resourceSet == manifest.PrimaryResourceSet)
                        continue;

                    if (!resourceSet.ContainsName(@string.Name))
                        diags.Report(
                            DiagnosticSeverity.Warning,
                            resourceSet.Location,
                            "String table for culture '{0}' is missing string '{1}'.",
                            resourceSet.Culture.Name,
                            @string.Name);
                }
            }
        }

        private static bool NeedsId(LocalizedString message)
        {
            return message != null && message.Id == LocalizedString.UnusedId;
        }
    }
}
