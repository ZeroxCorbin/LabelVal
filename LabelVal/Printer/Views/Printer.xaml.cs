using LabelVal.WindowViewModels;
using LabelVal.WindowViews;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LabelVal.Printer.Views;
/// <summary>
/// Interaction logic for PrinterControlView.xaml
/// </summary>
public partial class Printer : UserControl
{
    public Printer()
    {
        InitializeComponent();
    }

    private void btnShowPrinterDetails_Click(object sender, RoutedEventArgs e)
    {
        var view = ((MainWindowView)App.Current.MainWindow).PrinterDetails;
        var vm = ((MainWindowView)App.Current.MainWindow).DataContext as MainWindowViewModel;
        if (view.LeftDrawerContent == null)
        {
            var details = new Views.PrinterDetails();
            details.DataContext = vm.PrinterDetails;
            view.LeftDrawerContent = details;
        }
        view.IsLeftDrawerOpen = !view.IsLeftDrawerOpen;
    }
}
