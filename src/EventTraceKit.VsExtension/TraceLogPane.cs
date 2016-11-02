namespace EventTraceKit.VsExtension
{
    using System;
    using System.ComponentModel.Design;
    using System.Runtime.InteropServices;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;

    [Guid("D7E4C7D7-6A52-4586-9D42-D1AD0A407E4F")]
    public class TraceLogPane : ToolWindowPane
    {
        private readonly Func<IServiceProvider, TraceLogPaneContent> traceLogFactory;
        private readonly Action onClose;

        /// <summary>
        ///   Initializes a new instance of the <see cref="TraceLogPane"/>
        ///   class.
        /// </summary>
        public TraceLogPane(Func<IServiceProvider, TraceLogPaneContent> traceLogFactory, Action onClose)
        {
            this.traceLogFactory = traceLogFactory;
            this.onClose = onClose;

            Caption = "Trace Log";
            BitmapImageMoniker = KnownImageMonikers.TraceLog;
            ToolBar = new CommandID(PkgCmdId.TraceLogCmdSet, PkgCmdId.TraceLogToolbar);
        }

        protected override void Initialize()
        {
            base.Initialize();
            Content = traceLogFactory(this);
        }

        protected override void OnClose()
        {
            base.OnClose();
            onClose();
        }

        public override IVsSearchTask CreateSearch(
            uint dwCookie, IVsSearchQuery pSearchQuery, IVsSearchCallback pSearchCallback)
        {
            if (pSearchQuery == null || pSearchCallback == null)
                return null;
            return new SearchTask(dwCookie, pSearchQuery, pSearchCallback, this);
        }

        public override bool SearchEnabled => false;

        public override void ClearSearch()
        {
        }

        private class SearchTask : VsSearchTask
        {
            private readonly TraceLogPane pane;

            public SearchTask(
                uint dwCookie, IVsSearchQuery pSearchQuery,
                IVsSearchCallback pSearchCallback, TraceLogPane pane)
                : base(dwCookie, pSearchQuery, pSearchCallback)
            {
                this.pane = pane;
            }

            protected override void OnStartSearch()
            {
                SearchResults = 0;
                SearchCallback.ReportComplete(this, SearchResults);
                base.OnStartSearch();
            }

            protected override void OnStopSearch()
            {
                SearchResults = 0;
            }
        }
    }
}
