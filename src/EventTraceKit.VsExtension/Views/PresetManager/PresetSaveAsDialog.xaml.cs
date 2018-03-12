namespace EventTraceKit.VsExtension.Views.PresetManager
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;
    using EventTraceKit.VsExtension.Windows;

    public partial class PresetSaveAsDialog
    {
        private readonly IEnumerable<string> builtInPresetNames;
        private AsyncDelegateCommand saveCommand;

        public static readonly DependencyProperty CanExecuteProperty =
            DependencyProperty.Register(
                nameof(CanExecute),
                typeof(bool),
                typeof(PresetSaveAsDialog),
                new PropertyMetadata(
                    Boxed.False,
                    (d, e) => ((PresetSaveAsDialog)d).OnCanExecuteChanged(e),
                    (d, v) => ((PresetSaveAsDialog)d).CoerceCanExecuteProperty(v)));

        public static readonly DependencyProperty NewPresetNameProperty =
            DependencyProperty.Register(
                nameof(NewPresetName),
                typeof(string),
                typeof(PresetSaveAsDialog),
                new PropertyMetadata(
                    string.Empty,
                    (d, e) => ((PresetSaveAsDialog)d).OnNewPresetNameChanged(e)));

        private object CoerceCanExecuteProperty(object baseValue)
        {
            var trimmedName = NewPresetName.Trim();
            if (string.IsNullOrEmpty(trimmedName) || trimmedName.Contains('*'))
                return false;

            return !builtInPresetNames.Any(
                x => x.Trim().Equals(trimmedName, StringComparison.OrdinalIgnoreCase));
        }

        private PresetSaveAsDialog(AdvmPresetCollection presetCollection)
        {
            if (presetCollection == null)
                throw new ArgumentNullException(nameof(presetCollection));

            InitializeComponent();
            DataContext = this;
            this.builtInPresetNames = from p in presetCollection.BuiltInPresets select p.Name;
        }

        public static PresetSaveAsDialog ShowPresetSaveAsDialog(
            AdvmPresetCollection presetCollection)
        {
            var dialog = new PresetSaveAsDialog(presetCollection);
            dialog.ShowDialog();
            return dialog;
        }

        public bool CanExecute => (bool)GetValue(CanExecuteProperty);

        public string NewPresetName
        {
            get => (string)GetValue(NewPresetNameProperty);
            set => SetValue(NewPresetNameProperty, value);
        }

        public AsyncDelegateCommand SaveCommand
        {
            get
            {
                if (saveCommand == null)
                    saveCommand = new AsyncDelegateCommand(SaveAndApplyNewPreset, () => CanExecute);
                return saveCommand;
            }
        }

        private Task SaveAndApplyNewPreset()
        {
            DialogResult = true;
            Close();
            return Task.CompletedTask;
        }

        private void OnCanExecuteChanged(DependencyPropertyChangedEventArgs e)
        {
            SaveCommand.RaiseCanExecuteChanged();
        }

        private void OnNewPresetNameChanged(DependencyPropertyChangedEventArgs e)
        {
            CoerceValue(CanExecuteProperty);
        }
    }
}
