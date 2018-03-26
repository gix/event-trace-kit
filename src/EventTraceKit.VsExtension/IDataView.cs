namespace EventTraceKit.VsExtension
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Windows;
    using Expression = System.Linq.Expressions.Expression;

    public interface IDataView
    {
        DataViewColumnsCollection Columns { get; }
        DataViewColumnsCollection VisibleColumns { get; }
        int RowCount { get; }
        event EventHandler RowCountChanged;

        CellValue GetCellValue(int rowIndex, int columnIndex);
        void UpdateRowCount(int rows);

        void BeginDataUpdate();
        bool EndDataUpdate();
        void ApplyColumnView(DataColumnViewInfo[] dataColumnViewInfos);
        object DataValidityToken { get; }
        bool IsValidDataValidityToken(object dataValidityToken);
        DataColumnView CreateDataColumnViewFromInfo(DataColumnViewInfo dataColumnViewInfo);
        T GetInteractionWorkflow<T>(int? rowIndex, int? columnIndex) where T : class;
    }

    public sealed class DataViewColumnsCollection : IEnumerable<DataColumnView>
    {
        private readonly DataView view;

        public DataViewColumnsCollection(DataView view)
        {
            this.view = view;
        }

        public int IndexOf(DataColumnView column)
        {
            return view.GetDataColumnViewIndex(column);
        }

        public int Count => view.ColumnCount;

        public DataColumnView this[int columnIndex] => view.GetDataColumnView(columnIndex);

        public IEnumerator<DataColumnView> GetEnumerator()
        {
            for (int index = 0; index < Count; ++index)
                yield return this[index];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public sealed class DataColumnViewInfo
    {
        public IDataView View { get; set; }

        public Guid ColumnId { get; set; }
        public string Name { get; set; }
        public string HelpText { get; set; }
        public bool IsVisible { get; set; }
        public string Format { get; set; }
        public IFormatProvider FormatProvider { get; set; }
    }

    public abstract class DataColumnView
    {
        protected DataColumnView(DataColumn column, DataColumnViewInfo info)
        {
            Column = column;
            IsVisible = info.IsVisible;
            Format = info.Format;
            FormatProvider = info.FormatProvider;
        }

        public DataColumn Column { get; }

        public Guid ColumnId => Column.Id;
        public string Name => Column.Name;
        public string HelpText { get; set; }
        public bool IsVisible { get; set; }
        public string Format { get; set; }
        public IFormatProvider FormatProvider { get; set; }

        public CellValue GetCellValue(int index)
        {
            return GetCellValueCore(index);
        }

        protected abstract CellValue GetCellValueCore(int index);

        public object UntypedGetValue(int index)
        {
            return Column.UntypedGetValue(index);
        }
    }

    public sealed class DataColumnView<T> : DataColumnView
    {
        public DataColumnView(DataColumn<T> column, DataColumnViewInfo info)
            : base(column, info)
        {
            Column = column;
        }

        public new DataColumn<T> Column { get; }

        public T this[int index] => Column[index];

        protected sealed override CellValue GetCellValueCore(int index)
        {
            object value = UntypedGetValue(index);
            return new CellValue(value, FormatProvider, Format);
        }
    }

    public abstract class DataColumn
    {
        protected DataColumn(Type dataType)
        {
            DataType = dataType;
        }

        public Guid Id { get; set; }
        public string Name { get; set; }
        public double Width { get; set; }
        public bool IsVisible { get; set; }
        public bool IsResizable { get; set; }
        public TextAlignment TextAlignment { get; set; }
        public Type DataType { get; }

        public DataColumnView CreateView(DataColumnViewInfo info)
        {
            return CreateViewCore(info);
        }

        public object UntypedGetValue(int index)
        {
            return UntypedGetValueCore(index);
        }

        protected abstract DataColumnView CreateViewCore(DataColumnViewInfo info);
        protected abstract object UntypedGetValueCore(int index);

        public static DataColumn Create<T>()
        {
            return new DataColumn<T>();
        }

        public static DataColumn Create<T>(Func<int, T> generator)
        {
            return new DataColumn<T>(generator);
        }
    }

    public class DataColumn<T> : DataColumn
    {
        private readonly Func<int, T> generator;

        public DataColumn(Func<int, T> generator)
            : base(typeof(T))
        {
            this.generator = generator;
        }

        public DataColumn()
            : base(typeof(T))
        {
            generator = _ => default;
        }

        public new DataColumnView<T> CreateView(DataColumnViewInfo info)
        {
            return new DataColumnView<T>(this, info);
        }

        protected sealed override DataColumnView CreateViewCore(DataColumnViewInfo info)
        {
            return CreateView(info);
        }

        public T this[int index] => generator(index);

        protected sealed override object UntypedGetValueCore(int index)
        {
            return this[index];
        }
    }
}
