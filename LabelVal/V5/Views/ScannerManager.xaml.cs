using System.Windows;
using System.Windows.Controls;

namespace LabelVal.V5.Views;

/// <summary>
/// Interaction logic for ScannerManager.xaml
/// </summary>
public partial class ScannerManager : UserControl
{
    public ScannerManager() => InitializeComponent();

    private void btnShowDetails_Click(object sender, RoutedEventArgs e)
    {
        var view = ((Main.Views.MainWindow)App.Current.MainWindow).ScannerDetails;
        var vm = ((Main.Views.MainWindow)App.Current.MainWindow).DataContext as Main.ViewModels.MainWindow;
        if (view.LeftDrawerContent == null)
        {
            ScannerDetails details = new()
            {
                DataContext = vm.ScannerDetails
            };
            view.LeftDrawerContent = details;
        }
        view.IsLeftDrawerOpen = !view.IsLeftDrawerOpen;
    }
}
