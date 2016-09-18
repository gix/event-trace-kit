namespace EventTraceKit.VsExtension
{
    using System;

    public interface IOperationalModeProvider
    {
        VsOperationalMode CurrentMode { get; }
        event EventHandler<VsOperationalMode> OperationalModeChanged;
    }

    public enum VsOperationalMode
    {
        Design = 0,
        Debug = 1
    }

    internal class VsOperationalModeChangedEventArgs : EventArgs
    {
        public VsOperationalModeChangedEventArgs(VsOperationalMode newMode)
        {
            NewMode = newMode;
        }

        public VsOperationalMode NewMode { get; }
    }
}
