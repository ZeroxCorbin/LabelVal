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

namespace LabelVal.LVS_95xx.Views;
/// <summary>
/// Interaction logic for VerifierManager.xaml
/// </summary>
public partial class VerifierManager : UserControl
{
    public VerifierManager()
    {
        InitializeComponent();
    }


    //private void btnShowDetails_Click(object sender, RoutedEventArgs e) => ((MainWindowView)App.Current.MainWindow).ScannerDetails.IsLeftDrawerOpen = !((MainWindowView)App.Current.MainWindow).ScannerDetails.IsLeftDrawerOpen;
}
