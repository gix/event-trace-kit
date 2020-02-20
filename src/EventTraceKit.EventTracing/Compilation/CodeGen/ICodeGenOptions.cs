namespace EventTraceKit.EventTracing.Compilation
{
    public interface ICodeGenOptions
    {
        bool UseCustomEventEnabledChecks { get; }
        bool SkipDefines { get; }
        bool GenerateStubs { get; }
        string EtwNamespace { get; }
        string LogNamespace { get; }
        string AlwaysInlineAttribute { get; }
        string NoInlineAttribute { get; }
        string LogCallPrefix { get; }
    }
}
