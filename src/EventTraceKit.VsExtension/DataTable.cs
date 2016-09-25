namespace EventTraceKit.VsExtension
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;

    public class DataTable : ICloneable
    {
        private readonly List<DataColumn> columns = new List<DataColumn>();
        private readonly Dictionary<Guid, DataColumn> mapColumnGuidToColumn =
            new Dictionary<Guid, DataColumn>();
        private readonly Dictionary<string, DataColumn> mapColumnNameToColumn =
            new Dictionary<string, DataColumn>();

        public DataTable(string tableName)
        {
            TableName = tableName;
        }

        public string TableName { get; }
        internal int Count => columns.Count;
        public DataTableColumnCollection Columns => new DataTableColumnCollection(this);

        internal DataColumn this[int index] => columns[index];
        internal DataColumn this[string columnName] => mapColumnNameToColumn[columnName];
        internal DataColumn this[Guid columnGuid] => mapColumnGuidToColumn[columnGuid];

        internal void Add(DataColumn column)
        {
            if (column == null)
                throw new ArgumentNullException(nameof(column));
            if (column.Name == null)
                throw new ArgumentException("Column must have a name.");
            if (column.Id == Guid.Empty)
                throw new ArgumentException("Column must have an id.");

            if (mapColumnNameToColumn.ContainsKey(column.Name))
                throw new InvalidOperationException(
                    $"DataTable already contains a column named {column.Name}");
            if (mapColumnGuidToColumn.ContainsKey(column.Id))
                throw new InvalidOperationException(
                    $"DataTable already contains a column with ID {column.Id}.");

            columns.Add(column);
            mapColumnNameToColumn[column.Name] = column;
            mapColumnGuidToColumn[column.Id] = column;
        }

        object ICloneable.Clone() => Clone();

        public DataTable Clone()
        {
            var table = new DataTable(TableName);
            foreach (DataColumn column in Columns)
                table.Add(column);

            return table;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DataTableColumnCollection : IEnumerable<DataColumn>
    {
        private readonly DataTable table;

        public DataTableColumnCollection(DataTable table)
        {
            if (table == null)
                throw new ArgumentNullException(nameof(table));
            this.table = table;
        }

        public DataColumn this[int index] => table[index];
        public DataColumn this[Guid id] => table[id];
        public DataColumn this[string name] => table[name];

        public void Add(DataColumn column)
        {
            if (column == null)
                throw new ArgumentNullException(nameof(column));
            table.Add(column);
        }

        public IEnumerator<DataColumn> GetEnumerator()
        {
            return table.Columns.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
