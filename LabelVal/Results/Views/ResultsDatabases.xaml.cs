using LabelVal.Results.Databases;
using System.Windows;
using System.Windows.Controls;

namespace LabelVal.Results.Views;
/// <summary>
/// Interaction logic for (ResultssDatabasesView.xaml
/// </summary>
public partial class ResultssDatabases : UserControl
{
    public ResultssDatabases() => InitializeComponent();

    private void btnLockResultssDatabase_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if(sender is Button button && button.DataContext is ResultsDatabase ir)
            ir.IsLocked = !ir.IsLocked;
    }

    private void btnCollapseContent(object sender, System.Windows.RoutedEventArgs e) => ((Main.Views.MainWindow)Application.Current.MainWindow).ClearSelectedMenuItem();

    private void btnOpenResultsLocation(object sender, System.Windows.RoutedEventArgs e)
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
        {
            FileName = $"{App.ResultssDatabaseRoot}\\",
            UseShellExecute = true,
            Verb = "open"
        });
    }
}
