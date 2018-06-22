namespace EventTraceKit.VsExtension.Controls
{
    using System;
    using System.Windows;
    using EventTraceKit.VsExtension.Windows;

    public interface IVirtualCollection
    {
        int FocusIndex { get; set; }
        int RowCount { get; }
    }

    public sealed class AsyncDataGridCellsPresenterViewModel
        : DependencyObject, IVirtualCollection
    {
        private readonly AsyncDataViewModel advModel;

        public AsyncDataGridCellsPresenterViewModel(AsyncDataViewModel advModel)
        {
            this.advModel = advModel;

            RowSelection = new AsyncDataGridRowSelection(this);
        }

        public int RowCount => advModel.DataView.RowCount;

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
            get => (int)GetValue(FocusIndexProperty);
            set => SetValue(FocusIndexProperty, value);
        }

        private void OnFocusIndexChanged(DependencyPropertyChangedEventArgs e)
        {
            FocusIndexChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        public bool IsReady
        {
            get
            {
                VerifyAccess();
                return advModel.IsReady;
            }
        }

        public void RequestUpdate()
        {
            VerifyAccess();
            ValidateIsReady();
            advModel.RequestUpdate();
        }

        private void ValidateIsReady()
        {
            if (!IsReady)
                throw new InvalidOperationException();
        }
    }
}
