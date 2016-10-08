namespace EventTraceKit.VsExtension
{
    using System;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.Linq;
    using System.Windows;
    using System.Windows.Automation;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Documents;
    using System.Windows.Media;
    using Collections;
    using Controls;
    using Windows;

    public partial class PresetManagerDialog
    {
        public PresetManagerDialog()
        {
            InitializeComponent();
        }

        private PresetManagerDialog(AsyncDataGrid associatedView)
            : this()
        {
        }

        public static IValueConverter MultiplierConverter { get; } =
             new DelegateValueConverter<double, double, double>((x, param) => x * param, null);

        public static IValueConverter WidthToStringConverter { get; } =
            new DelegateValueConverter<int, string>(
                x => x.ToString(CultureInfo.CurrentCulture),
                x => {
                    int width;
                    if (!int.TryParse(x, NumberStyles.Integer, CultureInfo.CurrentCulture, out width))
                        return 100;
                    return width;
                });

        public static IValueConverter SeparatorToColorBrushConverter { get; } =
            new DelegateValueConverter<PresetManagerColumnViewModel, Brush>(x => {
                switch (x.ColumnType) {
                    case PresetManagerColumnType.LeftFreezableAreaSeparator:
                        return Brushes.Gray;
                    case PresetManagerColumnType.RightFreezableAreaSeparator:
                        return Brushes.Gray;
                    default:
                        return Brushes.Black;
                }
            });

        public static readonly DependencyProperty HeaderBrushProperty =
            DependencyProperty.Register(nameof(HeaderBrush), typeof(Brush), typeof(PresetManagerDialog), new PropertyMetadata());

        public Brush HeaderBrush
        {
            get { return (Brush)GetValue(HeaderBrushProperty); }
            set { SetValue(HeaderBrushProperty, value); }
        }

        private void AvailableListDropHandler(object sender, DragEventArgs e) { }
        private void LayoutListDropHandler(object sender, DragEventArgs e) { }
        private void SaveButtonClickHandler(object sender, RoutedEventArgs e) { }
        private void CloseButtonClickHandler(object sender, RoutedEventArgs e) { }
        private void CancelButtonClickHandler(object sender, RoutedEventArgs e) { }
        private void ApplyButtonClickHandler(object sender, RoutedEventArgs e) { }

        public static void ShowPresetManagerDialog(
            AsyncDataViewModel hdvViewModel, AsyncDataGrid associatedView)
        {
            if (hdvViewModel == null)
                throw new ArgumentNullException(nameof(hdvViewModel));
            //if (associatedView == null)
            //    throw new ArgumentNullException(nameof(associatedView));

            var model = new PresetManagerViewModel(hdvViewModel);
            var dialog = new PresetManagerDialog(associatedView) {
                DataContext = model,
                //viewModel = model,
                //HeaderBrush = associatedView.HeaderBrush,
                //GraphTreeItemView = associatedView
            };
            dialog.ShowModal();
        }
    }

    public static class CollectionUtils
    {
        public static ReadOnlyObservableCollection<T> InitializeReadOnly<T>(
            out ObservableCollection<T> collection)
        {
            collection = new ObservableCollection<T>();
            return new ReadOnlyObservableCollection<T>(collection);
        }
    }

    public class PresetManagerViewModel : DependencyObject
    {
        private readonly ObservableCollection<ColumnViewModelPreset> templateColumns;
        private readonly ObservableCollection<PresetManagerColumnViewModel> configurablePresetColumns;
        private readonly ObservableCollection<PresetManagerColumnViewModel> presetColumns;
        private readonly PresetManagerColumnViewModel leftFreezableAreaSeparatorColumn;
        private readonly PresetManagerColumnViewModel rightFreezableAreaSeparatorColumn;
        private AsyncDataViewModelPreset templatePreset;
        private AsyncDataViewModelPreset currentPreset;
        private bool refreshingFromPreset;

        public PresetManagerViewModel(AsyncDataViewModel advModel)
        {
            TemplateColumns = CollectionUtils.InitializeReadOnly(out templateColumns);
            PresetColumns = CollectionUtils.InitializeReadOnly(out presetColumns);
            ConfigurablePresetColumns = CollectionUtils.InitializeReadOnly(out configurablePresetColumns);

            leftFreezableAreaSeparatorColumn = new PresetManagerColumnViewModel(this, PresetManagerColumnType.LeftFreezableAreaSeparator);
            rightFreezableAreaSeparatorColumn = new PresetManagerColumnViewModel(this, PresetManagerColumnType.RightFreezableAreaSeparator);

            HdvViewModel = advModel;
        }

        public static readonly DependencyProperty DisplayNameProperty = DependencyProperty.Register(
            nameof(DisplayName), typeof(string), typeof(PresetManagerViewModel), new PropertyMetadata(default(object)));

        private static readonly DependencyPropertyKey HdvViewModelPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(HdvViewModel),
                typeof(AsyncDataViewModel),
                typeof(PresetManagerViewModel),
                new PropertyMetadata(
                    null,
                    (d, e) => ((PresetManagerViewModel)d).HdvViewModelPropertyChangedHandler(e)));

        public static readonly DependencyProperty HdvViewModelProperty =
            HdvViewModelPropertyKey.DependencyProperty;

        private static readonly DependencyPropertyKey TemplateColumnsPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(TemplateColumns),
                typeof(ReadOnlyObservableCollection<ColumnViewModelPreset>),
                typeof(PresetManagerViewModel),
                new PropertyMetadata(null));

        public static readonly DependencyProperty TemplateColumnsProperty =
            TemplateColumnsPropertyKey.DependencyProperty;

        private static readonly DependencyPropertyKey PresetColumnsPropertyKey =
            DependencyProperty.RegisterReadOnly("PresetColumns",
                typeof(ReadOnlyObservableCollection<PresetManagerColumnViewModel>), typeof(PresetManagerViewModel),
                new PropertyMetadata(null));

        public static readonly DependencyProperty PresetColumnsProperty = PresetColumnsPropertyKey.DependencyProperty;

        private static readonly DependencyPropertyKey ConfigurablePresetColumnsPropertyKey =
            DependencyProperty.RegisterReadOnly("ConfigurablePresetColumns",
                typeof(ReadOnlyObservableCollection<PresetManagerColumnViewModel>), typeof(PresetManagerViewModel),
                new PropertyMetadata(null));

        public static readonly DependencyProperty ConfigurablePresetColumnsProperty =
            ConfigurablePresetColumnsPropertyKey.DependencyProperty;

        public static readonly DependencyProperty SelectedColumnProperty = DependencyProperty.Register(
            "SelectedColumn", typeof(PresetManagerColumnViewModel), typeof(PresetManagerViewModel),
            new PropertyMetadata(default(PresetManagerColumnViewModel)));

        public int LastLeftFrozenIndex => presetColumns.IndexOf(leftFreezableAreaSeparatorColumn);
        public int FirstRightFrozenIndex => presetColumns.IndexOf(rightFreezableAreaSeparatorColumn);

        public AsyncDataViewModel HdvViewModel
        {
            get { return (AsyncDataViewModel)GetValue(HdvViewModelProperty); }
            private set { SetValue(HdvViewModelPropertyKey, value); }

        }

        public ReadOnlyObservableCollection<PresetManagerColumnViewModel> ConfigurablePresetColumns
        {
            get
            {
                return (ReadOnlyObservableCollection<PresetManagerColumnViewModel>)
                    GetValue(ConfigurablePresetColumnsProperty);
            }
            private set { SetValue(ConfigurablePresetColumnsPropertyKey, value); }
        }

        private void HdvViewModelPropertyChangedHandler(DependencyPropertyChangedEventArgs e)
        {
            var oldValue = (AsyncDataViewModel)e.OldValue;
            if (oldValue != null) {
                //oldValue.PresetCollection.AvailablePresetsChanged -= new EventHandler(this.AvailablePresetsChangedHandler);
                oldValue.PresetChanged -= PresetChangedHandler;
            }

            var newValue = (AsyncDataViewModel)e.NewValue;
            if (newValue != null) {
                //newValue.PresetCollection.AvailablePresetsChanged += new EventHandler(this.AvailablePresetsChangedHandler);
                newValue.PresetChanged += PresetChangedHandler;
                RefreshFromHdvViewModel();
            }
        }

        private void RefreshFromHdvViewModel()
        {
            templatePreset = HdvViewModel.TemplatePreset;
            templateColumns.Clear();
            templateColumns.AddRange(
                templatePreset.ConfigurableColumns.OrderBy(x => x.Name));
            RefreshFromPreset(HdvViewModel.Preset);
        }

        private void RefreshFromPreset(AsyncDataViewModelPreset preset)
        {
            refreshingFromPreset = true;
            currentPreset = preset.CreateCompatiblePreset(templatePreset);

            presetColumns.Clear();
            for (int i = 0; i < currentPreset.ConfigurableColumns.Count; ++i) {
                var columnPreset = currentPreset.ConfigurableColumns[i];
                DataColumnView prototypeViewForColumnPreset = HdvViewModel.GetPrototypeViewForColumnPreset(columnPreset);
                var item = new PresetManagerColumnViewModel(this, currentPreset.ConfigurableColumns[i], prototypeViewForColumnPreset);
                presetColumns.Add(item);
            }

            //List<Tuple<int, PresetManagerColumnViewModel>> list1 = new List<Tuple<int, PresetManagerColumnViewModel>> {
            //    new Tuple<int, PresetManagerColumnViewModel>(this.currentPreset.LeftFrozenColumnCount, this.leftFreezableAreaSeparatorColumn),
            //    new Tuple<int, PresetManagerColumnViewModel>(this.currentPreset.RightFrozenColumnCount, this.rightFreezableAreaSeparatorColumn),
            //};
            //list1.Sort((x, y) => x.Item1 - y.Item1);
            //foreach (var struct2 in list1) {
            //    this.presetColumns.Insert(struct2.Item1, struct2.Item2);
            //}

            foreach (var column in presetColumns)
                column.RefreshPositionDependentProperties();

            //this.CurrentSelectedPresetName = this.currentPreset.Name;
            //this.SetDialogStateDirty(this.currentPreset.IsModified);
            refreshingFromPreset = false;
        }

        private void PresetChangedHandler(object sender, ValueChangedEventArgs<AsyncDataViewModelPreset> e)
        {
        }

        public string DisplayName
        {
            get { return (string)GetValue(DisplayNameProperty); }
            set { SetValue(DisplayNameProperty, value); }
        }

        public ReadOnlyObservableCollection<ColumnViewModelPreset> TemplateColumns
        {
            get { return (ReadOnlyObservableCollection<ColumnViewModelPreset>)GetValue(TemplateColumnsProperty); }
            private set { SetValue(TemplateColumnsPropertyKey, value); }
        }

        public PresetManagerColumnViewModel SelectedColumn
        {
            get { return (PresetManagerColumnViewModel)GetValue(SelectedColumnProperty); }
            set { SetValue(SelectedColumnProperty, value); }
        }

        public ReadOnlyObservableCollection<PresetManagerColumnViewModel> PresetColumns
        {
            get { return (ReadOnlyObservableCollection<PresetManagerColumnViewModel>)GetValue(PresetColumnsProperty); }
            private set { SetValue(PresetColumnsPropertyKey, value); }
        }
    }

    public class PresetManagerColumnContainerStyleSelector : StyleSelector
    {
        public override Style SelectStyle(object item, DependencyObject container)
        {
            var model = item as PresetManagerColumnViewModel;
            if (model == null)
                return base.SelectStyle(item, container);
            if (model.ColumnType == PresetManagerColumnType.Configurable)
                return ConfigurableColumnStyle;
            return SeparatorColumnStyle;
        }

        public Style ConfigurableColumnStyle { get; set; }
        public Style SeparatorColumnStyle { get; set; }
    }

    public class PresetManagerColumnDetailsTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            PresetManagerColumnViewModel model = item as PresetManagerColumnViewModel;
            if (model == null)
                return base.SelectTemplate(item, container);

            switch (model.ColumnType) {
                case PresetManagerColumnType.LeftFreezableAreaSeparator:
                    return LeftFreezableAreaSeparatorColumnTemplate;
                case PresetManagerColumnType.RightFreezableAreaSeparator:
                    return RightFreezableAreaSeparatorColumnTemplate;
                case PresetManagerColumnType.Configurable:
                    return ConfigurableColumnTemplate;
                default:
                    throw ExceptionUtils.InvalidEnumArgumentException(
                        model.ColumnType, "model.ColumnType");
            }
        }

        public DataTemplate ConfigurableColumnTemplate { get; set; }
        public DataTemplate LeftFreezableAreaSeparatorColumnTemplate { get; set; }
        public DataTemplate RightFreezableAreaSeparatorColumnTemplate { get; set; }
    }

    public enum PresetManagerColumnType
    {
        Configurable,
        LeftFreezableAreaSeparator,
        RightFreezableAreaSeparator,
    }

    public class PresetManagerColumnViewModel : DependencyObject
    {
        private readonly ColumnViewModelPreset preset;
        private bool refreshingFromPreset;

        public PresetManagerColumnViewModel(
            PresetManagerViewModel presetManager, PresetManagerColumnType columnType)
        {
            PresetManager = presetManager;
            ColumnType = columnType;
        }

        public PresetManagerColumnViewModel(
            PresetManagerViewModel presetManager,
            ColumnViewModelPreset preset,
            DataColumnView columnView)
        {
            if (presetManager == null)
                throw new ArgumentNullException(nameof(presetManager));
            if (preset == null)
                throw new ArgumentNullException(nameof(preset));
            if (columnView == null)
                throw new ArgumentNullException(nameof(columnView));

            //if (preset.Name != columnView.ColumnName) {
            //    if (preset.IsFrozen)
            //        preset = preset.Clone();
            //    preset.Name = columnView.ColumnName;
            //}

            PresetManager = presetManager;
            this.preset = preset;
            //this.defaultSupportedFormat = columnView.FormatProvider.DefaultSupportedFormat();
            //this.supportedFormats = columnView.FormatProvider.SupportedFormats();
            RefreshFromPreset();
            //this.CellFormat = this.GetSupportedFormatFromFormatString(columnView.Format);
            //this.DataType = columnView.DataType;
            ColumnType = PresetManagerColumnType.Configurable;
        }

        private void RefreshFromPreset()
        {
            refreshingFromPreset = true;
            Name = preset.Name;
            AutomationProperties.SetName(this, preset.Name);
            Id = preset.Id;
            Width = preset.Width;
            IsVisible = preset.IsVisible;
            TextAlignment = preset.TextAlignment;
            //HelpText = preset.HelpText;
            refreshingFromPreset = false;
        }

        public PresetManagerViewModel PresetManager { get; }
        public PresetManagerColumnType ColumnType { get; }

        #region public Guid Id { get; set; }

        /// <summary>
        ///   Identifies the <see cref="Id"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IdProperty =
            DependencyProperty.Register(
                nameof(Id),
                typeof(Guid),
                typeof(PresetManagerColumnViewModel),
                new PropertyMetadata(null));

        /// <summary>
        ///   Gets or sets the id.
        /// </summary>
        public Guid Id
        {
            get { return (Guid)GetValue(IdProperty); }
            set { SetValue(IdProperty, value); }
        }

        #endregion

        #region public string Name { get; private set; }

        private static readonly DependencyPropertyKey NamePropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(Name),
                typeof(string),
                typeof(PresetManagerColumnViewModel),
                new PropertyMetadata(string.Empty));

        /// <summary>
        ///   Identifies the <see cref="Name"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty NameProperty =
            NamePropertyKey.DependencyProperty;

        /// <summary>
        ///   Gets the name.
        /// </summary>
        public string Name
        {
            get { return (string)GetValue(NameProperty); }
            private set { SetValue(NamePropertyKey, value); }
        }

        #endregion

        #region public int Width { get; set; }

        /// <summary>
        ///   Identifies the <see cref="Width"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty WidthProperty =
            DependencyProperty.Register(
                nameof(Width),
                typeof(int),
                typeof(PresetManagerColumnViewModel),
                new PropertyMetadata(
                    0,
                    (d, e) => ((PresetManagerColumnViewModel)d).OnPresetPropertyChanged(),
                    (d, v) => ((PresetManagerColumnViewModel)d).CoerceWidth(v)));

        /// <summary>
        ///   Gets or sets the width.
        /// </summary>
        public int Width
        {
            get { return (int)GetValue(WidthProperty); }
            set { SetValue(WidthProperty, value); }
        }

        private object CoerceWidth(object baseValue)
        {
            return Boxed.Int32(((int)baseValue).Clamp(0, 10000));
        }

        #endregion

        #region public bool IsVisible { get; set; }

        public static readonly DependencyProperty IsVisibleProperty =
            DependencyProperty.Register(
                nameof(IsVisible),
                typeof(bool),
                typeof(PresetManagerViewModel),
                new PropertyMetadata(
                    Boxed.True,
                    (d, e) => ((PresetManagerColumnViewModel)d).OnPresetPropertyChanged()));

        public bool IsVisible
        {
            get { return (bool)GetValue(IsVisibleProperty); }
            set { SetValue(IsVisibleProperty, value); }
        }

        #endregion

        #region public TextAlignment TextAlignment { get; set; }

        /// <summary>
        ///   Identifies the <see cref="TextAlignment"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TextAlignmentProperty =
            DependencyProperty.Register(
                nameof(TextAlignment),
                typeof(TextAlignment),
                typeof(PresetManagerColumnViewModel),
                new PropertyMetadata(
                    TextAlignment.Left,
                    (d, e) => ((PresetManagerColumnViewModel)d).OnPresetPropertyChanged()));

        /// <summary>
        ///   Gets or sets the text alignment.
        /// </summary>
        public TextAlignment TextAlignment
        {
            get { return (TextAlignment)GetValue(TextAlignmentProperty); }
            set { SetValue(TextAlignmentProperty, value); }
        }

        #endregion


        #region public bool IsFrozen { get; set; }

        /// <summary>
        ///   Identifies the <see cref="IsFrozen"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsFrozenProperty =
            DependencyProperty.Register(
                nameof(IsFrozen),
                typeof(bool),
                typeof(PresetManagerColumnViewModel),
                new PropertyMetadata(false));

        /// <summary>
        ///   Gets or sets whether the column is frozen.
        /// </summary>
        public bool IsFrozen
        {
            get { return (bool)GetValue(IsFrozenProperty); }
            set { SetValue(IsFrozenProperty, value); }
        }

        #endregion

        public void RefreshPositionDependentProperties()
        {
            int index = PresetManager.PresetColumns.IndexOf(this);
            IsFrozen = index >= 0 && ((index <= PresetManager.LastLeftFrozenIndex) || (index >= PresetManager.FirstRightFrozenIndex));
        }

        private void OnPresetPropertyChanged()
        {
            //if (!refreshingFromPreset && this.UpdateColumnPreset()) {
            //    PresetManager.SetDialogStateDirty(true);
            //    PresetManager.CoerceAreFiltersValid();
            //}
        }
    }

    public class BindableRichTextBox : RichTextBox
    {
        public static readonly DependencyProperty DocumentProperty =
            DependencyProperty.Register(
                nameof(Document),
                typeof(FlowDocument),
                typeof(BindableRichTextBox),
                new FrameworkPropertyMetadata(null, OnDocumentChanged));

        private static void OnDocumentChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            var source = (RichTextBox)d;
            var document = args.NewValue as FlowDocument ?? new FlowDocument();
            source.Document = document;
        }

        public new FlowDocument Document
        {
            get { return (FlowDocument)GetValue(DocumentProperty); }
            set { SetValue(DocumentProperty, value); }
        }
    }

    public class PresetManagerDesignTimeModel : PresetManagerViewModel
    {
        public PresetManagerDesignTimeModel()
            : base(CreateModel())
        {
        }

        private static AsyncDataViewModel CreateModel()
        {
            var idPreset = new ColumnViewModelPreset {
                Id = new Guid("A27E5F00-BCA0-4BFE-B43D-EAA4B3F20D42"),
                Name = "Id",
                IsVisible = true,
                Width = 80
            }.EnsureFrozen();
            var namePreset = new ColumnViewModelPreset {
                Id = new Guid("3050F05D-FDCC-43AC-AA63-72CF17E5B7FF"),
                Name = "Name",
                IsVisible = true,
                Width = 200
            }.EnsureFrozen();

            var template = new AsyncDataViewModelPreset();
            var table = new DataTable("Design");

            AddColumn(table, template, idPreset, DataColumn.Create(x => x));
            AddColumn(table, template, namePreset, DataColumn.Create(x => "Name" + x));

            return new AsyncDataViewModel(new DataView(table), template) {
                Preset = template
            };
        }

        private static void AddColumn(
            DataTable table, AsyncDataViewModelPreset preset,
            ColumnViewModelPreset columnPreset, DataColumn column)
        {
            column.Id = columnPreset.Id;
            column.Name = columnPreset.Name;
            column.Width = columnPreset.Width;
            column.IsVisible = columnPreset.IsVisible;
            column.IsResizable = true;
            column.TextAlignment = columnPreset.TextAlignment;
            preset.ConfigurableColumns.Add(columnPreset);
            table.Add(column);
        }
    }
}
