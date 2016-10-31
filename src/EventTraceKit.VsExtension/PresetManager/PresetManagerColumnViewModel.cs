namespace EventTraceKit.VsExtension
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows;
    using System.Windows.Automation;
    using Extensions;
    using Formatting;

    public class PresetManagerColumnViewModel : DependencyObject
    {
        private readonly SupportedFormat defaultSupportedFormat;
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

            PresetManager = presetManager;
            Preset = preset;
            ColumnType = PresetManagerColumnType.Configurable;

            defaultSupportedFormat = columnView.FormatProvider.DefaultSupportedFormat();
            SupportedFormats = columnView.FormatProvider.SupportedFormats();

            RefreshFromPreset();
        }

        public PresetManagerViewModel PresetManager { get; }
        public ColumnViewModelPreset Preset { get; }
        public PresetManagerColumnType ColumnType { get; }

        public IEnumerable<SupportedFormat> SupportedFormats { get; }
        public Visibility CellFormatVisibility =>
            SupportedFormats.Any() ? Visibility.Visible : Visibility.Collapsed;

        #region public Guid Id { get; private set; }

        private static readonly DependencyPropertyKey IdPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(Id),
                typeof(Guid),
                typeof(PresetManagerColumnViewModel),
                new PropertyMetadata(null));

        /// <summary>
        ///   Identifies the <see cref="Id"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IdProperty =
            IdPropertyKey.DependencyProperty;

        /// <summary>
        ///   Gets or sets the id.
        /// </summary>
        public Guid Id
        {
            get { return (Guid)GetValue(IdProperty); }
            private set { SetValue(IdPropertyKey, value); }
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
                typeof(PresetManagerColumnViewModel),
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

        #region public SupportedFormat CellFormat { get; set; }

        public static readonly DependencyProperty CellFormatProperty =
            DependencyProperty.Register(
                nameof(CellFormat),
                typeof(SupportedFormat),
                typeof(PresetManagerColumnViewModel),
                new PropertyMetadata(
                    new SupportedFormat(),
                    (d, e) => ((PresetManagerColumnViewModel)d).OnPresetPropertyChanged()));

        public SupportedFormat CellFormat
        {
            get { return (SupportedFormat)GetValue(CellFormatProperty); }
            private set { SetValue(CellFormatProperty, value); }
        }

        #endregion

        public void RefreshPositionDependentProperties()
        {
            int index = PresetManager.PresetColumns.IndexOf(this);
            var leftFrozenIndex = PresetManager.LastLeftFrozenIndex;
            var rightFrozenIndex = PresetManager.FirstRightFrozenIndex;

            IsFrozen =
                index >= 0 &&
                (leftFrozenIndex != -1 && index <= leftFrozenIndex ||
                 rightFrozenIndex != -1 && index >= rightFrozenIndex);
        }

        private void RefreshFromPreset()
        {
            refreshingFromPreset = true;
            try {
                Name = Preset.Name;
                AutomationProperties.SetName(this, Preset.Name);
                Id = Preset.Id;
                Width = Preset.Width;
                IsVisible = Preset.IsVisible;
                TextAlignment = Preset.TextAlignment;
                CellFormat = GetSupportedFormat(Preset.CellFormat);
            } finally {
                refreshingFromPreset = false;
            }
        }

        private void OnPresetPropertyChanged()
        {
            if (refreshingFromPreset)
                return;

            if (UpdateColumnPreset())
                PresetManager.SetDialogStateDirty(true);
        }

        private bool UpdateColumnPreset()
        {
            if (Preset == null)
                return false;

            bool updated = false;
            Update(Width, Preset.Width, x => Preset.Width = x, ref updated);
            Update(IsVisible, Preset.IsVisible, x => Preset.IsVisible = x, ref updated);
            Update(TextAlignment, Preset.TextAlignment, x => Preset.TextAlignment = x, ref updated);
            Update(CellFormat, GetSupportedFormat(Preset.CellFormat),
                x => Preset.CellFormat = x.Format, ref updated);

            return updated;
        }

        private SupportedFormat GetSupportedFormat(string format)
        {
            var supportedFormat = SupportedFormats.FirstOrDefault(x => x.Format == format);
            return supportedFormat.HasValue ? supportedFormat : defaultSupportedFormat;
        }

        private static void Update<T>(T newValue, T currentValue, Action<T> updateValue, ref bool updated)
        {
            if (!EqualityComparer<T>.Default.Equals(newValue, currentValue)) {
                updateValue(newValue);
                updated = true;
            }
        }
    }
}
