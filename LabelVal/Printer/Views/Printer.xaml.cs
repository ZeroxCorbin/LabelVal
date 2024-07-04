using System.Windows;
using System.Windows.Controls;

namespace LabelVal.Printer.Views;
/// <summary>
/// Interaction logic for PrinterControlView.xaml
/// </summary>
public partial class Printer : UserControl
{
    public Printer() => InitializeComponent();

    private void btnShowPrinterDetails_Click(object sender, RoutedEventArgs e)
    {
        MaterialDesignThemes.Wpf.DrawerHost view = ((Main.Views.MainWindow)App.Current.MainWindow).PrinterDetails;
        Main.ViewModels.MainWindow vm = ((Main.Views.MainWindow)App.Current.MainWindow).DataContext as Main.ViewModels.MainWindow;
        if (view.LeftDrawerContent == null)
        {
            PrinterDetails details = new()
            {
                DataContext = vm.PrinterDetails
            };
            view.LeftDrawerContent = details;
        }
        view.IsLeftDrawerOpen = !view.IsLeftDrawerOpen;
    }
}
