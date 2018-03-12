namespace EventTraceKit.VsExtension.Views
{
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
            private readonly TraceEventsDataView view;
            private readonly int? rowIndex;
            private readonly int? columnIndex;

            public WorkflowProvider(TraceEventsDataView view, int? rowIndex, int? columnIndex)
            {
                this.view = view;
                this.rowIndex = rowIndex;
                this.columnIndex = columnIndex;
            }

            public T GetWorkflow<T>() where T : class
            {
                if (typeof(T) == typeof(IContextMenuWorkflow))
                    return this as T;
                return null;
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
                    Command = new DelegateCommand(CopyValue)
                };

                yield return new Separator();

                var filterableColumns = view.Columns.Where(x => x.Column.FilterSelector != null)
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
            }

            private bool CanFilter(object obj)
            {
                var column = obj as DataColumnView ?? view.GetDataColumnView(columnIndex.Value);
                return column.Column.FilterSelector != null;
            }

            private void IncludeValue(object obj)
            {
                var column = obj as DataColumnView ?? view.GetDataColumnView(columnIndex.Value);
                ModifyFilter(column, FilterConditionAction.Include);
            }

            private void ExcludeValue(object obj)
            {
                var column = obj as DataColumnView ?? view.GetDataColumnView(columnIndex.Value);
                ModifyFilter(column, FilterConditionAction.Exclude);
            }

            private void CopyValue(object obj)
            {
                Debug.Assert(rowIndex != null);
                Debug.Assert(columnIndex != null);

                var value = view.GetCellValue(rowIndex.Value, columnIndex.Value).ToString();
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
                Debug.Assert(column.Column.FilterSelector != null);

                var value = column.UntypedGetValue(rowIndex.Value);
                filter.Conditions.Add(new TraceLogFilterCondition(
                    column.Column.FilterSelector, true, FilterRelationKind.Equal,
                    action, value));
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
