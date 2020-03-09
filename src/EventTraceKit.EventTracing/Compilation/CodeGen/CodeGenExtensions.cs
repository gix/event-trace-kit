namespace EventTraceKit.EventTracing.Compilation.CodeGen
{
    using EventTraceKit.EventTracing.Schema;

    public static class CodeGenExtensions
    {
        public static byte GetDescriptorChannelValue(this Event @event)
        {
            if (@event.Channel?.Value != null)
                return @event.Channel.Value.Value;
            if (@event.Provider.ControlGuid != null)
                return 12; // ProviderMetadata
            return 0;
        }

        internal static bool RequiresTraits(this Provider provider)
        {
            return provider.ControlGuid != null || provider.GroupGuid != null;
        }
    }
}
