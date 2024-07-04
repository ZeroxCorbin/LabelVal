using System;
using System.Windows;
using System.Windows.Controls;

namespace LabelVal.Run.Views;
/// <summary>
/// Interaction logic for RunView.xaml
/// </summary>
public partial class RunControl : UserControl
{
    //private Run.Views.Run win;

    public RunControl() => InitializeComponent();

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
