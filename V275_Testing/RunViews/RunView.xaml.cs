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

            if (FindResource("LabelsDataList") is CollectionViewSource viewSource)
            {
                viewSource.GroupDescriptions.Clear();
                viewSource.GroupDescriptions.Add(new PropertyGroupDescription(App.Settings.GetValue("LabelsDataList_Group", "Run.LoopCount")));
            }

            if (FindResource("RunEntriesDataList") is CollectionViewSource viewSource1)
            {
                viewSource1.SortDescriptions.RemoveAt(3);
                viewSource1.SortDescriptions.Add(new System.ComponentModel.SortDescription("TimeDate", App.Settings.GetValue("RunEntriesDataList_Sort", System.ComponentModel.ListSortDirection.Descending)));
            }
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

        private void BtnSortLabels_Click(object sender, RoutedEventArgs e)
        {
            if (FindResource("LabelsDataList") is CollectionViewSource viewSource)
                if (((PropertyGroupDescription)viewSource.GroupDescriptions[0]).PropertyName == "Run.LoopCount")
                {
                    viewSource.GroupDescriptions.Clear();
                    viewSource.GroupDescriptions.Add(new PropertyGroupDescription("Run.LabelImageUID"));
                    App.Settings.SetValue("LabelsDataList_Group", "Run.LabelImageUID");
                }
                else
                {
                    viewSource.GroupDescriptions.Clear();
                    viewSource.GroupDescriptions.Add(new PropertyGroupDescription("Run.LoopCount"));
                    App.Settings.SetValue("LabelsDataList_Group", "Run.LoopCount");
                }
        }

        private void BtnSortRuns_Click(object sender, RoutedEventArgs e)
        {
            if (FindResource("RunEntriesDataList") is CollectionViewSource viewSource)
                if (viewSource.SortDescriptions[3].Direction == System.ComponentModel.ListSortDirection.Ascending)
                {
                    viewSource.SortDescriptions.RemoveAt(3);
                    viewSource.SortDescriptions.Add(new System.ComponentModel.SortDescription("TimeDate", System.ComponentModel.ListSortDirection.Descending));
                    App.Settings.GetValue("RunEntriesDataList_Sort", System.ComponentModel.ListSortDirection.Descending);
                }
                else
                {
                    viewSource.SortDescriptions.RemoveAt(3);
                    viewSource.SortDescriptions.Add(new System.ComponentModel.SortDescription("TimeDate", System.ComponentModel.ListSortDirection.Ascending));
                    App.Settings.GetValue("RunEntriesDataList_Sort", System.ComponentModel.ListSortDirection.Ascending);
                }
        }

        private void BtnAutoScroll_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
