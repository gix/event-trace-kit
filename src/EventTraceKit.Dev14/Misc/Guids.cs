namespace EventTraceKit.Dev14.Misc
{
    using System;

    /// <summary>
    /// This class is used only to expose the list of Guids used by this package.
    /// This list of guids must match the set of Guids used inside the VSCT file.
    /// </summary>
    static class GuidsList
    {
        // Now define the list of guids as public static members.
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        public static readonly Guid guidClientPkg = new Guid("{7867DA46-69A8-40D7-8B8F-92B0DE8084D8}");
        public static readonly Guid guidClientCmdSet = new Guid("{A5BCFF42-2F6E-465A-9484-9B4FC3C8B2AC}");

        /// <summary>
        /// This Guid is the persistence guid for the output window.
        /// It can be found by running this sample, bringing up the output window,
        /// selecting it in the Persisted window and then looking in the Properties
        /// window.
        /// </summary>
        public static readonly Guid guidOutputWindowFrame = new Guid("{34E76E81-EE4A-11D0-AE2E-00A0C90FFFC3}");
    }
}
