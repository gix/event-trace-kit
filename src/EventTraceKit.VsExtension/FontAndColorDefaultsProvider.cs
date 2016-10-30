namespace EventTraceKit.VsExtension
{
    using System;
    using System.Runtime.InteropServices;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell.Interop;
    using Styles;

    [Guid(ServiceId)]
    public class FontAndColorDefaultsProvider : IVsFontAndColorDefaultsProvider
    {
        public const string ServiceId = "C9CB1093-28C0-4CF1-8579-3F304A1CA35D";

        private readonly ResourceSynchronizer synchronizer;
        private TraceLogFontAndColorDefaults traceLogDefaults;

        public FontAndColorDefaultsProvider(ResourceSynchronizer synchronizer)
        {
            this.synchronizer = synchronizer;
        }

        private TraceLogFontAndColorDefaults TraceLogDefaults =>
            traceLogDefaults ?? (traceLogDefaults = new TraceLogFontAndColorDefaults(synchronizer));

        public int GetObject(ref Guid rguidCategory, out object ppObj)
        {
            if (rguidCategory == TraceLogFontAndColorDefaults.CategoryId)
                ppObj = TraceLogDefaults;
            else
                ppObj = null;
            return VSConstants.S_OK;
        }
    }
}
