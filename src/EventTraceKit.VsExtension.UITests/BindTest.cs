namespace EventTraceKit.VsExtension.UITests
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using EventTraceKit.VsExtension.Windows;
    using Microsoft.VisualStudio.Threading;
    using Xunit;

    public class BindTest
    {
        private static IEnumerable<string> NextItems(Random rng, int count)
        {
            return Enumerable.Range(0, count).Select(x => rng.Next().ToString()).ToList();
        }

        [StaFact]
        public void BindItemsSource()
        {
            var comboBox = new ComboBox();
            var rng = new Random();

            comboBox.ItemsSource = NextItems(rng, 5);

            DisplayElement(comboBox);
        }

        [StaFact]
        public void BindItemsSourceProvider()
        {
            var comboBox = new ComboBox();
            var rng = new Random();

            IEnumerable<string> Source()
            {
                return NextItems(rng, 5);
            }

            Bind.SetItemsSourceProvider(comboBox, Source);

            DisplayElement(comboBox);
        }

        [StaFact]
        public void BindItemsSourceAsync()
        {
            var comboBox = new ComboBox();
            var rng = new Random();

            async Task<IEnumerable> Source()
            {
                await Task.Delay(5000);
                return NextItems(rng, 5);
            }

            Bind.SetItemsSourceAsync(comboBox, Task.Run(Source));

            DisplayElement(comboBox);
        }

        [StaFact]
        public void BindItemsSourceProviderAsync()
        {
            var comboBox = new ComboBox();
            var rng = new Random();

            async Task<IEnumerable> Source()
            {
                await Task.Delay(2000);
                return NextItems(rng, 5);
            }

            Bind.SetItemsSourceProviderAsync(comboBox, () => Task.Run(Source));

            DisplayElement(comboBox);
        }

        [StaFact]
        public void BindItemsSourceLazy()
        {
            var comboBox = new ComboBox();
            var rng = new Random();

            async Task<IEnumerable> Source()
            {
                await Task.Delay(2000);
                return NextItems(rng, 5);
            }

            var lazySource = new AsyncLazy<IEnumerable>(() => Task.Run(Source));
            Bind.SetItemsSourceProviderAsync(comboBox, async () => await lazySource.GetValueAsync());

            DisplayElement(comboBox);
        }

        private static void DisplayElement(params UIElement[] elements)
        {
            UIElement content;
            if (elements.Length == 1) {
                content = elements[0];
            } else {
                var panel = new StackPanel();
                foreach (var element in elements)
                    panel.Children.Add(element);
                content = panel;
            }

            var window = new Window();
            window.Width = 300;
            window.Height = 200;
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            window.Content = new Grid {
                MinWidth = 200,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Children = { content }
            };
            window.ShowDialog();
        }
    }
}
