namespace EventTraceKit.VsExtension.Views
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Windows.Controls;
    using EventTraceKit.VsExtension.Extensions;
    using EventTraceKit.VsExtension.Filtering;
    using EventTraceKit.VsExtension.Formatting;
    using Microsoft.VisualStudio.PlatformUI;

    public class TraceEventsDataView : DataView
    {
        private readonly IFilterable filterable;

        public TraceEventsDataView(DataTable table, IFilterable filterable)
            : base(table, new DefaultFormatProviderSource())
        {
            this.filterable = filterable;
        }

        public override T GetInteractionWorkflow<T>(int? rowIndex, int? columnIndex)
        {
            return new WorkflowProvider(this, rowIndex, columnIndex).GetWorkflow<T>();
        }

        private class WorkflowProvider : IInteractionWorkflowProvider, IContextMenuWorkflow
        {
            private static readonly Dictionary<Guid, Guid> filterableColumnsMap = new Dictionary<Guid, Guid> {
                {GenericEventsViewModelSource.ProviderIdColumnId, Guid.Empty},
                {GenericEventsViewModelSource.ProviderNameColumnId, GenericEventsViewModelSource.ProviderIdColumnId},
                {GenericEventsViewModelSource.IdColumnId, Guid.Empty},
                {GenericEventsViewModelSource.VersionColumnId, Guid.Empty},
                {GenericEventsViewModelSource.ChannelColumnId, Guid.Empty},
                {GenericEventsViewModelSource.ChannelNameColumnId, GenericEventsViewModelSource.ChannelColumnId},
                {GenericEventsViewModelSource.TaskColumnId, Guid.Empty},
                {GenericEventsViewModelSource.TaskNameColumnId, GenericEventsViewModelSource.TaskColumnId},
                {GenericEventsViewModelSource.OpcodeColumnId, Guid.Empty},
                {GenericEventsViewModelSource.OpcodeNameColumnId, GenericEventsViewModelSource.OpcodeColumnId},
                {GenericEventsViewModelSource.LevelColumnId, Guid.Empty},
                {GenericEventsViewModelSource.LevelNameColumnId, GenericEventsViewModelSource.LevelColumnId},
                {GenericEventsViewModelSource.KeywordColumnId, Guid.Empty},
                {GenericEventsViewModelSource.ProcessIdColumnId, Guid.Empty},
                {GenericEventsViewModelSource.ThreadIdColumnId, Guid.Empty},
                {GenericEventsViewModelSource.DecodingSourceColumnId, Guid.Empty},
            };
            private static readonly Dictionary<Guid, string> columnToFieldMap = new Dictionary<Guid, string> {
                {GenericEventsViewModelSource.ProviderIdColumnId, "ProviderId"},
                {GenericEventsViewModelSource.IdColumnId, "Id"},
                {GenericEventsViewModelSource.VersionColumnId, "Version"},
                {GenericEventsViewModelSource.ChannelColumnId, "Channel"},
                {GenericEventsViewModelSource.TaskColumnId, "Task"},
                {GenericEventsViewModelSource.OpcodeColumnId, "Opcode"},
                {GenericEventsViewModelSource.LevelColumnId, "Level"},
                {GenericEventsViewModelSource.KeywordColumnId, "Keyword"},
                {GenericEventsViewModelSource.ProcessIdColumnId, "ProcessId"},
                {GenericEventsViewModelSource.ThreadIdColumnId, "ThreadId"},
                {GenericEventsViewModelSource.DecodingSourceColumnId, "DecodingSource"},
            };


            private readonly TraceEventsDataView view;
            private readonly int? rowIndex;
            private readonly int? columnIndex;
            private readonly DataColumnView<Guid> providerIdColumn;

            public WorkflowProvider(TraceEventsDataView view, int? rowIndex, int? columnIndex)
            {
                this.view = view;
                this.rowIndex = rowIndex;
                this.columnIndex = columnIndex;

                providerIdColumn = (DataColumnView<Guid>)view.Columns.First(
                    x => x.ColumnId == GenericEventsViewModelSource.ProviderIdColumnId);

            }

            public T GetWorkflow<T>() where T : class
            {
                if (typeof(T) == typeof(IContextMenuWorkflow))
                    return this as T;
                return null;
            }

            private void Foo()
            {
            }

            public IEnumerable<object> GetItems()
            {
                //yield return new MenuItem {
                //    Header = "Show stack trace",
                //    Command = new DelegateCommand(ShowStrackTrace, HasStackTrace)
                //};

                if (rowIndex == null || columnIndex == null)
                    yield break;

                var valuePreview = view.GetCellValue(rowIndex.Value, columnIndex.Value)
                    .ToString().TrimToLength(50);

                var includeCommand = new DelegateCommand(IncludeValue, CanFilter);
                var excludeCommand = new DelegateCommand(ExcludeValue, CanFilter);
                var copyCommand = new DelegateCommand(CopyValue, CanCopy);

                yield return new MenuItem {
                    Header = $"Include '{valuePreview}'",
                    Command = includeCommand
                };
                yield return new MenuItem {
                    Header = $"Exclude '{valuePreview}'",
                    Command = excludeCommand
                };
                yield return new MenuItem {
                    Header = $"Copy '{valuePreview}'",
                    Command = copyCommand
                };

                yield return new Separator();

                var filterableColumns = view.Columns.Where(x => filterableColumnsMap.ContainsKey(x.ColumnId))
                    .OrderBy(x => x.Name).ToList();

                var includeMenu = new MenuItem { Header = "Include" };
                foreach (var column in filterableColumns) {
                    includeMenu.Items.Add(new MenuItem {
                        Header = column.Name,
                        Command = includeCommand,
                        CommandParameter = column
                    });
                }
                yield return includeMenu;

                var excludeMenu = new MenuItem { Header = "Exclude" };
                foreach (var column in filterableColumns) {
                    excludeMenu.Items.Add(new MenuItem {
                        Header = column.Name,
                        Command = excludeCommand,
                        CommandParameter = column
                    });
                }
                yield return excludeMenu;

                var copyMenu = new MenuItem { Header = "Copy" };
                foreach (var column in view.Columns) {
                    copyMenu.Items.Add(new MenuItem {
                        Header = column.Name,
                        Command = copyCommand,
                        CommandParameter = column
                    });
                }
                yield return copyMenu;
            }

            private DataColumnView GetColumn(object obj)
            {
                if (obj is DataColumnView column)
                    return column;
                if (columnIndex != null)
                    return view.GetDataColumnView(columnIndex.Value);
                return null;
            }

            private bool CanFilter(object obj)
            {
                if (rowIndex == null)
                    return false;
                var column = GetColumn(obj);
                if (column == null)
                    return false;
                return filterableColumnsMap.ContainsKey(column.ColumnId);
            }

            private void IncludeValue(object obj)
            {
                ModifyFilter(GetColumn(obj), FilterConditionAction.Include);
            }

            private void ExcludeValue(object obj)
            {
                ModifyFilter(GetColumn(obj), FilterConditionAction.Exclude);
            }

            private bool CanCopy(object obj)
            {
                return rowIndex != null && GetColumn(obj) != null;
            }

            private void CopyValue(object obj)
            {
                var value = GetColumn(obj).GetCellValue(rowIndex.Value).ToString();
                ClipboardUtils.SetText(value);
            }

            private void ModifyFilter(DataColumnView column, FilterConditionAction action)
            {
                var filter = view.filterable.Filter ?? new TraceLogFilter();
                ModifyFilter(filter, column, action);
                view.filterable.Filter = filter;
            }

            private void ModifyFilter(
                TraceLogFilter filter, DataColumnView column, FilterConditionAction action)
            {
                Debug.Assert(rowIndex != null);

                var columnId = filterableColumnsMap[column.ColumnId];
                if (columnId != Guid.Empty)
                    column = view.Columns.First(x => x.ColumnId == columnId);
                else
                    columnId = column.ColumnId;

                var providerId = providerIdColumn[rowIndex.Value];

                string expr;
                if (columnId == providerIdColumn.ColumnId) {
                    expr = $"ProviderId == {providerId:B}";
                } else {
                    var field = columnToFieldMap[columnId];
                    var value = FormatValue(column.UntypedGetValue(rowIndex.Value));
                    expr = $"ProviderId == {providerId:B} && {field} == {value}";
                }

                filter.Conditions.Add(new TraceLogFilterCondition(expr, true, action));
            }

            private string FormatValue(object value)
            {
                if (value is Guid guid)
                    return guid.ToString("B");
                if (value is string str)
                    return "\"" + str.Replace("\"", "\"\"") + "\"";
                return value?.ToString();
            }

            //private bool HasStackTrace(object obj)
            //{
            //    return row != null && view.HasStackTrace(row.Value);
            //}

            //private void ShowStrackTrace(object obj)
            //{
            //    var stackTrace = view.ProjectStackTrace(row.Value);
            //    if (stackTrace == null)
            //        return;

            //    var window = new StackTraceView();
            //    var viewModel = new StackTraceViewModel(stackTrace);
            //    window.DataContext = viewModel;
            //    window.ShowModal();
            //}
        }
    }
}
