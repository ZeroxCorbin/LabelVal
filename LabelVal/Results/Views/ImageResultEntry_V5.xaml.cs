using LabelVal.Dialogs;
using LabelVal.Sectors.Views;
using LabelVal.WindowViews;
using MahApps.Metro.Controls.Dialogs;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LabelVal.Results.Views;
/// <summary>
/// Interaction logic for ImageResultEntry_V5.xaml
/// </summary>
public partial class ImageResultEntry_V5 : UserControl
{
    public ImageResultEntry_V5()
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
                case "v5Stored":
                    ((ViewModels.ImageResultEntry)DataContext).V5FocusedStoredSector = null;
                    break;
                case "v5Current":
                    ((ViewModels.ImageResultEntry)DataContext).V5FocusedCurrentSector = null;
                    ((ViewModels.ImageResultEntry)DataContext).V5FocusedStoredSector = null;
                    break;
            }
        }
    }

    private void V5StoredSectors_Click(object sender, RoutedEventArgs e)
    {
        if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
        {
            if (((ViewModels.ImageResultEntry)DataContext).V5ResultRow != null)
            {
                var pop = new PopupJSONViewer();
                pop.Viewer1.JSON = ((ViewModels.ImageResultEntry)DataContext).V5ResultRow.Report;
                pop.Viewer1.Title = "Report";

                pop.Popup.PlacementTarget = (Button)sender;
                pop.Popup.IsOpen = true;
            }
        }
        else
        {
            var pop = new PopupSectorsDetails();
            pop.DataContext = ((ViewModels.ImageResultEntry)DataContext).V5StoredSectors;

            pop.Popup.PlacementTarget = (Button)sender;
            pop.Popup.IsOpen = true;
        }
    }
    private void V5CurrentSectorDetails_Click(object sender, RoutedEventArgs e)
    {
        if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
        {
            if (((ViewModels.ImageResultEntry)DataContext).V5CurrentReport != null)
            {
                var pop = new PopupJSONViewer();
                pop.Viewer1.JSON = ((ViewModels.ImageResultEntry)DataContext).V5CurrentReport;
                pop.Viewer1.Title = "Report";

                pop.Popup.PlacementTarget = (Button)sender;
                pop.Popup.IsOpen = true;
            }
        }
        else
        {
            var pop = new PopupSectorsDetails();
            pop.DataContext = ((ViewModels.ImageResultEntry)DataContext).V5CurrentSectors;

            pop.Popup.PlacementTarget = (Button)sender;
            pop.Popup.IsOpen = true;
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
