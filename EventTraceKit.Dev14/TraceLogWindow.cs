namespace EventTraceKit.Dev14
{
    using System;
    using System.Runtime.InteropServices;
    using Microsoft.VisualStudio.Imaging;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;

    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    /// </summary>
    /// <remarks>
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
    /// usually implemented by the package implementer.
    /// <para>
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its
    /// implementation of the IVsUIElementPane interface.
    /// </para>
    /// </remarks>
    [Guid("d7e4c7d7-6a52-4586-9d42-d1ad0a407e4f")]
    public class TraceLogWindow : ToolWindowPane
    {
        /// <summary>
        ///   Initializes a new instance of the <see cref="TraceLogWindow"/>
        ///   class.
        /// </summary>
        public TraceLogWindow()
            : base(null)
        {
            Caption = "ToolWindow1";

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            Content = new TraceLogWindowControl();

            InfoBarTextSpan textSpan1 = new InfoBarTextSpan("This is a sample info bar ");
            InfoBarHyperlink link1 = new InfoBarHyperlink("sample link1 ", "1");
            InfoBarHyperlink link2 = new InfoBarHyperlink("sample link2 ", "2");
            InfoBarButton button1 = new InfoBarButton("sample button1", "3");
            InfoBarButton button2 = new InfoBarButton("sample button2", "4");
            InfoBarTextSpan[] textSpanCollection = { textSpan1, link1, link2 };
            InfoBarActionItem[] actionItemCollection = { button1, button2 };
            InfoBarModel infoBarModel = new InfoBarModel(textSpanCollection, actionItemCollection,
                KnownMonikers.StatusInformation, isCloseButtonVisible: true);
            this.AddInfoBar(infoBarModel);
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
