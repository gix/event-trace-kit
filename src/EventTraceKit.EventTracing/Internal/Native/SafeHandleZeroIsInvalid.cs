namespace EventTraceKit.EventTracing.Internal.Native
{
    using System;
    using System.Runtime.ConstrainedExecution;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Security.Permissions;

    /// <summary>
    ///   Provides a base class for Win32 safe handle implementations in which
    ///   the value of 0 indicates an invalid handle.
    /// </summary>
    [SecurityCritical]
    [SecurityPermission(SecurityAction.InheritanceDemand, UnmanagedCode = true)]
    internal abstract class SafeHandleZeroIsInvalid : SafeHandle
    {
        /// <summary>
        ///   Initializes a new instance of the <see cref="SafeHandleZeroIsInvalid"/>
        ///   class, specifying whether the handle is to be reliably released.
        /// </summary>
        /// <param name="ownsHandle">
        ///   <see langword="true"/> to reliably release the handle during the
        ///   finalization phase; <see langword="false"/> to prevent reliable
        ///   release (not recommended).
        /// </param>
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        protected SafeHandleZeroIsInvalid(bool ownsHandle)
            : base(IntPtr.Zero, ownsHandle)
        {
        }

        /// <summary>
        ///   Gets a value that indicates whether the handle is invalid.
        /// </summary>
        public override bool IsInvalid
        {
            [SecurityCritical]
            get { return handle == IntPtr.Zero; }
        }
    }
}
