namespace EventTraceKit.VsExtension.Controls
{
    using System;
    using System.Windows;

    public sealed class AsyncDataGridCellsPresenterViewModel
        : DependencyObject
    {
        private readonly HdvViewModel hdv;

        public AsyncDataGridCellsPresenterViewModel(HdvViewModel hdv)
        {
            this.hdv = hdv;

            RowSelection = new AsyncDataGridRowSelection(this);
        }

        public int RowCount => hdv.Hdv.RowCount;

        public AsyncDataGridRowSelection RowSelection { get; }

        #region public int FocusIndex { get; set; }

        public event EventHandler FocusIndexChanged;

        /// <summary>
        ///   Identifies the <see cref="FocusIndex"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FocusIndexProperty =
            DependencyProperty.Register(
                nameof(FocusIndex),
                typeof(int),
                typeof(AsyncDataGridCellsPresenterViewModel),
                new PropertyMetadata(
                    Boxed.Int32Zero,
                    (d, e) => ((AsyncDataGridCellsPresenterViewModel)d).OnFocusIndexChanged(e)));

        /// <summary>
        ///   Gets or sets the focux index.
        /// </summary>
        public int FocusIndex
        {
            get { return (int)GetValue(FocusIndexProperty); }
            set { SetValue(FocusIndexProperty, value); }
        }

        private void OnFocusIndexChanged(DependencyPropertyChangedEventArgs e)
        {
            FocusIndexChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        internal bool IsReady
        {
            get
            {
                VerifyAccess();
                return hdv.IsReady;
            }
        }

        protected void ValidateIsReady()
        {
            if (!IsReady)
                throw new InvalidOperationException();
        }

        public void RequestUpdate(bool updateFromViewModel)
        {
            VerifyAccess();
            ValidateIsReady();
            hdv.RequestUpdate(updateFromViewModel);
        }
    }
}