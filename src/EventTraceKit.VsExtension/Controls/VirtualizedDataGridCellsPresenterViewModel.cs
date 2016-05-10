namespace EventTraceKit.VsExtension.Controls
{
    using System;
    using System.Windows;

    public sealed class VirtualizedDataGridCellsPresenterViewModel
        : DependencyObject
    {
        private readonly VirtualizedDataGridViewModel parent;

        public VirtualizedDataGridCellsPresenterViewModel(
            VirtualizedDataGridViewModel parent)
        {
            this.parent = parent;

            RowSelection = new VirtualizedDataGridRowSelection(this);
        }

        public int RowCount => parent.RowCount;

        public VirtualizedDataGridRowSelection RowSelection { get; }

        #region public int FocusIndex { get; set; }

        public event EventHandler FocusIndexChanged;

        /// <summary>
        ///   Identifies the <see cref="FocusIndex"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FocusIndexProperty =
            DependencyProperty.Register(
                nameof(FocusIndex),
                typeof(int),
                typeof(VirtualizedDataGridCellsPresenterViewModel),
                new PropertyMetadata(
                    Boxed.Int32Zero,
                    (d, e) => ((VirtualizedDataGridCellsPresenterViewModel)d).OnFocusIndexChanged(e)));

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

        public void RequestUpdate(bool updateFromViewModel)
        {
            VerifyAccess();
            //base.ValidateIsReady();
            parent.RequestUpdate(updateFromViewModel);
            //base.hdvViewModel.RequestUpdate(updateFromViewModel);
        }
    }
}