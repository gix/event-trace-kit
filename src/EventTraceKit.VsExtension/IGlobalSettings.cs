﻿namespace EventTraceKit.VsExtension
{
    public interface IGlobalSettings
    {
        string ActiveViewPreset { get; set; }
        bool AutoLog { get; set; }
        bool ShowColumnHeaders { get; set; }
        bool ShowStatusBar { get; set; }
    }
}