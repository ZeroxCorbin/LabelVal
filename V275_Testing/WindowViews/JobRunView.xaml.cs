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

namespace V275_Testing.WindowViews
{
    /// <summary>
    /// Interaction logic for JobRunView.xaml
    /// </summary>
    public partial class JobRunView : MetroWindow
    {
        public JobRunView()
        {
            InitializeComponent();

            
            JobList.IsOpen = true;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            JobList.IsOpen = true;
        }

        private void JobList_LostMouseCapture(object sender, MouseEventArgs e)
        {
            JobList.IsOpen = false;
        }

        private void JobList_LostFocus(object sender, RoutedEventArgs e)
        {
            if (JobList.IsShown)
                JobList.IsOpen = false;
        }

        private void BtnSort_Click(object sender, RoutedEventArgs e)
        {
            CollectionViewSource viewSource = FindResource("GroupedDataList") as CollectionViewSource;
            viewSource.SortDescriptions.Clear();
            viewSource.SortDescriptions.Add(new System.ComponentModel.SortDescription("TimeDate", System.ComponentModel.ListSortDirection.Descending));

        }
    }
}
