namespace EventTraceKit.VsExtension
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows;
    using Controls;
    using Serialization;

    public class HdvViewModelPresetCollection : DependencyObject
    {
        private int deferChangeNotificationCount;

        private static readonly DependencyPropertyKey UserPresetsPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(UserPresets),
                typeof(FreezableCollection<AsyncDataViewModelPreset>),
                typeof(HdvViewModelPresetCollection),
                new PropertyMetadata(
                    null,
                    (s, e) => ((HdvViewModelPresetCollection)s).UserPresetsPropertyChanged(e)));

        public static readonly DependencyProperty UserPresetsProperty =
            UserPresetsPropertyKey.DependencyProperty;

        private static readonly DependencyPropertyKey PersistedPresetsPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(PersistedPresets),
                typeof(FreezableCollection<AsyncDataViewModelPreset>),
                typeof(HdvViewModelPresetCollection),
                new PropertyMetadata(
                    null,
                    (s, e) => ((HdvViewModelPresetCollection)s).PersistedPresetsPropertyChanged(e)));

        public static readonly DependencyProperty PersistedPresetsProperty =
            PersistedPresetsPropertyKey.DependencyProperty;

        private static readonly DependencyPropertyKey BuiltInPresetsPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(BuiltInPresets),
                typeof(FreezableCollection<AsyncDataViewModelPreset>),
                typeof(HdvViewModelPresetCollection),
                new PropertyMetadata(
                    null,
                    (s, e) => ((HdvViewModelPresetCollection)s).BuiltInPresetsPropertyChanged(e)));

        public static readonly DependencyProperty BuiltInPresetsProperty =
            BuiltInPresetsPropertyKey.DependencyProperty;

        public event EventHandler AvailablePresetsChanged;

        public HdvViewModelPresetCollection() : this(null)
        {
        }

        public HdvViewModelPresetCollection(string name)
        {
            Name = name;
            BuiltInPresets = new FreezableCollection<AsyncDataViewModelPreset>();
            UserPresets = new FreezableCollection<AsyncDataViewModelPreset>();
            PersistedPresets = new FreezableCollection<AsyncDataViewModelPreset>();
        }

        public FreezableCollection<AsyncDataViewModelPreset> BuiltInPresets
        {
            get { return (FreezableCollection<AsyncDataViewModelPreset>)GetValue(BuiltInPresetsProperty); }
            private set { SetValue(BuiltInPresetsPropertyKey, value); }
        }

        public bool IsBuiltInPreset(string name)
        {
            return BuiltInPresets.Any(p => p.Name == name);
        }

        [Serialize]
        //[CustomDeserializer(typeof(HdvVMPCPersistedPresetsDeserializer))]
        public FreezableCollection<AsyncDataViewModelPreset> PersistedPresets
        {
            get { return (FreezableCollection<AsyncDataViewModelPreset>)GetValue(PersistedPresetsProperty); }
            private set { SetValue(PersistedPresetsPropertyKey, value); }
        }

        [Serialize("ModifiedPresets")]
        public FreezableCollection<AsyncDataViewModelPreset> UserPresets
        {
            get { return (FreezableCollection<AsyncDataViewModelPreset>)GetValue(UserPresetsProperty); }
            private set { SetValue(UserPresetsPropertyKey, value); }
        }

        public string Name { get; set; }

        public int Count => UserPresets.Count + BuiltInPresets.Count;

        public IEnumerable<AsyncDataViewModelPreset> EnumerateAllPresets()
        {
            return UserPresets.Concat(BuiltInPresets);
        }

        public IEnumerable<string> EnumerateAllPresetsByName()
        {
            return from preset in EnumerateAllPresets()
                   orderby preset.Name
                   select preset.Name;
        }

        public AsyncDataViewModelPreset TryGetPresetByName(string name)
        {
            return TryGetPresetByName(name, EnumerateAllPresets());
        }

        private static AsyncDataViewModelPreset TryGetPresetByName(
            string name, IEnumerable<AsyncDataViewModelPreset> presets)
        {
            return presets.FirstOrDefault(p => p.Name.Equals(name));
        }

        private void OnBuiltInPresetsChanged(object sender, EventArgs e)
        {
            RaiseChangeNotificationIfNecessary();
        }

        private void OnPersistedPresetsChanged(object sender, EventArgs e)
        {
            RaiseChangeNotificationIfNecessary();
        }

        private void OnUserPresetsChanged(object sender, EventArgs e)
        {
            RaiseChangeNotificationIfNecessary();
        }

        private void BuiltInPresetsPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            var oldValue = e.OldValue as FreezableCollection<AsyncDataViewModelPreset>;
            if (oldValue != null && !oldValue.IsFrozen)
                oldValue.Changed -= OnBuiltInPresetsChanged;

            var newValue = e.NewValue as FreezableCollection<AsyncDataViewModelPreset>;
            if (newValue != null && !newValue.IsFrozen)
                newValue.Changed += OnBuiltInPresetsChanged;

            RaiseChangeNotificationIfNecessary();
        }

        private void PersistedPresetsPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            var oldValue = e.OldValue as FreezableCollection<AsyncDataViewModelPreset>;
            if (oldValue != null && !oldValue.IsFrozen)
                oldValue.Changed -= OnPersistedPresetsChanged;

            var newValue = e.NewValue as FreezableCollection<AsyncDataViewModelPreset>;
            if (newValue != null && !newValue.IsFrozen)
                newValue.Changed += OnPersistedPresetsChanged;

            RaiseChangeNotificationIfNecessary();
        }

        private void UserPresetsPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            var oldValue = e.OldValue as FreezableCollection<AsyncDataViewModelPreset>;
            if (oldValue != null && !oldValue.IsFrozen)
                oldValue.Changed -= OnUserPresetsChanged;

            var newValue = e.NewValue as FreezableCollection<AsyncDataViewModelPreset>;
            if (newValue != null && !newValue.IsFrozen)
                newValue.Changed += OnUserPresetsChanged;

            RaiseChangeNotificationIfNecessary();
        }

        private void RaiseChangeNotificationIfNecessary()
        {
            if (deferChangeNotificationCount == 0) {
                RaiseAvailablePresetsChanged();
            }
        }

        private void RaiseAvailablePresetsChanged()
        {
            AvailablePresetsChanged?.Invoke(this, EventArgs.Empty);
        }

        private static int GetPresetIndexByName(string name, IEnumerable<AsyncDataViewModelPreset> presets)
        {
            return presets.IndexOf(x => x.Name.Equals(name));
        }

        private int GetPersistedPresetIndexByName(string name)
        {
            return GetPresetIndexByName(name, this.PersistedPresets);
        }

        public bool DeletePersistedPresetByName(string name)
        {
            base.VerifyAccess();
            int persistedPresetIndexByName = this.GetPersistedPresetIndexByName(name);
            if (persistedPresetIndexByName == -1) {
                return false;
            }
            this.PersistedPresets.RemoveAt(persistedPresetIndexByName);
            return true;
        }

        private int GetUserPresetIndexByName(string name)
        {
            return GetPresetIndexByName(name, this.UserPresets);
        }

        public bool DeleteUserPresetByName(string name)
        {
            base.VerifyAccess();
            int userPresetIndexByName = this.GetUserPresetIndexByName(name);
            if (userPresetIndexByName == -1) {
                return false;
            }
            this.UserPresets.RemoveAt(userPresetIndexByName);
            return true;
        }

        public AsyncDataViewModelPreset TryGetUserPresetByName(string name)
        {
            return TryGetPresetByName(name, this.UserPresets);
        }

        public void SetUserPreset(AsyncDataViewModelPreset preset)
        {
            base.VerifyAccess();
            if (preset == null) {
                throw new ArgumentNullException(nameof(preset));
            }
            AsyncDataViewModelPreset other = this.TryGetUserPresetByName(preset.Name);
            if (other == null) {
                this.AddUserPreset(preset);
            } else if (!preset.Equals(other)) {
                this.RemoveUserPreset(other);
                this.AddUserPreset(preset);
            }
        }

        private void AddUserPreset(AsyncDataViewModelPreset preset)
        {
            base.VerifyAccess();
            this.UserPresets.Add(preset);
        }

        private void RemoveUserPreset(AsyncDataViewModelPreset preset)
        {
            base.VerifyAccess();
            this.UserPresets.Remove(preset);
        }

        public bool ContainsPreset(AsyncDataViewModelPreset preset)
        {
            if (preset == null) {
                throw new ArgumentNullException("preset");
            }
            var other = this.TryGetPresetByName(preset.Name);
            if (other == null) {
                return false;
            }
            return preset.Equals(other);
        }

        public AsyncDataViewModelPreset TryGetPersistedPresetByName(string name)
        {
            return TryGetPresetByName(name, this.PersistedPresets);
        }
    }
}
