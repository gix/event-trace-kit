namespace EventTraceKit.VsExtension
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using System.Windows;
    using Formatting;

    public class DataView : DependencyObject, IDataView, INotifyPropertyChanged
    {
        private readonly DataTable table;
        private readonly IFormatProviderSource formatProviderSource;

        private int deferredUpdateNestingDepth;

        public DataView(DataTable table, IFormatProviderSource formatProviderSource)
        {
            if (table == null)
                throw new ArgumentNullException(nameof(table));
            this.table = table;
            this.formatProviderSource = formatProviderSource;

            ClearCache();
        }

        public DataViewColumnsCollection Columns =>
            new DataViewColumnsCollection(this);

        public DataViewColumnsCollection VisibleColumns =>
            new DataViewColumnsCollection(this);

        protected DataColumnViewInfo[] DataColumnViewInfos { get; set; }
        protected DataColumnView[] DataColumnViews { get; set; }

        public bool DeferUpdates => deferredUpdateNestingDepth > 0;

        public void BeginDataUpdate()
        {
            ++deferredUpdateNestingDepth;
        }

        public bool EndDataUpdate()
        {
            if (--deferredUpdateNestingDepth != 0)
                return false;

            OnDataUpdated();
            return true;
        }

        protected virtual void OnDataUpdated()
        {
        }

        public void ApplyColumnView(DataColumnViewInfo[] dataColumnViewInfos)
        {
            ApplyColumnViewCore(dataColumnViewInfos);
        }

        protected void ApplyColumnViewCore(IEnumerable<DataColumnViewInfo> dataColumnViewInfos)
        {
            if (dataColumnViewInfos == null)
                throw new ArgumentNullException(nameof(dataColumnViewInfos));

            DataColumnViewInfos = dataColumnViewInfos.ToArray();
            foreach (DataColumnViewInfo info in DataColumnViewInfos)
                info.View = this;

            RefreshDataColumnViewFromViewInfos();
            //this.VisibleDataColumnViewIndices = new Int32List();
            //this.RefreshVisibleColumns();
        }

        private void RefreshDataColumnViewFromViewInfos()
        {
            DataColumnViews = new DataColumnView[DataColumnViewInfos.Length];
            Parallel.For(0, DataColumnViewInfos.Length, i => {
                DataColumnViews[i] = CreateDataColumnViewFromInfo(DataColumnViewInfos[i]);
            });
        }

        public DataColumnView CreateDataColumnViewFromInfo(DataColumnViewInfo info)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            DataColumn column = table.Columns[info.ColumnId];

            var columnView = column.CreateView(info);
            if (columnView.FormatProvider == null)
                columnView.FormatProvider = formatProviderSource.GetFormatProvider(column.DataType);

            return columnView;
        }

        public CellValue GetCellValue(int rowIndex, int columnIndex)
        {
            if (rowIndex >= RowCount)
                return null;

            return DataColumnViews[columnIndex].GetCellValue(rowIndex);
        }

        public event EventHandler RowCountChanged;

        public void UpdateRowCount(int newCount)
        {
            if (newCount == RowCount)
                return;

            if (newCount == 0)
                ClearCache();

            RowCount = newCount;
            //Application.Current.Dispatcher.Invoke(delegate {
            //    Updated?.Invoke(this, trueEventArgs);
            //});
            //RaisePropertyChanged(rowCountChangedArgs);
            RaisePropertyChanged(rowChangedEventArgs);
            RowCountChanged?.Invoke(this, EventArgs.Empty);
        }

        private readonly PropertyChangedEventArgs rowChangedEventArgs = new PropertyChangedEventArgs(nameof(RowCount));

        public int RowCount { get; private set; }
        public int ColumnCount => DataColumnViews?.Length ?? 0;

        public DataColumnView GetDataColumnView(int columnIndex)
        {
            return DataColumnViews[columnIndex];
        }

        public void Clear()
        {
            ClearCache();
        }

        private void ClearCache()
        {
        }

        public object DataValidityToken { get; private set; }

        public bool IsValidDataValidityToken(object dataValidityToken)
        {
            return dataValidityToken != null && dataValidityToken == DataValidityToken;
        }

        public int GetDataColumnViewIndex(DataColumnView column)
        {
            if (DataColumnViews == null)
                return -1;
            return Array.IndexOf(DataColumnViews, column);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(
            [CallerMemberName] string propertyName = null)
        {
            RaisePropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        private void RaisePropertyChanged(PropertyChangedEventArgs eventArgs)
        {
            PropertyChanged?.Invoke(this, eventArgs);
        }
    }
}
