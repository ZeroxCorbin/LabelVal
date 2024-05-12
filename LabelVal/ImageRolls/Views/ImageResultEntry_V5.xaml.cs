using LabelVal.Dialogs;
using LabelVal.WindowViews;
using MahApps.Metro.Controls.Dialogs;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace LabelVal.ImageRolls.Views;
/// <summary>
/// Interaction logic for ImageResultEntry_V5.xaml
/// </summary>
public partial class ImageResultEntry_V5 : UserControl
{
    public ImageResultEntry_V5()
    {
        InitializeComponent();
    }

    private void V5StoredSectors_Click(object sender, RoutedEventArgs e)
    {
        if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
        {
            if (((ViewModels.ImageResultEntry)DataContext).V5ResultRow != null)
            {
                V5StoredTemplateJsonView.Load(((ViewModels.ImageResultEntry)DataContext).V5ResultRow.Template);
                V5StoredReportJsonView.Load(((ViewModels.ImageResultEntry)DataContext).V5ResultRow.Report);
                V5StoredJsonPopup.PlacementTarget = (Button)sender;
                V5StoredJsonPopup.IsOpen = true;
            }
        }
        else
        {
            if (((ViewModels.ImageResultEntry)DataContext).V5StoredSectors.Count > 0)
            {
                V5StoredSectorsDetailsPopup.PlacementTarget = (Button)sender;
                V5StoredSectorsDetailsPopup.IsOpen = true;
            }
        }
    }
    private void V5CurrentSectorDetails_Click(object sender, RoutedEventArgs e)
    {
        if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
        {
            if (((ViewModels.ImageResultEntry)DataContext).V5CurrentReport != null)
            {
                V5CurrentReportJsonView.Load(Newtonsoft.Json.JsonConvert.SerializeObject(((ViewModels.ImageResultEntry)DataContext).V5CurrentReport));
                V5CurrentReportJsonPopup.PlacementTarget = (Button)sender;
                V5CurrentReportJsonPopup.IsOpen = true;
            }
        }
        else
        {
            if (((ViewModels.ImageResultEntry)DataContext).V5CurrentSectors.Count > 0)
            {
                V5CurrentSectorsDetailsPopup.PlacementTarget = (Button)sender;
                V5CurrentSectorsDetailsPopup.IsOpen = true;
            }
        }
    }
    private void ScrollV5StoredSectors_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (e.VerticalChange != 0)
            ScrollV5CurrentSectors.ScrollToVerticalOffset(e.VerticalOffset);
    }
    private void ScrollV5CurrentSectors_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (e.VerticalChange != 0)
            ScrollV5StoredSectors.ScrollToVerticalOffset(e.VerticalOffset);
    }
}
