namespace EventTraceKit.VsExtension
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows;
    using Windows;
    using Controls;
    using Serialization;

    [SerializedShape(typeof(Settings.ViewPresets))]
    public class HdvViewModelPresetCollection : FreezableCustomSerializerAccessBase
    {
        private int deferChangeNotificationCount;

        private class PersistedPresetsSerializerCallback : IDeserializationCallback
        {
            public void OnDeserialized(object obj)
            {
                var persistedPresets = (FreezableCollection<AsyncDataViewModelPreset>)obj;
                foreach (var preset in persistedPresets)
                    preset.IsModified = true;
            }
        }

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

        public HdvViewModelPresetCollection()
        {
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

        public void SetUserPresets(IEnumerable<AsyncDataViewModelPreset> presets)
        {
            base.VerifyAccess();
            if (presets == null) {
                throw new ArgumentNullException("presets");
            }
            foreach (AsyncDataViewModelPreset preset in presets) {
                this.SetUserPreset(preset);
            }
        }

        public void SetPersistedPresets(IEnumerable<AsyncDataViewModelPreset> presets)
        {
            base.VerifyAccess();
            if (presets == null) {
                throw new ArgumentNullException("presets");
            }
            foreach (var preset in presets) {
                this.SetPersistedPreset(preset);
            }
        }

        [Serialize]
        [DeserializationCallback(typeof(PersistedPresetsSerializerCallback))]
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
            if (deferChangeNotificationCount == 0)
                RaiseAvailablePresetsChanged();
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
            return GetPresetIndexByName(name, PersistedPresets);
        }

        public bool DeletePersistedPresetByName(string name)
        {
            VerifyAccess();
            int persistedPresetIndexByName = GetPersistedPresetIndexByName(name);
            if (persistedPresetIndexByName == -1) {
                return false;
            }
            PersistedPresets.RemoveAt(persistedPresetIndexByName);
            return true;
        }

        private int GetUserPresetIndexByName(string name)
        {
            return GetPresetIndexByName(name, UserPresets);
        }

        public bool DeleteUserPresetByName(string name)
        {
            VerifyAccess();
            int userPresetIndexByName = GetUserPresetIndexByName(name);
            if (userPresetIndexByName == -1)
                return false;

            UserPresets.RemoveAt(userPresetIndexByName);
            return true;
        }

        public AsyncDataViewModelPreset TryGetBuiltInPresetByName(string name)
        {
            return TryGetPresetByName(name, BuiltInPresets);
        }

        public AsyncDataViewModelPreset TryGetUserPresetByName(string name)
        {
            return TryGetPresetByName(name, UserPresets);
        }

        public void SetUserPreset(AsyncDataViewModelPreset preset)
        {
            VerifyAccess();
            if (preset == null) {
                throw new ArgumentNullException(nameof(preset));
            }
            AsyncDataViewModelPreset other = TryGetUserPresetByName(preset.Name);
            if (other == null) {
                AddUserPreset(preset);
            } else if (!preset.Equals(other)) {
                RemoveUserPreset(other);
                AddUserPreset(preset);
            }
        }

        private void AddUserPreset(AsyncDataViewModelPreset preset)
        {
            VerifyAccess();
            UserPresets.Add(preset);
        }

        private void RemoveUserPreset(AsyncDataViewModelPreset preset)
        {
            VerifyAccess();
            UserPresets.Remove(preset);
        }

        public bool ContainsPreset(AsyncDataViewModelPreset preset)
        {
            if (preset == null)
                throw new ArgumentNullException(nameof(preset));

            var other = TryGetPresetByName(preset.Name);
            if (other == null)
                return false;

            return preset.Equals(other);
        }

        public AsyncDataViewModelPreset TryGetPersistedPresetByName(string name)
        {
            return TryGetPresetByName(name, PersistedPresets);
        }

        public void SetPersistedPreset(AsyncDataViewModelPreset preset)
        {
            VerifyAccess();
            if (preset == null)
                throw new ArgumentNullException(nameof(preset));

            var other = TryGetPersistedPresetByName(preset.Name);
            if (other == null) {
                AddPersistedPreset(preset);
            } else if (!preset.Equals(other)) {
                RemovePersistedPreset(other);
                AddPersistedPreset(preset);
            }
        }

        private void AddPersistedPreset(AsyncDataViewModelPreset preset)
        {
            VerifyAccess();
            PersistedPresets.Add(preset);
        }

        private void RemovePersistedPreset(AsyncDataViewModelPreset preset)
        {
            VerifyAccess();
            PersistedPresets.Remove(preset);
        }

        public void BeginDeferChangeNotifications()
        {
            ++deferChangeNotificationCount;
        }

        public void EndDeferChangeNotifications()
        {
            --deferChangeNotificationCount;
            RaiseChangeNotificationIfNecessary();
        }

        protected override Freezable CreateInstanceCore()
        {
            return new HdvViewModelPresetCollection();
        }
    }
}
