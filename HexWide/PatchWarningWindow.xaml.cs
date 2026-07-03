using System.Windows;

namespace HexWide;

public partial class PatchWarningWindow : Window
{
    public PatchWarningWindow()
    {
        InitializeComponent();
    }

    public bool DontShowAgain => chkDontShow.IsChecked == true;

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
