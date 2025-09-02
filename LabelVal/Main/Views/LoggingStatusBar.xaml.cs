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

namespace LabelVal.Main.Views;
/// <summary>
/// Interaction logic for LoggingStatusBar.xaml
/// </summary>
public partial class LoggingStatusBar : UserControl
{
    public LoggingStatusBar()
    {
        InitializeComponent();
    }

    private void btnShowViewer_Click(object sender, RoutedEventArgs e)
    {
        PopupViewer.IsOpen = !PopupViewer.IsOpen;
    }
}
