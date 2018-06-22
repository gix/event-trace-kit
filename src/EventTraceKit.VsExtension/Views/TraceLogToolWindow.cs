namespace EventTraceKit.VsExtension.Views
{
    using System;
    using System.ComponentModel.Design;
    using System.Runtime.InteropServices;
    using Microsoft.VisualStudio.Shell;

    [Guid("D7E4C7D7-6A52-4586-9D42-D1AD0A407E4F")]
    public class TraceLogToolWindow : ToolWindowPane
    {
        private readonly Func<IServiceProvider, TraceLogToolContent> traceLogFactory;
        private readonly Action<object> onClose;
        private TraceLogToolContent content;

        /// <summary>
        ///   Initializes a new instance of the <see cref="TraceLogToolWindow"/>
        ///   class.
        /// </summary>
        public TraceLogToolWindow(
            Func<IServiceProvider, TraceLogToolContent> traceLogFactory,
            Action<object> onClose)
        {
            this.traceLogFactory = traceLogFactory;
            this.onClose = onClose;

            Caption = "Trace Log";
            BitmapImageMoniker = KnownImageMonikers.TraceLog;
            ToolBar = new CommandID(PkgCmdId.TraceLogCmdSet, PkgCmdId.TraceLogToolbar);
        }

        public override object Content => content ?? (content = traceLogFactory(this));

        protected override void OnClose()
        {
            base.OnClose();
            onClose(content?.DataContext);
        }

        public override bool SearchEnabled => false;
    }
}
