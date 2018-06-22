namespace EventTraceKit.VsExtension
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Formatting;

    public class DataView : IDataView
    {
        private readonly DataTable table;
        private readonly IFormatProviderSource formatProviderSource;

        private int deferredUpdateNestingDepth;

        public DataView(DataTable table, IFormatProviderSource formatProviderSource)
        {
            this.table = table ?? throw new ArgumentNullException(nameof(table));
            this.formatProviderSource = formatProviderSource;

            ClearCache();
        }

        public event EventHandler RowCountChanged;

        public DataViewColumnsCollection Columns => new DataViewColumnsCollection(this);
        public DataViewColumnsCollection VisibleColumns => new DataViewColumnsCollection(this);

        public bool DeferUpdates => deferredUpdateNestingDepth > 0;

        public int RowCount { get; private set; }
        public int ColumnCount => DataColumnViews?.Length ?? 0;

        protected DataColumnViewInfo[] DataColumnViewInfos { get; set; }
        protected DataColumnView[] DataColumnViews { get; set; }

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
        }

        private void RefreshDataColumnViewFromViewInfos()
        {
            DataColumnViews = new DataColumnView[DataColumnViewInfos.Length];
            for (int i = 0; i < DataColumnViewInfos.Length; ++i)
                DataColumnViews[i] = CreateDataColumnViewFromInfo(DataColumnViewInfos[i]);
        }

        public DataColumnView CreateDataColumnViewFromInfo(DataColumnViewInfo info)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            DataColumn column = table.Columns[info.ColumnId];

            var columnView = column.CreateView(info);
            if (columnView.FormatProvider == null) {
                columnView.FormatProvider = formatProviderSource.GetFormatProvider(column.DataType);
                columnView.Format = formatProviderSource.GetFormat(columnView.FormatProvider, columnView.Format);
            }

            return columnView;
        }

        public CellValue GetCellValue(int rowIndex, int columnIndex)
        {
            if (rowIndex >= RowCount)
                return null;

            return DataColumnViews[columnIndex].GetCellValue(rowIndex);
        }

        public virtual T GetInteractionWorkflow<T>(int? rowIndex, int? columnIndex)
            where T : class
        {
            return null;
        }

        public void UpdateRowCount(int newCount)
        {
            if (newCount == RowCount)
                return;

            if (newCount == 0)
                ClearCache();

            RowCount = newCount;
            RowCountChanged?.Invoke(this, EventArgs.Empty);
        }

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
            DataValidityToken = new object();
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
    }
}
