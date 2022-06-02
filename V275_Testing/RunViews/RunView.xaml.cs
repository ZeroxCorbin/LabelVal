using MahApps.Metro.Controls;
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
using System.Windows.Shapes;
using V275_Testing.WindowViewModels;

namespace V275_Testing.RunViews
{
    /// <summary>
    /// Interaction logic for RunRunView.xaml
    /// </summary>
    public partial class RunView : MetroWindow
    {
        public RunView()
        {
            InitializeComponent();

            
            RunList.IsOpen = true;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            RunList.IsOpen = true;
        }

        private void RunList_LostMouseCapture(object sender, MouseEventArgs e)
        {
            RunList.IsOpen = false;
        }

        private void RunList_LostFocus(object sender, RoutedEventArgs e)
        {
            if (RunList.IsShown)
                RunList.IsOpen = false;
        }

        private void BtnSort_Click(object sender, RoutedEventArgs e)
        {
            CollectionViewSource viewSource = FindResource("GroupedDataList") as CollectionViewSource;
            viewSource.SortDescriptions.Clear();
            viewSource.SortDescriptions.Add(new System.ComponentModel.SortDescription("TimeDate", System.ComponentModel.ListSortDirection.Descending));

        }
    }
}
