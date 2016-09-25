namespace EventTraceKit.VsExtension
{
    using System;

    /// <summary>
    /// This class is used only to expose the list of Guids used by this package.
    /// This list of guids must match the set of Guids used inside the VSCT file.
    /// </summary>
    internal static class Guids
    {
        public static readonly Guid TraceLogCmdSet = new Guid("46A772AB-D554-45B9-8DE2-EF68FCEF6732");

        /// <summary>
        /// This Guid is the persistence guid for the output window.
        /// It can be found by running this sample, bringing up the output window,
        /// selecting it in the Persisted window and then looking in the Properties
        /// window.
        /// </summary>
        public static readonly Guid guidOutputWindowFrame = new Guid("34E76E81-EE4A-11D0-AE2E-00A0C90FFFC3");
    }
}
