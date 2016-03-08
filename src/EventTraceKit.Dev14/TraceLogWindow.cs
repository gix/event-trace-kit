namespace EventTraceKit.Dev14
{
    using System;
    using System.Runtime.InteropServices;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;

    [Guid("D7E4C7D7-6A52-4586-9D42-D1AD0A407E4F")]
    public class TraceLogWindow : ToolWindowPane
    {
        private readonly EventTraceKitPackage package;

        /// <summary>
        ///   Initializes a new instance of the <see cref="TraceLogWindow"/>
        ///   class.
        /// </summary>
        public TraceLogWindow(EventTraceKitPackage package)
        {
            this.package = package;

            var control = new TraceLogWindowControl {
                DataContext = new TraceLogWindowViewModel()
            };

            Caption = "Trace Log";
            Content = control;
        }

        public TraceLogWindow()
            : this(null)
        {
        }

        public override IVsSearchTask CreateSearch(
            uint dwCookie, IVsSearchQuery pSearchQuery, IVsSearchCallback pSearchCallback)
        {
            if (pSearchQuery == null || pSearchCallback == null)
                return null;
            return new SearchTask(dwCookie, pSearchQuery, pSearchCallback, this);
        }

        public override bool SearchEnabled => base.SearchEnabled;

        public override void ClearSearch()
        {
            var control = (TraceLogWindowControl)Content;
            //control.SearchResultsTextBox.Text = control.SearchContent;
        }

        private class SearchTask : VsSearchTask
        {
            private readonly TraceLogWindow toolwindow;

            public SearchTask(
                uint dwCookie, IVsSearchQuery pSearchQuery,
                IVsSearchCallback pSearchCallback, TraceLogWindow toolwindow)
                : base(dwCookie, pSearchQuery, pSearchCallback)
            {
                this.toolwindow = toolwindow;
            }

            protected override void OnStartSearch()
            {
                // Use the original content of the text box as the target of the search. 
                var separator = new[] { Environment.NewLine };
                var control = (TraceLogWindowControl)toolwindow.Content;

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
