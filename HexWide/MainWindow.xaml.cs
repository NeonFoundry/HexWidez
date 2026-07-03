using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Serilog;

namespace HexWide
{
    public partial class MainWindow : Window
    {
        private readonly PatchViewModel _viewModel;
        private CancellationTokenSource? _patchCancellationTokenSource;

        public MainWindow() : this(new PatchService(), ResolutionOptions.CreateDefaults())
        {
        }

        public MainWindow(IPatchService patchService, IReadOnlyDictionary<string, string> resolutionOptions)
        {
            _viewModel = new PatchViewModel(patchService, resolutionOptions);

            InitializeComponent();
            comboResolutions.ItemsSource = _viewModel.AvailableResolutions;
            comboResolutions.SelectedIndex = 0;
            ResetProgressUi();
            ShowStatus("Select an executable to get started.", false);
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow { Owner = this };
            var result = settingsWindow.ShowDialog();
            if (result == true)
            {
                // reload settings and update UI
                var settings = AppSettingsLoader.Load();
                var newRes = settings.Resolutions;
                _viewModel.UpdateResolutionOptions(newRes);
                comboResolutions.ItemsSource = _viewModel.AvailableResolutions;
                if (comboResolutions.Items.Count > 0)
                    comboResolutions.SelectedIndex = 0;
            }
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FilePickerWindow
            {
                Owner = this,
                Filter = "*.exe",
            };

            // If current path is a file, set initial directory to its folder
            try
            {
                if (!string.IsNullOrWhiteSpace(txtPath.Text) && File.Exists(txtPath.Text))
                    picker.InitialDirectory = Path.GetDirectoryName(txtPath.Text) ?? picker.InitialDirectory;
            }
            catch { }

            var result = picker.ShowDialog();
            if (result == true && !string.IsNullOrWhiteSpace(picker.SelectedFile))
            {
                txtPath.Text = picker.SelectedFile;
            }
        }

        private async void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ExecutablePath = txtPath.Text;
            _viewModel.SelectedResolution = comboResolutions.SelectedItem?.ToString();
            _viewModel.IncludeFovFix = chkFovFix.IsChecked == true;

            var validation = _viewModel.ValidatePatchRequest();
            if (!validation.IsValid)
            {
                ShowStatus(validation.ErrorMessage!, true);
                return;
            }

            // Show one-time patch warning if enabled in settings
            try
            {
                var settings = AppSettingsLoader.Load();
                if (settings.ShowPatchWarning)
                {
                    var warn = new PatchWarningWindow { Owner = this };
                    var dlg = warn.ShowDialog();
                    if (dlg != true)
                    {
                        ShowStatus("Patch cancelled.", true);
                        return;
                    }

                    if (warn.DontShowAgain)
                    {
                        settings.ShowPatchWarning = false;
                        try { AppSettingsLoader.Save(settings); } catch { }
                    }
                }
            }
            catch { }

            _patchCancellationTokenSource?.Cancel();
            _patchCancellationTokenSource = new CancellationTokenSource();

            try
            {
                ResetProgressUi();
                ShowStatus("Preparing patch operation...", false);
                var progress = new Progress<int>(percent =>
                {
                    patchProgressBar.Visibility = Visibility.Visible;
                    progressLabel.Visibility = Visibility.Visible;
                    patchProgressBar.Value = percent;
                    progressLabel.Text = $"{percent}% complete";
                    ShowStatus($"Patching in progress... {percent}%", false);
                });
                PatchResult result = await _viewModel.ApplyPatchAsync(progress, _patchCancellationTokenSource.Token);
                ResetProgressUi();
                ShowStatus($"Patch applied successfully. {result.ReplacementCount} replacements made. Backup saved to: {result.BackupPath}", false);
                Log.Information("Patch completed successfully for {ExecutablePath}", txtPath.Text);
            }
            catch (OperationCanceledException)
            {
                ResetProgressUi();
                ShowStatus("Patch cancelled.", true);
                Log.Information("Patch cancelled by user.");
            }
            catch (Exception ex)
            {
                ResetProgressUi();
                ShowStatus($"Patch failed: {ex.Message}", true);
                Log.Error(ex, "Patch failed for {ExecutablePath}", txtPath.Text);
            }
        }

        private void ShowStatus(string message, bool isError)
        {
            statusMessage.Text = message;
            statusMessage.Foreground = isError
                ? new SolidColorBrush(Color.FromRgb(248, 113, 113))
                : new SolidColorBrush(Color.FromRgb(56, 189, 248));
        }

        private void ResetProgressUi()
        {
            patchProgressBar.Visibility = Visibility.Collapsed;
            patchProgressBar.Value = 0;
            progressLabel.Visibility = Visibility.Collapsed;
            progressLabel.Text = string.Empty;
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.SystemCommands.MinimizeWindow(this);
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Normal)
                System.Windows.SystemCommands.MaximizeWindow(this);
            else
                System.Windows.SystemCommands.RestoreWindow(this);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.SystemCommands.CloseWindow(this);
        }

        private static DependencyObject? GetParentElement(DependencyObject? element)
        {
            if (element == null)
                return null;

            if (element is Visual || element is System.Windows.Media.Media3D.Visual3D)
                return VisualTreeHelper.GetParent(element);

            if (element is FrameworkContentElement contentElement)
                return contentElement.Parent;

            return LogicalTreeHelper.GetParent(element);
        }

        private static bool IsDescendantOf<T>(DependencyObject? element) where T : DependencyObject
        {
            while (element != null)
            {
                if (element is T)
                    return true;

                element = GetParentElement(element);
            }

            return false;
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.Handled)
                return;

            if (IsDescendantOf<Button>(e.OriginalSource as DependencyObject))
                return;

            if (e.ClickCount == 2)
            {
                // double-click toggles maximize
                MaximizeButton_Click(sender, e);
                return;
            }

            if (e.ButtonState == MouseButtonState.Pressed)
            {
                try
                {
                    this.DragMove();
                }
                catch
                {
                    // ignore drag exceptions when window state prevents it
                }
            }
        }
    }
}