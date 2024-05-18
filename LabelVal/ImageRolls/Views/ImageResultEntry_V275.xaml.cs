using LabelVal.Dialogs;
using LabelVal.WindowViews;
using MahApps.Metro.Controls.Dialogs;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace LabelVal.ImageRolls.Views;
/// <summary>
/// Interaction logic for ImageResultEntry_V275.xaml
/// </summary>
public partial class ImageResultEntry_V275 : UserControl
{
    public ImageResultEntry_V275()
    {
        InitializeComponent();
    }

    private void V275StoredSectors_Click(object sender, RoutedEventArgs e)
    {
        if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
        {
            if (((ViewModels.ImageResultEntry)DataContext).V275ResultRow != null)
            {
                var pop = new PopupJSONViewer();
                pop.Viewer1.JSON = ((ViewModels.ImageResultEntry)DataContext).V275ResultRow.Template;
                pop.Viewer1.Title = "Template";
                pop.Viewer2.JSON = ((ViewModels.ImageResultEntry)DataContext).V275ResultRow.Report;
                pop.Viewer2.Title = "Report";

                pop.Popup.PlacementTarget = (Button)sender;
                pop.Popup.IsOpen = true;
            }
        }
        else
        {
            if (((ViewModels.ImageResultEntry)DataContext).V275StoredSectors.Count > 0)
            {
                V275StoredSectorsDetailsPopup.PlacementTarget = (Button)sender;
                V275StoredSectorsDetailsPopup.IsOpen = !V275StoredSectorsDetailsPopup.IsOpen;
            }
            else
                V275StoredSectorsDetailsPopup.IsOpen = false;
        }
    }
    private void V275CurrentSectorDetails_Click(object sender, RoutedEventArgs e)
    {
        if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
        {
            var pop = new PopupJSONViewer();
            pop.Viewer1.JSON = ((ViewModels.ImageResultEntry)DataContext).V275CurrentTemplate;
            pop.Viewer1.Title = "Template";
            pop.Viewer2.JSON = ((ViewModels.ImageResultEntry)DataContext).V275CurrentReport;
            pop.Viewer2.Title = "Report";

            pop.Popup.PlacementTarget = (Button)sender;
            pop.Popup.IsOpen = true;
        }
        else
        {
            if (((ViewModels.ImageResultEntry)DataContext).V275CurrentSectors.Count > 0)
            {
                V275CurrentSectorsDetailsPopup.PlacementTarget = (Button)sender;
                V275CurrentSectorsDetailsPopup.IsOpen = true;
            }
        }
    }
    private void ScrollV275StoredSectors_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (e.VerticalChange != 0)
            ScrollV275CurrentSectors.ScrollToVerticalOffset(e.VerticalOffset);
    }
    private void ScrollV275CurrentSectors_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (e.VerticalChange != 0)
            ScrollV275StoredSectors.ScrollToVerticalOffset(e.VerticalOffset);
    }

}
