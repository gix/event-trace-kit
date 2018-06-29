namespace EventTraceKit.VsExtension.Views
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;
    using EventManifestFramework.Schema;
    using Task = System.Threading.Tasks.Task;

    public partial class KeywordSelector
    {
        private IReadOnlyList<Keyword> loadedKeywords;

        public KeywordSelector()
        {
            InitializeComponent();
            IsVisibleChanged += OnIsVisibleChanged;
        }

        public static readonly DependencyProperty KeywordsSourceProperty =
            DependencyProperty.Register(
                nameof(KeywordsSource),
                typeof(Func<Task<IReadOnlyList<Keyword>>>),
                typeof(KeywordSelector),
                new FrameworkPropertyMetadata(
                    null,
                    (d, e) => ((KeywordSelector)d).OnKeywordsChanged()));

        public Func<Task<IReadOnlyList<Keyword>>> KeywordsSource
        {
            get => (Func<Task<IReadOnlyList<Keyword>>>)GetValue(KeywordsSourceProperty);
            set => SetValue(KeywordsSourceProperty, value);
        }

        public static readonly DependencyProperty MaskProperty =
            DependencyProperty.Register(
                nameof(Mask),
                typeof(ulong),
                typeof(KeywordSelector),
                new FrameworkPropertyMetadata(
                    0UL,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    (d, e) => ((KeywordSelector)d).OnMaskChanged((ulong)e.OldValue, (ulong)e.NewValue)));

        public ulong Mask
        {
            get => (ulong)GetValue(MaskProperty);
            set => SetValue(MaskProperty, value);
        }

        private async void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            await Refresh();
        }

        private void SetStatus(string text)
        {
            StatusText.Text = text;
            StatusText.Visibility = string.IsNullOrEmpty(text) ?
                Visibility.Collapsed : Visibility.Visible;
        }

        private async void OnKeywordsChanged()
        {
            await Refresh();
        }

        private async Task Refresh()
        {
            if (!IsVisible || KeywordsSource == null)
                return;

            IReadOnlyList<Keyword> keywords;
            try {
                SetStatus("Loading…");
                keywords = await KeywordsSource();
                SetStatus(null);
            } catch (OperationCanceledException) {
                SetStatus(null);
                return;
            } catch (Exception ex) {
                SetStatus(ex.Message);
                return;
            }

            if (ReferenceEquals(loadedKeywords, keywords))
                return;

            loadedKeywords = keywords;
            ItemList.Items.Clear();

            if (keywords.Count != 0) {
                var allBits = keywords.Aggregate(0UL, (a, k) => a | k.Mask);
                ItemList.Items.Add(new KeywordItem(allBits, "(All)", HasBits, SetBits));
                foreach (var keyword in keywords)
                    ItemList.Items.Add(new KeywordItem(keyword, HasBits, SetBits));
            } else {
                SetStatus("No known keywords");
            }
        }

        private bool HasBits(ulong bits)
        {
            return (Mask & bits) == bits;
        }

        private void SetBits(ulong bits, bool set)
        {
            if (set)
                Mask |= bits;
            else
                Mask &= ~bits;
        }

        private void OnMaskChanged(ulong oldValue, ulong newValue)
        {
            foreach (KeywordItem item in ItemList.Items)
                item.RaiseIsEnabledChanged();
        }
    }

    public sealed class KeywordItem : ObservableModel
    {
        private readonly Func<ulong, bool> hasMask;
        private readonly Action<ulong, bool> setMask;

        public KeywordItem(Keyword keyword, Func<ulong, bool> hasMask, Action<ulong, bool> setMask)
        {
            this.hasMask = hasMask;
            this.setMask = setMask;
            Mask = keyword.Mask;
            Name = keyword.Name.Value.ToPrefixedString();
        }

        public KeywordItem(ulong mask, string name, Func<ulong, bool> hasMask, Action<ulong, bool> setMask)
        {
            this.hasMask = hasMask;
            this.setMask = setMask;
            Mask = mask;
            Name = name;
        }

        public ulong Mask { get; }
        public string Name { get; }

        public bool IsEnabled
        {
            get => hasMask(Mask);
            set => setMask(Mask, value);
        }

        public void RaiseIsEnabledChanged()
        {
            RaisePropertyChanged(nameof(IsEnabled));
        }
    }
}
