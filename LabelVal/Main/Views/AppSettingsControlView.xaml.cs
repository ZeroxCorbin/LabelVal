using LabelVal.ORM_Test;
using System.Windows;
using System.Windows.Controls;

namespace LabelVal.Main.Views;

/// <summary>
/// Interaction logic for AppSettingsControlView.xaml
/// </summary>
public partial class AppSettingsControlView : UserControl
{
    public AppSettingsControlView()
    {
        InitializeComponent();

        DataContext = App.Current.MainWindow.DataContext;
    }

    //private void btnSelectDirectory_Click(object sender, RoutedEventArgs e)
    //{
    //    using System.Windows.Forms.FolderBrowserDialog fbd = new();
    //    System.Windows.Forms.DialogResult result = fbd.ShowDialog();

    //    if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
    //    {
    //        ((ViewModels.MainWindow)DataContext).NodeManager.SimulatorImageDirectory = fbd.SelectedPath;
    //    }
    //}

    private void btnShowORMSettingsDialog_Click(object sender, RoutedEventArgs e)
    {
        NHibernateSettingsView tmp = new()
        {
            Owner = App.Current.MainWindow
        };
        tmp.Show();
    }
}
