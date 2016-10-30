namespace EventTraceKit.VsExtension
{
    using System;
    using Microsoft.VisualStudio.Shell;

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    internal sealed class FontAndColorsRegistrationAttribute : RegistrationAttribute
    {
        public FontAndColorsRegistrationAttribute(string name, string provider, string category)
        {
            Name = name;
            ProviderId = new Guid(provider);
            CategoryId = new Guid(category);
        }

        public string Name { get; }
        public Guid ProviderId { get; }
        public Guid CategoryId { get; }

        private string KeyPath => $@"FontAndColors\{Name}";

        public override void Register(RegistrationContext context)
        {
            if (context == null)
                return;

            context.Log.WriteLine(
                "FontAndColors:    Name:{0}, Category:{1:B}, Package:{2:B}", Name, CategoryId, ProviderId);

            using (var key = context.CreateKey(KeyPath)) {
                key.SetValue("Category", CategoryId.ToString("B"));
                key.SetValue("Package", ProviderId.ToString("B"));
            }
        }

        public override void Unregister(RegistrationContext context)
        {
            context.RemoveKey(KeyPath);
        }
    }
}
