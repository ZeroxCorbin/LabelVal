using LabelVal.Sectors.Views;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LabelVal.Results.Views;
/// <summary>
/// Interaction logic for ImageResultEntry_L95xx.xaml
/// </summary>
public partial class ImageResultEntry_L95xx : UserControl
{
    public ImageResultEntry_L95xx()
    {
        InitializeComponent();
    }

    private void btnCloseDetails_Click(object sender, RoutedEventArgs e)
    {
        if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
        {
            ((ViewModels.ImageResultEntry)DataContext).V275FocusedStoredSector = null;
            ((ViewModels.ImageResultEntry)DataContext).V275FocusedCurrentSector = null;
            ((ViewModels.ImageResultEntry)DataContext).V5FocusedStoredSector = null;
            ((ViewModels.ImageResultEntry)DataContext).V5FocusedCurrentSector = null;
            ((ViewModels.ImageResultEntry)DataContext).L95xxFocusedStoredSector = null;
            ((ViewModels.ImageResultEntry)DataContext).L95xxFocusedCurrentSector = null;
        }
        else
        {
            switch ((string)((Button)sender).Tag)
            {
                case "l95xxStored":
                    ((ViewModels.ImageResultEntry)DataContext).L95xxFocusedStoredSector = null;
                    break;
                case "l95xxCurrent":
                    ((ViewModels.ImageResultEntry)DataContext).L95xxFocusedCurrentSector = null;
                    ((ViewModels.ImageResultEntry)DataContext).L95xxFocusedStoredSector = null;
                    break;
            }
        }
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
            var pop = new PopupSectorsDetails
            {
                DataContext = ((ViewModels.ImageResultEntry)DataContext).L95xxStoredSectors
            };

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
            var pop = new PopupSectorsDetails
            {
                DataContext = ((ViewModels.ImageResultEntry)DataContext).L95xxCurrentSectors
            };

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

    private void btnSaveImage_Click(object sender, RoutedEventArgs e)
    {
        var sect = Utilities.VisualTreeHelp.GetVisualParent<Sector>((Button)sender);

        if (sect != null)
        {
            string path;
            if ((path = Utilities.FileUtilities.SaveFileDialog("plot", "PNG|*.png", "Save sector details.")) != "")
            {
                try
                {
                    SaveToPng(sect, path);
                }
                catch { }
            }
        }
    }
    private void btnCopyImage_Click(object sender, RoutedEventArgs e)
    {
        var sect = Utilities.VisualTreeHelp.GetVisualParent<Sector>((Button)sender);

        if (sect != null)
            CopyToClipboard(sect);

    }
    public void SaveToPng(FrameworkElement visual, string fileName)
    {
        var encoder = new PngBitmapEncoder();
        EncodeVisual(visual, encoder);

        using var stream = System.IO.File.Create(fileName);
        encoder.Save(stream);
    }
    public void CopyToClipboard(FrameworkElement visual)
    {
        var encoder = new PngBitmapEncoder();
        EncodeVisual(visual, encoder);

        using (var stream = new System.IO.MemoryStream())
        {
            encoder.Save(stream);
            stream.Seek(0, System.IO.SeekOrigin.Begin);
            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.StreamSource = stream;
            bitmapImage.EndInit();
            Clipboard.SetImage(bitmapImage);
        }
    }
    private static void EncodeVisual(FrameworkElement visual, BitmapEncoder encoder)
    {
        var bitmap = new RenderTargetBitmap((int)visual.ActualWidth, (int)visual.ActualHeight, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        var frame = BitmapFrame.Create(bitmap);
        encoder.Frames.Add(frame);
    }

}
