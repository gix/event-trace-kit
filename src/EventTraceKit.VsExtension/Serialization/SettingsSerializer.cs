namespace EventTraceKit.VsExtension.Serialization
{
    using Settings.Persistence;

    public class SettingsSerializer : ShapingXamlSerializer<SettingsElement>
    {
        public SettingsSerializer()
            : base(CreateSerializer(), new SerializationShaper<SettingsElement>())
        {
        }

        private static SafeXamlSerializer CreateSerializer()
        {
            var serializer = new SafeXamlSerializer(typeof(SettingsElement).Assembly);

            foreach (var type in typeof(SettingsElement).Assembly.GetTypes()) {
                if (typeof(SettingsElement).IsAssignableFrom(type))
                    serializer.AddKnownType(type);
            }

            return serializer;
        }
    }
}
