using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace HexWide
{
    public partial class SettingsWindow : Window
    {
        private ObservableCollection<ResolutionItem> _items = new();

        public SettingsWindow()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void LoadSettings()
        {
            var settings = AppSettingsLoader.Load();
            _items = new ObservableCollection<ResolutionItem>(settings.Resolutions.Select(kv => new ResolutionItem { Name = kv.Key, Hex = kv.Value }));
            resolutionsGrid.ItemsSource = _items;

            txtAspectFind.Text = settings.AspectFindHex ?? string.Empty;
            txtFovFind.Text = settings.FovFindHex ?? string.Empty;
            txtFovReplace.Text = settings.FovReplaceHex ?? string.Empty;
        }

        private void AddResolutionButton_Click(object sender, RoutedEventArgs e)
        {
            _items.Add(new ResolutionItem { Name = "new-resolution", Hex = "00 00 00 00" });
            resolutionsGrid.Items.Refresh();
            resolutionsGrid.SelectedIndex = _items.Count - 1;
        }

        private void RemoveResolutionButton_Click(object sender, RoutedEventArgs e)
        {
            if (resolutionsGrid.SelectedItem is ResolutionItem item)
            {
                _items.Remove(item);
                resolutionsGrid.Items.Refresh();
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var settings = AppSettingsLoader.Load();
            settings.Resolutions = _items.ToDictionary(i => i.Name, i => i.Hex);
            settings.AspectFindHex = txtAspectFind.Text ?? settings.AspectFindHex;
            settings.FovFindHex = txtFovFind.Text ?? settings.FovFindHex;
            settings.FovReplaceHex = txtFovReplace.Text ?? settings.FovReplaceHex;

            AppSettingsLoader.Save(settings);
            this.DialogResult = true;
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private class ResolutionItem
        {
            public string Name { get; set; } = string.Empty;
            public string Hex { get; set; } = string.Empty;
        }
    }
}
