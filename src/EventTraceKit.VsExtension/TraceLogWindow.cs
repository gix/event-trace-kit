namespace EventTraceKit.VsExtension
{
    using System;
    using System.ComponentModel.Design;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Windows.Input;
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

            Caption = "Trace Log";
            ToolBar = new CommandID(Guids.TraceLogCmdSet, PkgCmdId.TraceLogToolbar);

            var control = new TraceLogWindowControl {
                DataContext = new TraceLogWindowViewModel(package)
            };

            Content = control;
        }

        public TraceLogWindow()
            : this(null)
        {
        }

        protected override void Initialize()
        {
            base.Initialize();

            var viewModel = (Content as TraceLogWindowControl)?.DataContext as TraceLogWindowViewModel;
            var mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (mcs != null)
                viewModel?.Attach(mcs);
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
