using LabelVal.WindowViewModels;
using LabelVal.WindowViews;
using System.Windows;
using System.Windows.Controls;

namespace LabelVal.V5.Views;

/// <summary>
/// Interaction logic for ScannerManager.xaml
/// </summary>
public partial class ScannerManager : UserControl
{
    public ScannerManager()
    {
        InitializeComponent();
    }

    private void btnShowDetails_Click(object sender, RoutedEventArgs e)
    {
        var view = ((MainWindowView)App.Current.MainWindow).ScannerDetails;
        var vm = ((MainWindowView)App.Current.MainWindow).DataContext as MainWindowViewModel;
        if (view.LeftDrawerContent == null)
        {
            var details = new Views.ScannerDetails();
            details.DataContext = vm.ScannerDetails;
            view.LeftDrawerContent = details;
        }
        view.IsLeftDrawerOpen = !view.IsLeftDrawerOpen;
    }
}
