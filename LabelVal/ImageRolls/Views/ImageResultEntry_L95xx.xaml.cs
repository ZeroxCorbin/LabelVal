using LabelVal.Dialogs;
using LabelVal.WindowViews;
using MahApps.Metro.Controls.Dialogs;
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

namespace LabelVal.ImageRolls.Views;
/// <summary>
/// Interaction logic for ImageResultEntry_L95xx.xaml
/// </summary>
public partial class ImageResultEntry_L95xx : UserControl
{
    public ImageResultEntry_L95xx()
    {
        InitializeComponent();
    }

    private void L95xxStoredSectors_Click(object sender, RoutedEventArgs e)
    {
        if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
        {
            if (((ViewModels.ImageResultEntry)DataContext).L95xxResultRow != null)
            {
                var pop = new PopupJSONViewer();
                pop.Viewer1.JSON = ((ViewModels.ImageResultEntry)DataContext).L95xxResultRow.Report;
                pop.Viewer1.Title = "Report";

                pop.Popup.PlacementTarget = (Button)sender;
                pop.Popup.IsOpen = true;
            }
        }
        else
        {
            var pop = new PopupSectorsDetails();
            pop.ItemsControl.ItemsSource = ((ViewModels.ImageResultEntry)DataContext).L95xxStoredSectors;

            pop.Popup.PlacementTarget = (Button)sender;
            pop.Popup.IsOpen = true;
        }
    }
    private void L95xxCurrentSectorDetails_Click(object sender, RoutedEventArgs e)
    {
        if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
        {
            var pop = new PopupJSONViewer();
            pop.Viewer1.JSON = ((ViewModels.ImageResultEntry)DataContext).L95xxCurrentReport;
            pop.Viewer1.Title = "Report";

            pop.Popup.PlacementTarget = (Button)sender;
            pop.Popup.IsOpen = true;
        }
        else
        {
            var pop = new PopupSectorsDetails();
            pop.ItemsControl.ItemsSource = ((ViewModels.ImageResultEntry)DataContext).L95xxCurrentSectors;

            pop.Popup.PlacementTarget = (Button)sender;
            pop.Popup.IsOpen = true;
        }
    }
    private void ScrollL95xxStoredSectors_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (e.VerticalChange != 0)
            ScrollL95xxCurrentSectors.ScrollToVerticalOffset(e.VerticalOffset);
    }
    private void ScrollL95xxCurrentSectors_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (e.VerticalChange != 0)
            ScrollL95xxStoredSectors.ScrollToVerticalOffset(e.VerticalOffset);
    }

}
