namespace EventTraceKit.VsExtension.UITests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Threading;
    using Controls;
    using EventTraceKit.VsExtension.Views;
    using EventTraceKit.VsExtension.Views.PresetManager;
    using Formatting;
    using Native;

    public class AsyncDataGridTestViewModel : ViewModel
    {
        private bool isRunning;
        private AsyncDataGridViewModel gridModel;
        private int rowCount;

        private FontFamily rowFontFamily;
        private double rowFontSize;
        private Brush rowForeground;
        private Brush background;
        private Brush rowBackground;
        private Brush alternatingRowBackground;
        private Brush rowSelectionForeground;
        private Brush rowSelectionBackground;
        private Brush rowInactiveSelectionForeground;
        private Brush rowInactiveSelectionBackground;
        private Brush rowFocusBorderBrush;
        private DataView dataView;
        private AsyncDataViewModel adv;

        public AsyncDataGridTestViewModel()
        {
            StartCommand = new AsyncDelegateCommand(Start, CanStart);
            StopCommand = new AsyncDelegateCommand(Stop, CanStop);
            ClearCommand = new AsyncDelegateCommand(Clear);
            OpenViewEditorCommand = new AsyncDelegateCommand(OpenViewEditor);
            OpenFilterEditorCommand = new AsyncDelegateCommand(OpenFilterEditor);

            SelectableBrushes = new ObservableCollection<BrushEntry>();
            foreach (var property in typeof(Brushes).GetProperties(BindingFlags.Static | BindingFlags.Public).OrderBy(x => x.Name)) {
                var brush = (Brush)property.GetValue(null);

                BrushEntry.KnownBrushes[brush] = property.Name;
                SelectableBrushes.Add(new BrushEntry(property.Name, brush));
            }

            PropertyEditors = new ObservableCollection<PropertyEditor>();
            PropertyEditors.Add(new SliderPropertyEditor(nameof(RowFontSize), x => RowFontSize = x));
            PropertyEditors.Add(new BrushPropertyEditor(nameof(Background), SelectableBrushes, x => Background = x));
            PropertyEditors.Add(new BrushPropertyEditor(nameof(RowForeground), SelectableBrushes, x => RowForeground = x));
            PropertyEditors.Add(new BrushPropertyEditor(nameof(RowBackground), SelectableBrushes, x => RowBackground = x));
            PropertyEditors.Add(new BrushPropertyEditor(nameof(RowBackground) + "Alt", SelectableBrushes, x => AlternatingRowBackground = x));
            PropertyEditors.Add(new BrushPropertyEditor(nameof(RowSelectionForeground), SelectableBrushes, x => RowSelectionForeground = x));
            PropertyEditors.Add(new BrushPropertyEditor(nameof(RowSelectionBackground), SelectableBrushes, x => RowSelectionBackground = x));
            PropertyEditors.Add(new BrushPropertyEditor(nameof(RowInactiveSelectionForeground), SelectableBrushes, x => RowInactiveSelectionForeground = x));
            PropertyEditors.Add(new BrushPropertyEditor(nameof(RowInactiveSelectionBackground), SelectableBrushes, x => RowInactiveSelectionBackground = x));
            PropertyEditors.Add(new BrushPropertyEditor(nameof(RowFocusBorderBrush), SelectableBrushes, x => RowFocusBorderBrush = x));

            RowFontFamily = new FontFamily("Consolas");
            RowFontSize = (double)new FontSizeConverter().ConvertFromInvariantString("9pt");
            RowForeground = Brushes.Black;
            Background = Brushes.White;
            RowBackground = Brushes.WhiteSmoke;
            AlternatingRowBackground = Brushes.AliceBlue;
            RowSelectionForeground = Brushes.White;
            RowSelectionBackground = Brushes.DarkBlue;
            RowInactiveSelectionForeground = Brushes.DimGray;
            RowInactiveSelectionBackground = Brushes.LightGray;
            RowFocusBorderBrush = Brushes.GreenYellow;

            adv = CreateModel(out dataView);
            GridModel = adv.GridViewModel;

            RowCount = 10;
        }

        public DataView DataView => dataView;

        public bool IsRunning
        {
            get { return isRunning; }
            set { SetProperty(ref isRunning, value); }
        }

        private bool CanStart()
        {
            return !IsRunning;
        }

        private Task Start()
        {
            IsRunning = true;
            CommandManager.InvalidateRequerySuggested();
            return Task.CompletedTask;
        }

        private bool CanStop()
        {
            return IsRunning;
        }

        private Task Stop()
        {
            IsRunning = false;
            CommandManager.InvalidateRequerySuggested();
            return Task.CompletedTask;
        }

        private Task Clear()
        {
            return Task.CompletedTask;
        }

        private Task OpenViewEditor()
        {
            var dialog = PresetManagerDialog.CreateDialog(adv);
            dialog.Owner = Application.Current.MainWindow;
            dialog.ShowDialog();
            return Task.CompletedTask;
        }

        private unsafe Task OpenFilterEditor()
        {
            var viewModel = new FilterDialogViewModel();
            var dialog = new FilterDialog { DataContext = viewModel };
            if (dialog.ShowDialog() != true)
                return Task.CompletedTask;

            var predicateExpr = viewModel.GetFilter().CreatePredicateExpr();
            var predicate = predicateExpr.CompileToTransientAssembly();
            MessageBox.Show(predicateExpr.ToString());

            var record = new EVENT_RECORD();
            record.EventHeader.EventDescriptor.Id = 23;
            var info = new TRACE_EVENT_INFO();
            MessageBox.Show(predicate((IntPtr)(&record), (IntPtr)(&info), (UIntPtr)Marshal.SizeOf(typeof(TRACE_EVENT_INFO))).ToString());

            return Task.CompletedTask;
        }

        private AsyncDataViewModel CreateModel(out DataView dataView)
        {
            var presetCollection = new AdvmPresetCollection();
            var dataTable = new DataTable("Test");
            var templatePreset = new AsyncDataViewModelPreset();
            templatePreset.Name = "Default";

            for (int i = 0; i < 20; ++i) {
                var columnPreset = new ColumnViewModelPreset();
                columnPreset.Id = Guid.NewGuid();
                columnPreset.Name = "Column " + i;
                columnPreset.IsVisible = i % 2 == 0;
                columnPreset.Width = 100;
                templatePreset.ConfigurableColumns.Add(columnPreset);

                int col = i;
                var dataColumn = new DataColumn<string>(row => $"C[{row},{col}]");
                dataColumn.Id = columnPreset.Id;
                dataColumn.Name = columnPreset.Name;
                dataColumn.Width = columnPreset.Width;
                dataColumn.IsVisible = columnPreset.IsVisible;
                dataColumn.TextAlignment = columnPreset.TextAlignment;
                dataColumn.IsResizable = true;
                dataTable.Columns.Add(dataColumn);
            }

            dataView = new DataView(dataTable, new DefaultFormatProviderSource());
            return new AsyncDataViewModel(
                new WorkManager(Dispatcher.CurrentDispatcher),
                dataView, templatePreset, templatePreset, presetCollection);
        }

        public ICommand StartCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand OpenViewEditorCommand { get; }
        public ICommand OpenFilterEditorCommand { get; }

        public ObservableCollection<BrushEntry> SelectableBrushes { get; }
        public ObservableCollection<PropertyEditor> PropertyEditors { get; }

        public AsyncDataGridViewModel GridModel
        {
            get { return gridModel; }
            set { SetProperty(ref gridModel, value); }
        }

        public int RowCount
        {
            get { return rowCount; }
            set
            {
                if (SetProperty(ref rowCount, value))
                    DataView.UpdateRowCount(value);
            }
        }

        public FontFamily RowFontFamily
        {
            get { return rowFontFamily; }
            set { SetProperty(ref rowFontFamily, value); }
        }

        public double RowFontSize
        {
            get { return rowFontSize; }
            set { SetProperty(ref rowFontSize, value); }
        }

        public Brush RowForeground
        {
            get { return rowForeground; }
            set { SetProperty(ref rowForeground, value); }
        }

        public Brush Background
        {
            get { return background; }
            set { SetProperty(ref background, value); }
        }

        public Brush RowBackground
        {
            get { return rowBackground; }
            set { SetProperty(ref rowBackground, value); }
        }

        public Brush AlternatingRowBackground
        {
            get { return alternatingRowBackground; }
            set { SetProperty(ref alternatingRowBackground, value); }
        }

        public Brush RowSelectionForeground
        {
            get { return rowSelectionForeground; }
            set { SetProperty(ref rowSelectionForeground, value); }
        }

        public Brush RowSelectionBackground
        {
            get { return rowSelectionBackground; }
            set { SetProperty(ref rowSelectionBackground, value); }
        }

        public Brush RowInactiveSelectionForeground
        {
            get { return rowInactiveSelectionForeground; }
            set { SetProperty(ref rowInactiveSelectionForeground, value); }
        }

        public Brush RowInactiveSelectionBackground
        {
            get { return rowInactiveSelectionBackground; }
            set { SetProperty(ref rowInactiveSelectionBackground, value); }
        }

        public Brush RowFocusBorderBrush
        {
            get { return rowFocusBorderBrush; }
            set { SetProperty(ref rowFocusBorderBrush, value); }
        }
    }

    public class BrushEntry
    {
        public BrushEntry(Brush brush)
        {
            Name = brush.ToString();
            Brush = brush;
        }

        public BrushEntry(string name, Brush brush)
        {
            Name = name;
            Brush = brush;
        }

        public static implicit operator BrushEntry(Brush brush)
        {
            return new BrushEntry(FindKnownBrush(brush), brush);
        }

        public static readonly Dictionary<Brush, string> KnownBrushes = new Dictionary<Brush, string>();

        private static string FindKnownBrush(Brush brush)
        {
            KnownBrushes.TryGetValue(brush, out var name);
            return name ?? brush.ToString();
        }

        public string Name { get; }
        public Brush Brush { get; }
    }

    public abstract class PropertyEditor : ViewModel
    {
    }

    public abstract class PropertyEditor<T> : PropertyEditor
    {
        private T value;

        public T Value
        {
            get { return value; }
            set
            {
                if (SetProperty(ref this.value, value))
                    OnValueChanged(value);
            }
        }

        protected virtual void OnValueChanged(T newValue)
        {
        }
    }

    public class BrushPropertyEditor : PropertyEditor<BrushEntry>
    {
        private readonly Action<Brush> onChanged;

        public BrushPropertyEditor(
            string propertyName,
            ObservableCollection<BrushEntry> selectableBrushes, Action<Brush> onChanged)
        {
            this.onChanged = onChanged;
            PropertyName = propertyName;
            SelectableBrushes = selectableBrushes;
        }

        public string PropertyName { get; }

        public ObservableCollection<BrushEntry> SelectableBrushes { get; }

        protected override void OnValueChanged(BrushEntry newValue)
        {
            onChanged(newValue.Brush);
        }
    }

    public class SliderPropertyEditor : PropertyEditor<double>
    {
        private readonly Action<double> onChanged;

        public SliderPropertyEditor(
            string propertyName, Action<double> onChanged)
        {
            this.onChanged = onChanged;
            PropertyName = propertyName;
        }

        public string PropertyName { get; }

        protected override void OnValueChanged(double newValue)
        {
            onChanged(newValue);
        }
    }
}
