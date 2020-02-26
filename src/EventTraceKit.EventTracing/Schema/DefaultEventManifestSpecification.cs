namespace EventTraceKit.EventTracing.Schema
{
    using System;
    using System.Linq;
    using EventTraceKit.EventTracing.Internal.Native;
    using EventTraceKit.EventTracing.Support;

    internal sealed class DefaultEventManifestSpecification
        : IEventManifestSpecification
    {
        private readonly IDiagnostics diags;

        public DefaultEventManifestSpecification()
            : this(new NullDiagnostics())
        {
        }

        public DefaultEventManifestSpecification(IDiagnostics diags)
        {
            this.diags = diags;
        }

        public bool IsSatisfiedBy(Provider provider)
        {
            bool result = true;

            if (provider.ControlGuid != null) {
                var trailingGuid = ExtractGuidFromProviderName(provider.Name.Value);
                if (trailingGuid == null || trailingGuid != provider.Id) {
                    result = false;
                    diags.ReportError(
                        provider.Name.Location,
                        "Invalid provider name '{0}'. When controlGuid is specified, the provider name must end with the provider guid.",
                        provider.Name.Value);
                }
            }

            return result;
        }

        /// <devdoc>
        ///   Microsoft's message compiler reads 32 hexadecimal characters (as
        ///   determined by <c>std::iswxdigit</c>) from the back, skipping any
        ///   other characters.
        /// </devdoc>
        private static Guid? ExtractGuidFromProviderName(string name)
        {
            var guidChars = new char[32];

            int nameIdx = name.Length - 1;
            for (int i = guidChars.Length - 1; i >= 0; --i) {
                char c;
                do {
                    if (nameIdx < 0) {
                        // Input string exhausted before reading characters.
                        return null;
                    }

                    c = name[nameIdx--];
                } while (!IsHexChar(c));

                guidChars[i] = c;
            }

            if (!Guid.TryParse(new string(guidChars), out var id))
                return null;

            return id;
        }

        private static bool IsHexChar(char c)
        {
            return (c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
        }

        public bool IsSatisfiedBy(Event @event)
        {
            bool result = true;

            if (@event.Value > 0xFFFF) {
                result = false;
                diags.ReportError(
                    @event.Value.Location ?? @event.Location,
                    "Invalid value '{0}' (0x{0:X}) for event '{1}'. Event values must be in [0, 65535].",
                    @event.Value,
                    @event.Symbol ?? "<no symbol>");
            }

            if (!(@event.Version >= 0 && @event.Version <= 255)) {
                result = false;
                diags.ReportError(
                    @event.Version.Location ?? @event.Location,
                    "Invalid version '{0}' (0x{0:X}) for event '{1}' ('{2}'). Event version must be in [0, 255].",
                    @event.Version,
                    @event.Symbol ?? "<no symbol>",
                    @event.Value);
            }

            // Docs say: "You must specify a message if the channel type to which the event is written is Admin."
            // but mc.exe does not enforce this.
            if (false && @event.Channel.Type == ChannelType.Admin && @event.Message == null) {
                result = false;
                diags.ReportError(
                    @event.Location,
                    "Event '{0}' ('{1}') is written to an admin channel and must have a message.",
                    @event.Symbol ?? "<no symbol>",
                    @event.Value);
            }

            return result;
        }

        public bool IsSatisfiedBy(Channel channel)
        {
            bool result = true;

            if (ContainsInvalidChannelNameChars(channel.Name)) {
                result = false;
                diags.ReportError(
                    channel.Name.Location ?? channel.Location,
                    "Invalid channel name '{0}'. " +
                    "The name must not contain U+001F, \", >, <, &, |, \\, \', :, * or ?.",
                    channel.Name);
            }

            // Name.Length in [1, 255]
            if (!(channel.Name.Value.Length >= 1 && channel.Name.Value.Length <= 255)) {
                result = false;
                diags.ReportError(
                    channel.Name.Location ?? channel.Location,
                    "Length of channel name must be in the range [1, 255], but is {0}.",
                    channel.Name.Value.Length);
            }

            //if (!channel.Name.Value.StartsWith(channel.Provider.Name.Value + "/")) {
            //    diags.Report(
            //        DiagnosticSeverity.Warning,
            //        channel.Name.Location ?? channel.Location,
            //        "Channel name should be '{0}/{1}' per convention to name channels using 'ProviderName/ChannelType'.",
            //        channel.Provider.Name,
            //        channel.Name);
            //}

            // Value in [16, 255]
            if (channel.Value.HasValue && !(channel.Value >= 16 && channel.Value <= 255)) {
                result = false;
                diags.ReportError(
                    channel.Value.Location ?? channel.Location,
                    "Invalid value '{0}' (0x{0:X}) for channel '{1}'. User-defined channel values must be in the range [16, 255].",
                    channel.Value,
                    channel.Name);
            }

            if (channel.Id != null && channel.Id.Value.Length == 0) {
                result = false;
                diags.ReportError(
                    channel.Id.Location ?? channel.Location,
                    "Channel id must not be empty.");
            }

            if (channel.Access != null &&
                !NativeMethods.IsValidSecurityDescriptorString(channel.Access)) {
                result = false;
                diags.ReportError(
                    channel.Access.Location ?? channel.Location,
                    "Access descriptor '{0}' for channel '{1}' is not a valid SDDL string.",
                    channel.Access,
                    channel.Name);
            }

            return result;
        }

        private static readonly char[] InvalidChannelNameChars = {
            '>', '<', '&', '"', '|', '\\', ':', '\'', '?', '*'
        };

        private static bool ContainsInvalidChannelNameChars(string name)
        {
            return
                name.IndexOfAny(InvalidChannelNameChars) != -1 ||
                name.Any(c => c <= 31);
        }

        public bool IsSatisfiedBy(Level level)
        {
            bool result = true;

            if (!(level.Value >= 16 && level.Value <= 255)) {
                result = false;
                diags.ReportError(
                    level.Value.Location ?? level.Location,
                    "Invalid value '{0}' (0x{0:X}) for level '{1}'. User-defined level values must be in the range [16, 255].",
                    level.Value,
                    level.Symbol ?? "<no symbol>");
            }

            return result;
        }

        public bool IsSatisfiedBy(Task task)
        {
            bool result = true;

            if (!(task.Value >= 0 && task.Value <= 0xFFFF)) {
                result = false;
                diags.ReportError(
                    task.Value.Location ?? task.Location,
                    "Invalid value '{0}' (0x{0:X}) for task '{1}'. User-defined task values must be in the range [16, 255].",
                    task.Value,
                    task.Symbol ?? "<no symbol>");
            }

            foreach (var opcode in task.Opcodes) {
                if (opcode.Name.Value.Namespace == WinEventSchema.Namespace) {
                    result = false;
                    diags.ReportError(
                        opcode.Name.Location ?? task.Location,
                        "Invalid opcode '{0}' for task '{1}'. Task-specific opcodes cannot use opcode values defined in winmeta.xml.",
                        opcode.Name,
                        task.Symbol ?? "<no symbol>");
                }
            }

            return result;
        }

        public bool IsSatisfiedBy(Opcode opcode)
        {
            bool result = true;

            if (!(opcode.Value >= 10 && opcode.Value <= 239)) {
                result = false;
                diags.ReportError(
                    opcode.Value.Location ?? opcode.Location,
                    "Invalid value '{0}' (0x{0:X}) for opcode '{1}'. User-defined opcode values must be in the range [10, 239].",
                    opcode.Value,
                    opcode.Symbol ?? "<no symbol>");
            }

            return result;
        }

        public bool IsSatisfiedBy(Keyword keyword)
        {
            bool result = true;

            if (CountBits(keyword.Mask.Value) != 1) {
                result = false;
                diags.ReportError(
                    keyword.Mask.Location ?? keyword.Location,
                    "Invalid mask '0x{0:X}' for keyword '{1}'. Keyword masks must have one and only one bit set.",
                    keyword.Mask,
                    keyword.Symbol ?? "<no symbol>");
            }

            return result;
        }

        private int CountBits(ulong value)
        {
            int count;
            for (count = 0; value != 0; ++count)
                value &= value - 1; // Clear least significant bit set.
            return count;
        }

        public bool IsSatisfiedBy(Filter filter)
        {
            // UserData not allowed for template with TId '{0}', since it is a filter template.
            // Struct not allowed for property '{0}' of template with TId '{1}', since it is a filter template.
            // Array not allowed for property '{0}' of template with TId '{1}', since it is a filter template.
            // Map not allowed for property '{0}' of template with TId '{1}', since it is a filter template.
            // Attribute outType is required for property '{0}' of template with TId '{1}', since it is a filter template.
            return true;
        }

        public bool IsSatisfiedBy(Template template)
        {
            bool result = true;

            if (template.Properties.Count > 99) {
                diags.ReportError(
                    template.Location,
                    "Template '{0}' has more than 99 properties.",
                    template.Id);
                result = false;
            }

            return result;
        }

        public bool IsSatisfiedBy(Map map)
        {
            return true;
        }

        public bool IsSatisfiedBy(DataProperty property)
        {
            if (property.Map != null) {
                // FIXME
                //UInt8, UInt16, UInt32, or HexInt32
            }

            if (property.InType.Name == WinEventSchema.Binary) {
                if (!property.Length.IsSpecified) {
                    diags.ReportError(
                        property.Location,
                        "Property '{0}' with inType '{1}' requires a length.",
                        property.Name,
                        property.InType.Name.AsPrefixedString);
                }
            } else if (property.InType.Name == WinEventSchema.CountedUnicodeString ||
                       property.InType.Name == WinEventSchema.CountedAnsiString ||
                       property.InType.Name == WinEventSchema.CountedBinary) {
                if (property.Length.IsSpecified) {
                    diags.ReportError(
                        property.Location,
                        "Property '{0}' with inType '{1}' must not have a length.",
                        property.Name,
                        property.InType.Name.AsPrefixedString);
                }
            }

            return true;
        }

        public bool IsSatisfiedBy(StructProperty property)
        {
            return true;
        }

        public bool IsSatisfiedBy(LocalizedString @string)
        {
            return true;
        }
    }
}
