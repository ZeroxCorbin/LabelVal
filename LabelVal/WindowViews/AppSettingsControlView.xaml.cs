using LabelVal.ORM_Test;
using LabelVal.WindowViewModels;
using MahApps.Metro.SimpleChildWindow;
using System;
using System.Collections.Generic;
using System.IO;
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

namespace LabelVal.WindowViews
{
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

        private void btnSelectDirectory_Click(object sender, RoutedEventArgs e)
        {
            using (var fbd = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult result = fbd.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    ((MainWindowViewModel)DataContext).V275NodesViewModel.SimulatorImageDirectory = fbd.SelectedPath;
                }
            }
        }

        private void btnShowORMSettingsDialog_Click(object sender, RoutedEventArgs e)
        {
            var tmp = new NHibernateSettingsView();
            tmp.Owner = App.Current.MainWindow;
            tmp.Show();
        }
    }
}
