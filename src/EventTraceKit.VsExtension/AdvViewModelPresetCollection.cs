namespace EventTraceKit.VsExtension
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows;
    using Windows;
    using Collections;
    using Controls;
    using Extensions;
    using Serialization;

    [SerializedShape(typeof(Settings.ViewPresets))]
    public class AdvViewModelPresetCollection : FreezableCustomSerializerAccessBase
    {
        private int deferChangeNotificationCount;

        private static readonly DependencyPropertyKey UserPresetsPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(UserPresets),
                typeof(FreezableCollection<AsyncDataViewModelPreset>),
                typeof(AdvViewModelPresetCollection),
                new PropertyMetadata(
                    null,
                    (s, e) => ((AdvViewModelPresetCollection)s).UserPresetsPropertyChanged(e)));

        public static readonly DependencyProperty UserPresetsProperty =
            UserPresetsPropertyKey.DependencyProperty;

        private static readonly DependencyPropertyKey PersistedPresetsPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(PersistedPresets),
                typeof(FreezableCollection<AsyncDataViewModelPreset>),
                typeof(AdvViewModelPresetCollection),
                new PropertyMetadata(
                    null,
                    (s, e) => ((AdvViewModelPresetCollection)s).PersistedPresetsPropertyChanged(e)));

        public static readonly DependencyProperty PersistedPresetsProperty =
            PersistedPresetsPropertyKey.DependencyProperty;

        private static readonly DependencyPropertyKey BuiltInPresetsPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(BuiltInPresets),
                typeof(FreezableCollection<AsyncDataViewModelPreset>),
                typeof(AdvViewModelPresetCollection),
                new PropertyMetadata(
                    null,
                    (s, e) => ((AdvViewModelPresetCollection)s).BuiltInPresetsPropertyChanged(e)));

        public static readonly DependencyProperty BuiltInPresetsProperty =
            BuiltInPresetsPropertyKey.DependencyProperty;

        public AdvViewModelPresetCollection()
        {
            BuiltInPresets = new FreezableCollection<AsyncDataViewModelPreset>();
            UserPresets = new FreezableCollection<AsyncDataViewModelPreset>();
            PersistedPresets = new FreezableCollection<AsyncDataViewModelPreset>();
        }

        public event EventHandler AvailablePresetsChanged;

        public int Count => UserPresets.Count + BuiltInPresets.Count;

        public FreezableCollection<AsyncDataViewModelPreset> BuiltInPresets
        {
            get { return (FreezableCollection<AsyncDataViewModelPreset>)GetValue(BuiltInPresetsProperty); }
            private set { SetValue(BuiltInPresetsPropertyKey, value); }
        }

        [Serialize]
        [DeserializationCallback(typeof(PersistedPresetsSerializerCallback))]
        public FreezableCollection<AsyncDataViewModelPreset> PersistedPresets
        {
            get { return (FreezableCollection<AsyncDataViewModelPreset>)GetValue(PersistedPresetsProperty); }
            private set { SetValue(PersistedPresetsPropertyKey, value); }
        }

        [Serialize]
        public FreezableCollection<AsyncDataViewModelPreset> UserPresets
        {
            get { return (FreezableCollection<AsyncDataViewModelPreset>)GetValue(UserPresetsProperty); }
            private set { SetValue(UserPresetsPropertyKey, value); }
        }

        protected override Freezable CreateInstanceCore()
        {
            return new AdvViewModelPresetCollection();
        }

        protected override void CloneCore(Freezable sourceFreezable)
        {
            base.CloneCore(sourceFreezable);
            var source = (AdvViewModelPresetCollection)sourceFreezable;
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

        public AsyncDataViewModelPreset TryGetPresetByName(string name)
        {
            return TryGetPresetByName(name, EnumerateAllPresets());
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

            var other = TryGetPresetByName(preset.Name);
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
            return TryGetPresetByName(name, BuiltInPresets);
        }

        public AsyncDataViewModelPreset TryGetUserPresetByName(string name)
        {
            return TryGetPresetByName(name, UserPresets);
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
            return TryGetPresetByName(name, PersistedPresets);
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
                TryGetPresetByName(presetName);
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

        private static AsyncDataViewModelPreset TryGetPresetByName(
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
            var oldValue = e.OldValue as FreezableCollection<AsyncDataViewModelPreset>;
            if (oldValue != null && !oldValue.IsFrozen)
                oldValue.Changed -= OnBuiltInPresetsChanged;

            var newValue = e.NewValue as FreezableCollection<AsyncDataViewModelPreset>;
            if (newValue != null && !newValue.IsFrozen)
                newValue.Changed += OnBuiltInPresetsChanged;

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
            private readonly AdvViewModelPresetCollection collection;

            public DeferChangeNotificationsScope(AdvViewModelPresetCollection collection)
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
                var persistedPresets = (FreezableCollection<AsyncDataViewModelPreset>)obj;
                foreach (var preset in persistedPresets)
                    preset.IsModified = true;
            }
        }
    }

    public static class AdvViewModelPresetCollectionExtensions
    {
        public static void MergeInBuiltInPresets(
            this AdvViewModelPresetCollection target, AdvViewModelPresetCollection source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            using (target.DeferChangeNotifications())
                target.BuiltInPresets.AddRange(source.BuiltInPresets);
        }

        public static void MergeInUserPresets(
            this AdvViewModelPresetCollection target, AdvViewModelPresetCollection source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            using (target.DeferChangeNotifications())
                target.SetUserPresets(source.UserPresets);
        }

        public static void MergeInPersistedPresets(
            this AdvViewModelPresetCollection target, AdvViewModelPresetCollection source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            using (target.DeferChangeNotifications())
                target.SetPersistedPresets(source.PersistedPresets);
        }

        public static void MergeInUserPreset(
            this AdvViewModelPresetCollection target, AsyncDataViewModelPreset preset)
        {
            if (preset == null)
                throw new ArgumentNullException(nameof(preset));

            using (target.DeferChangeNotifications())
                target.SetUserPreset(preset);
        }

        public static void MergeInPersistedPreset(
            this AdvViewModelPresetCollection target, AsyncDataViewModelPreset preset)
        {
            if (preset == null)
                throw new ArgumentNullException(nameof(preset));

            using (target.DeferChangeNotifications())
                target.SetPersistedPreset(preset);
        }
    }
}
