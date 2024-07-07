using System;
using System.Windows;
using System.Windows.Controls;

namespace LabelVal.Run.Views;
/// <summary>
/// Interaction logic for RunView.xaml
/// </summary>
public partial class RunControl : UserControl
{
    private Run.Views.MainWindow RunWindow;

    public RunControl() => InitializeComponent();

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        if (RunWindow != null)
        {
            RunWindow.BringIntoView();
            return;
        }

        RunWindow = new Run.Views.MainWindow();
        RunWindow.Closed += Win_Closed;
        RunWindow.Show();
    }

    private void Win_Closed(object sender, EventArgs e)
    {
        ((Run.Views.MainWindow)sender).Closed -= Win_Closed;
        RunWindow = null;
    }
}
