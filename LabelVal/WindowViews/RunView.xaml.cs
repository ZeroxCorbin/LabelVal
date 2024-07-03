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

namespace LabelVal.WindowViews;
/// <summary>
/// Interaction logic for RunView.xaml
/// </summary>
public partial class RunView : UserControl
{
    //private Run.Views.Run win;

    public RunView()
    {
        InitializeComponent();
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        //if (win != null)
        //{
        //    win.BringIntoView();
        //    return;
        //}

        //win = new Run.Views.Run();
        //win.Closed += Win_Closed;
        //win.Show();

    }

    private void Win_Closed(object sender, EventArgs e)
    {
        //((Run.Views.Run)sender).Closed -= Win_Closed;

        //win = null;

        //GC.Collect();
    }

    
}
