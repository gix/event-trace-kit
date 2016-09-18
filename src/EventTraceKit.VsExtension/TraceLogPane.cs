namespace EventTraceKit.VsExtension
{
    using System;
    using System.ComponentModel.Design;
    using System.Runtime.InteropServices;
    using EnvDTE;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;

    [Guid("D7E4C7D7-6A52-4586-9D42-D1AD0A407E4F")]
    public class TraceLogPane : ToolWindowPane
    {
        private readonly IOperationalModeProvider operationalModeProvider;

        /// <summary>
        ///   Initializes a new instance of the <see cref="TraceLogPane"/>
        ///   class.
        /// </summary>
        public TraceLogPane(IOperationalModeProvider operationalModeProvider)
        {
            this.operationalModeProvider = operationalModeProvider;

            Caption = "Trace Log";
            ToolBar = new CommandID(Guids.TraceLogCmdSet, PkgCmdId.TraceLogToolbar);
        }

        protected override void Initialize()
        {
            base.Initialize();

            var dte = (DTE)GetService(typeof(DTE));
            var traceLog = new TraceLogWindowViewModel(
                operationalModeProvider, () => new SolutionFileGatherer(dte));

            Content = new TraceLogWindow {
                DataContext = traceLog
            };

            var mcs = GetService(typeof(IMenuCommandService)) as MenuCommandService;
            if (mcs != null)
                traceLog.Attach(mcs);
        }

        public override IVsSearchTask CreateSearch(
            uint dwCookie, IVsSearchQuery pSearchQuery, IVsSearchCallback pSearchCallback)
        {
            if (pSearchQuery == null || pSearchCallback == null)
                return null;
            return new SearchTask(dwCookie, pSearchQuery, pSearchCallback, this);
        }

        public override bool SearchEnabled => true;

        public override void ClearSearch()
        {
            var control = (TraceLogWindow)Content;
            //control.SearchResultsTextBox.Text = control.SearchContent;
        }

        private class SearchTask : VsSearchTask
        {
            private readonly TraceLogPane toolwindow;

            public SearchTask(
                uint dwCookie, IVsSearchQuery pSearchQuery,
                IVsSearchCallback pSearchCallback, TraceLogPane toolwindow)
                : base(dwCookie, pSearchQuery, pSearchCallback)
            {
                this.toolwindow = toolwindow;
            }

            protected override void OnStartSearch()
            {
                // Use the original content of the text box as the target of the search. 
                var separator = new[] { Environment.NewLine };
                var control = (TraceLogWindow)toolwindow.Content;

                SearchResults = 0;

                base.OnStartSearch();
            }

            protected override void OnStopSearch()
            {
                SearchResults = 0;
            }
        }
    }
}
