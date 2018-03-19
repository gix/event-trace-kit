namespace EventTraceKit.VsExtension
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows;
    using Controls;
    using Extensions;
    using Serialization;
    using Windows;

    [SerializedShape(typeof(Settings.Persistence.ViewPresets))]
    [DeserializationCallback(typeof(PersistedPresetsSerializerCallback))]
    public class AdvmPresetCollection : FreezableCustomSerializerAccessBase
    {
        private int deferChangeNotificationCount;

        private static readonly DependencyPropertyKey UserPresetsPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(UserPresets),
                typeof(FreezableCollection<AsyncDataViewModelPreset>),
                typeof(AdvmPresetCollection),
                new PropertyMetadata(
                    null,
                    (s, e) => ((AdvmPresetCollection)s).UserPresetsPropertyChanged(e)));

        public static readonly DependencyProperty UserPresetsProperty =
            UserPresetsPropertyKey.DependencyProperty;

        private static readonly DependencyPropertyKey PersistedPresetsPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(PersistedPresets),
                typeof(FreezableCollection<AsyncDataViewModelPreset>),
                typeof(AdvmPresetCollection),
                new PropertyMetadata(
                    null,
                    (s, e) => ((AdvmPresetCollection)s).PersistedPresetsPropertyChanged(e)));

        public static readonly DependencyProperty PersistedPresetsProperty =
            PersistedPresetsPropertyKey.DependencyProperty;

        private static readonly DependencyPropertyKey BuiltInPresetsPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(BuiltInPresets),
                typeof(FreezableCollection<AsyncDataViewModelPreset>),
                typeof(AdvmPresetCollection),
                new PropertyMetadata(
                    null,
                    (s, e) => ((AdvmPresetCollection)s).BuiltInPresetsPropertyChanged(e)));

        public static readonly DependencyProperty BuiltInPresetsProperty =
            BuiltInPresetsPropertyKey.DependencyProperty;

        public AdvmPresetCollection()
        {
            BuiltInPresets = new FreezableCollection<AsyncDataViewModelPreset>();
            UserPresets = new FreezableCollection<AsyncDataViewModelPreset>();
            PersistedPresets = new FreezableCollection<AsyncDataViewModelPreset>();
        }

        public event EventHandler AvailablePresetsChanged;

        public int Count => UserPresets.Count + BuiltInPresets.Count;

        public FreezableCollection<AsyncDataViewModelPreset> BuiltInPresets
        {
            get => (FreezableCollection<AsyncDataViewModelPreset>)GetValue(BuiltInPresetsProperty);
            private set => SetValue(BuiltInPresetsPropertyKey, value);
        }

        [Serialize]
        public FreezableCollection<AsyncDataViewModelPreset> PersistedPresets
        {
            get => (FreezableCollection<AsyncDataViewModelPreset>)GetValue(PersistedPresetsProperty);
            private set => SetValue(PersistedPresetsPropertyKey, value);
        }

        [Serialize]
        public FreezableCollection<AsyncDataViewModelPreset> UserPresets
        {
            get => (FreezableCollection<AsyncDataViewModelPreset>)GetValue(UserPresetsProperty);
            private set => SetValue(UserPresetsPropertyKey, value);
        }

        protected override Freezable CreateInstanceCore()
        {
            return new AdvmPresetCollection();
        }

        protected override void CloneCore(Freezable sourceFreezable)
        {
            base.CloneCore(sourceFreezable);
            var source = (AdvmPresetCollection)sourceFreezable;
            using (DeferChangeNotifications()) {
                BuiltInPresets.AddRange(source.BuiltInPresets);
                UserPresets.AddRange(source.UserPresets);
                PersistedPresets.AddRange(source.PersistedPresets);
            }
        }

        public IEnumerable<AsyncDataViewModelPreset> EnumerateAllPresets()
        {
            return UserPresets.Concat(BuiltInPresets);
        }

        public IEnumerable<AsyncDataViewModelPreset> EnumerateAllPresetsByName()
        {
            return from preset in EnumerateAllPresets()
                   orderby preset.Name
                   select preset;
        }

        public bool IsBuiltInPreset(string name)
        {
            return BuiltInPresets.Any(p => p.Name == name);
        }

        public bool HasPersistedPreset(string name)
        {
            return TryGetPersistedPresetByName(name) != null;
        }

        public void SetUserPresets(IEnumerable<AsyncDataViewModelPreset> presets)
        {
            VerifyAccess();
            if (presets == null)
                throw new ArgumentNullException(nameof(presets));

            foreach (var preset in presets)
                SetUserPreset(preset);
        }

        public void SetPersistedPresets(IEnumerable<AsyncDataViewModelPreset> presets)
        {
            VerifyAccess();
            if (presets == null)
                throw new ArgumentNullException(nameof(presets));

            foreach (var preset in presets)
                SetPersistedPreset(preset);
        }

        public AsyncDataViewModelPreset TryGetUnmodifiedPresetByName(string name)
        {
            return TryGetUnmodifiedPresetByName(name, EnumerateAllPresets());
        }

        public bool DeletePersistedPresetByName(string name)
        {
            VerifyAccess();

            int index = GetPersistedPresetIndexByName(name);
            if (index == -1)
                return false;

            PersistedPresets.RemoveAt(index);
            return true;
        }

        public bool ContainsPreset(AsyncDataViewModelPreset preset)
        {
            if (preset == null)
                throw new ArgumentNullException(nameof(preset));

            var other = TryGetUnmodifiedPresetByName(preset.Name);
            if (other == null)
                return false;

            return preset.Equals(other);
        }

        public bool DeleteUserPresetByName(string name)
        {
            VerifyAccess();

            int index = GetUserPresetIndexByName(name);
            if (index == -1)
                return false;

            UserPresets.RemoveAt(index);
            return true;
        }

        public AsyncDataViewModelPreset TryGetBuiltInPresetByName(string name)
        {
            return TryGetUnmodifiedPresetByName(name, BuiltInPresets);
        }

        public AsyncDataViewModelPreset TryGetUserPresetByName(string name)
        {
            return TryGetUnmodifiedPresetByName(name, UserPresets);
        }

        public void SetUserPreset(AsyncDataViewModelPreset preset)
        {
            if (preset == null)
                throw new ArgumentNullException(nameof(preset));
            VerifyAccess();

            var existing = TryGetUserPresetByName(preset.Name);
            if (existing == null) {
                UserPresets.Add(preset);
            } else if (!preset.Equals(existing)) {
                UserPresets.Remove(existing);
                UserPresets.Add(preset);
            }
        }

        public AsyncDataViewModelPreset TryGetPersistedPresetByName(string name)
        {
            return TryGetUnmodifiedPresetByName(name, PersistedPresets);
        }

        public void SetPersistedPreset(AsyncDataViewModelPreset preset)
        {
            VerifyAccess();
            if (preset == null)
                throw new ArgumentNullException(nameof(preset));

            var other = TryGetPersistedPresetByName(preset.Name);
            if (other == null) {
                PersistedPresets.Add(preset);
            } else if (!preset.Equals(other)) {
                PersistedPresets.Remove(other);
                PersistedPresets.Add(preset);
            }
        }

        public AsyncDataViewModelPreset TryGetCurrentPresetByName(string presetName)
        {
            return
                TryGetPersistedPresetByName(presetName) ??
                TryGetUnmodifiedPresetByName(presetName);
        }

        public void SavePreset(
            AsyncDataViewModelPreset preset, bool isPersistedPreset,
            string originalPresetName)
        {
            if (preset == null)
                throw new ArgumentNullException(nameof(preset));

            SetUserPreset(preset);
            if (isPersistedPreset)
                DeletePersistedPresetByName(originalPresetName);
        }

        public void CachePreset(AsyncDataViewModelPreset preset)
        {
            if (preset == null)
                throw new ArgumentNullException(nameof(preset));

            if (!preset.IsModified)
                return;

            var unmodifiedPreset = TryGetPersistedPresetByName(preset.Name);
            SetPersistedPreset(preset);

            bool hasBuiltIn = TryGetBuiltInPresetByName(preset.Name) != null;
            bool hasUser = TryGetUserPresetByName(preset.Name) != null;
            if (!hasBuiltIn && !hasUser)
                SetUserPreset(unmodifiedPreset ?? preset);
        }

        public IDisposable DeferChangeNotifications()
        {
            return new DeferChangeNotificationsScope(this);
        }

        private static AsyncDataViewModelPreset TryGetUnmodifiedPresetByName(
            string name, IEnumerable<AsyncDataViewModelPreset> presets)
        {
            return presets.FirstOrDefault(p => p.Name == name);
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
            if (e.OldValue is FreezableCollection<AsyncDataViewModelPreset> oldValue && !oldValue.IsFrozen)
                oldValue.Changed -= OnBuiltInPresetsChanged;

            if (e.NewValue is FreezableCollection<AsyncDataViewModelPreset> newValue && !newValue.IsFrozen)
                newValue.Changed += OnBuiltInPresetsChanged;

            RaiseChangeNotificationIfNecessary();
        }

        private void UserPresetsPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is FreezableCollection<AsyncDataViewModelPreset> oldValue && !oldValue.IsFrozen)
                oldValue.Changed -= OnUserPresetsChanged;

            if (e.NewValue is FreezableCollection<AsyncDataViewModelPreset> newValue && !newValue.IsFrozen)
                newValue.Changed += OnUserPresetsChanged;

            RaiseChangeNotificationIfNecessary();
        }

        private void PersistedPresetsPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is FreezableCollection<AsyncDataViewModelPreset> oldValue && !oldValue.IsFrozen)
                oldValue.Changed -= OnPersistedPresetsChanged;

            if (e.NewValue is FreezableCollection<AsyncDataViewModelPreset> newValue && !newValue.IsFrozen)
                newValue.Changed += OnPersistedPresetsChanged;

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

        private int GetUserPresetIndexByName(string name)
        {
            return GetPresetIndexByName(name, UserPresets);
        }

        private int GetPersistedPresetIndexByName(string name)
        {
            return GetPresetIndexByName(name, PersistedPresets);
        }

        private static int GetPresetIndexByName(
            string name, IEnumerable<AsyncDataViewModelPreset> presets)
        {
            return presets.IndexOf(name, (x, n) => x.Name.Equals(n));
        }

        private void BeginDeferChangeNotifications()
        {
            ++deferChangeNotificationCount;
        }

        private void EndDeferChangeNotifications()
        {
            --deferChangeNotificationCount;
            RaiseChangeNotificationIfNecessary();
        }

        private sealed class DeferChangeNotificationsScope : IDisposable
        {
            private readonly AdvmPresetCollection collection;

            public DeferChangeNotificationsScope(AdvmPresetCollection collection)
            {
                this.collection = collection;
                this.collection.BeginDeferChangeNotifications();
            }

            public void Dispose()
            {
                collection.EndDeferChangeNotifications();
            }
        }

        private class PersistedPresetsSerializerCallback : IDeserializationCallback
        {
            public void OnDeserialized(object obj)
            {
                var collection = (AdvmPresetCollection)obj;
                foreach (var preset in collection.PersistedPresets)
                    preset.IsModified = true;
            }
        }
    }

    public static class AdvViewModelPresetCollectionExtensions
    {
        public static void MergeInBuiltInPresets(
            this AdvmPresetCollection target, AdvmPresetCollection source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            using (target.DeferChangeNotifications())
                target.BuiltInPresets.AddRange(source.BuiltInPresets);
        }

        public static void MergeInUserPresets(
            this AdvmPresetCollection target, AdvmPresetCollection source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            using (target.DeferChangeNotifications())
                target.SetUserPresets(source.UserPresets);
        }

        public static void MergeInPersistedPresets(
            this AdvmPresetCollection target, AdvmPresetCollection source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            using (target.DeferChangeNotifications())
                target.SetPersistedPresets(source.PersistedPresets);
        }

        public static void MergeInUserPreset(
            this AdvmPresetCollection target, AsyncDataViewModelPreset preset)
        {
            if (preset == null)
                throw new ArgumentNullException(nameof(preset));

            using (target.DeferChangeNotifications())
                target.SetUserPreset(preset);
        }

        public static void MergeInPersistedPreset(
            this AdvmPresetCollection target, AsyncDataViewModelPreset preset)
        {
            if (preset == null)
                throw new ArgumentNullException(nameof(preset));

            using (target.DeferChangeNotifications())
                target.SetPersistedPreset(preset);
        }
    }
}
