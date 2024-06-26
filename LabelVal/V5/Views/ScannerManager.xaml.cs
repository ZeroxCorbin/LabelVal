using LabelVal.WindowViews;
using System.Windows;
using System.Windows.Controls;

namespace LabelVal.V5.Views;

/// <summary>
/// Interaction logic for ScannerManager.xaml
/// </summary>
public partial class ScannerManager : UserControl
{
    public ScannerManager() => InitializeComponent();

    private void btnShowDetails_Click(object sender, RoutedEventArgs e) => ((MainWindowView)App.Current.MainWindow).ScannerDetails.IsLeftDrawerOpen = !((MainWindowView)App.Current.MainWindow).ScannerDetails.IsLeftDrawerOpen;
}
