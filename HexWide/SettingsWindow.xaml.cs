using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace HexWide
{
    public partial class SettingsWindow : Window
    {
        private ObservableCollection<ResolutionItem> _items = new();
        private class AspectFindOption
        {
            public string Name { get; }
            public string Hex { get; }

            public AspectFindOption(string name, string hex)
            {
                Name = name;
                Hex = hex;
            }
        }

        private readonly List<AspectFindOption> _aspectFindOptions = new()
        {
            new("Standard 16:9 exact", "39 8E E3 3F"),
            new("Standard 16:9 +1 ULP", "3A 8E E3 3F"),
            new("Life is Strange TC 16:9", "3B 8E E3 3F"),
            new("Standard 16:9 -1 ULP", "38 8E E3 3F"),
            new("Rare Code 0", "AB AA E2 3F"),
            new("Rare Code 1", "00 00 E0 3F")
        };

        public SettingsWindow()
        {
            InitializeComponent();
            LoadSettings();
        }
        private void AspectFindComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (aspectFindComboBox.SelectedItem is AspectFindOption option)
            {
                txtAspectFind.Text = option.Hex;
            }
        }
        private void LoadSettings()
        {
            var settings = AppSettingsLoader.Load();
            _items = new ObservableCollection<ResolutionItem>(settings.Resolutions.Select(kv => new ResolutionItem { Name = kv.Key, Hex = kv.Value }));
            resolutionsGrid.ItemsSource = _items;
            
            aspectFindComboBox.ItemsSource = _aspectFindOptions;
            aspectFindComboBox.DisplayMemberPath = "Name";
            aspectFindComboBox.SelectedValuePath = "Hex";

            // Populate the resolution dropdown
            resolutionComboBox.ItemsSource = _items.Select(r => r.Name).ToList();

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

        private void CalculateCustomResolution_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!int.TryParse(txtCustomX.Text, out int x) || !int.TryParse(txtCustomY.Text, out int y))
                {
                    calculatedHexLabel.Text = "Invalid input. Enter numbers for width and height.";
                    calculatedHexLabel.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
                    return;
                }

                if (x <= 0 || y <= 0)
                {
                    calculatedHexLabel.Text = "Width and height must be positive values.";
                    calculatedHexLabel.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
                    return;
                }

                string hexValue = HexConversionUtility.ResolutionToHex(x, y);
                string resolutionName = $"{x}x{y}";

                calculatedHexLabel.Text = $"✓ Calculated hex: {hexValue}";
                calculatedHexLabel.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Green);

                // Check if resolution already exists
                var existingItem = _items.FirstOrDefault(r => r.Name == resolutionName);
                if (existingItem != null)
                {
                    MessageBox.Show(
                        $"The resolution '{resolutionName}' already exists.\n\nIts hex value has been updated.",
                        "Resolution Already Exists",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    existingItem.Hex = hexValue;
                    resolutionsGrid.Items.Refresh();
                }
                else
                {
                    _items.Add(new ResolutionItem { Name = resolutionName, Hex = hexValue });
                    resolutionsGrid.Items.Refresh();
                }

                // Update the dropdown
                resolutionComboBox.ItemsSource = _items.Select(r => r.Name).ToList();
                resolutionComboBox.SelectedItem = resolutionName;

                // Clear input fields
                txtCustomX.Clear();
                txtCustomY.Clear();
            }
            catch (Exception ex)
            {
                calculatedHexLabel.Text = $"Error: {ex.Message}";
                calculatedHexLabel.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
            }
        }

        private void ResolutionComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (resolutionComboBox.SelectedItem is string selectedName)
            {
                var selectedItem = _items.FirstOrDefault(r => r.Name == selectedName);
                if (selectedItem != null)
                {
                    selectedResolutionLabel.Text = $"Selected: {selectedItem.Name} → {selectedItem.Hex}";
                    selectedResolutionLabel.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.CornflowerBlue);
                }
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
