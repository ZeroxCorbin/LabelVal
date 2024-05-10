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
                L95xxStoredTemplateJsonView.Load(((ViewModels.ImageResultEntry)DataContext).L95xxResultRow.Template);
                L95xxStoredReportJsonView.Load(((ViewModels.ImageResultEntry)DataContext).L95xxResultRow.Report);
                L95xxStoredJsonPopup.PlacementTarget = (Button)sender;
                L95xxStoredJsonPopup.IsOpen = true;
            }
        }
        else
        {
            if (((ViewModels.ImageResultEntry)DataContext).L95xxStoredSectors.Count > 0)
            {
                L95xxStoredSectorsDetailsPopup.PlacementTarget = (Button)sender;
                L95xxStoredSectorsDetailsPopup.IsOpen = true;
            }
        }
    }
    private void L95xxCurrentSectorDetails_Click(object sender, RoutedEventArgs e)
    {
        if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
        {
            if (((ViewModels.ImageResultEntry)DataContext).L95xxCurrentReport != null)
            {
                L95xxCurrentReportJsonView.Load(Newtonsoft.Json.JsonConvert.SerializeObject(((ViewModels.ImageResultEntry)DataContext).L95xxCurrentReport));
                L95xxCurrentReportJsonPopup.PlacementTarget = (Button)sender;
                L95xxCurrentReportJsonPopup.IsOpen = true;
            }
        }
        else
        {
            if (((ViewModels.ImageResultEntry)DataContext).L95xxCurrentSectors.Count > 0)
            {
                L95xxCurrentSectorsDetailsPopup.PlacementTarget = (Button)sender;
                L95xxCurrentSectorsDetailsPopup.IsOpen = true;
            }
        }
    }
    private void L95xxImage_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            _ = ShowImage(((ViewModels.ImageResultEntry)DataContext).L95xxImage, ((ViewModels.ImageResultEntry)DataContext).L95xxSectorsImageOverlay);
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

    private bool ShowImage(byte[] image, DrawingImage overlay)
    {
        var dc = new ImageViewerDialogViewModel();

        dc.LoadImage(image, overlay);
        if (dc.Image == null) return false;

        var yourParentWindow = (MainWindowView)Window.GetWindow(this);

        dc.Width = yourParentWindow.ActualWidth - 100;
        dc.Height = yourParentWindow.ActualHeight - 100;

        _ = DialogCoordinator.Instance.ShowMetroDialogAsync(yourParentWindow.DataContext, new ImageViewerDialogView() { DataContext = dc });

        return true;

    }
}
