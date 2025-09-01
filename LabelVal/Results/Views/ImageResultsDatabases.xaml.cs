using System.Windows;
using System.Windows.Controls;

namespace LabelVal.Results.Views;
/// <summary>
/// Interaction logic for ImageResultsDatabasesView.xaml
/// </summary>
public partial class ImageResultsDatabases : UserControl
{
    public ImageResultsDatabases() => InitializeComponent();

    private void btnLockImageResultsDatabase_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if(sender is Button button && button.DataContext is Databases.ImageResultsDatabase ir)
            ir.IsLocked = !ir.IsLocked;
    }

    private void btnCollapseContent(object sender, System.Windows.RoutedEventArgs e) => ((Main.Views.MainWindow)Application.Current.MainWindow).ClearSelectedMenuItem();

    private void btnOpenImageResultLocation(object sender, System.Windows.RoutedEventArgs e)
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
        {
            FileName = $"{App.ImageResultsDatabaseRoot}\\",
            UseShellExecute = true,
            Verb = "open"
        });
    }
}
