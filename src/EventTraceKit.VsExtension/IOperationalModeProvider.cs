namespace EventTraceKit.VsExtension
{
    using System;
    using System.Collections.Generic;

    public interface IOperationalModeProvider
    {
        event Action<VsOperationalMode, IReadOnlyList<DebuggedProjectInfo>> OperationalModeChanged;
    }

    public enum VsOperationalMode
    {
        Design = 0,
        Debug = 1
    }
}
