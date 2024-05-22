using LabelVal.WindowViews;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace LabelVal.V275.Views;
/// <summary>
/// Interaction logic for V275NodesView.xaml
/// </summary>
public partial class V275 : UserControl
{
    public V275() => InitializeComponent();

    public void btnShowDetails_Click(object sender, RoutedEventArgs e) => ((MainWindowView)App.Current.MainWindow).NodeDetails.IsLeftDrawerOpen = !((MainWindowView)App.Current.MainWindow).NodeDetails.IsLeftDrawerOpen;

    private void btnShowSettings_Click(object sender, RoutedEventArgs e) => drwSettings.IsTopDrawerOpen = !drwSettings.IsTopDrawerOpen;

    private void btnOpenInBrowser_Click(object sender, RoutedEventArgs e)
    {
        var v275 = $"http://{((ViewModels.V275)DataContext).V275_Host}:{((ViewModels.V275)DataContext).V275_SystemPort}";
        var ps = new ProcessStartInfo(v275)
        {
            UseShellExecute = true,
            Verb = "open"
        };
        _ = Process.Start(ps);
    }
}
