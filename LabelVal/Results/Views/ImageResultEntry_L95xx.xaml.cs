using LabelVal.Dialogs;
using LabelVal.ImageRolls.ViewModels;
using LabelVal.Sectors.Views;
using MahApps.Metro.Controls.Dialogs;
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
            pop.Viewer1.JSON = ((ViewModels.ImageResultEntry)DataContext).L95xxResultRow.Report;
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

    private void lstDissimilarSector_Click(object sender, MouseButtonEventArgs e)
    {
        SectorDifferences sndr = (SectorDifferences)sender;
        System.Collections.ObjectModel.Collection<Sector> sectors = Utilities.VisualTreeHelp.GetVisualChildren<Sector>(this);
        foreach (Sector s in sectors)
        {
            if (s.SectorName == ((Sectors.Interfaces.ISectorDifferences)sndr.DataContext).UserName)
                s.ShowSectorDetails();
        }
    }

    private void L95xxStoredImage_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            _ = ShowImage(((ViewModels.ImageResultEntry)DataContext).L95xxStoredImage, ((ViewModels.ImageResultEntry)DataContext).L95xxStoredImageOverlay);
    }
    private void L95xxCurrentImage_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            _ = ShowImage(((ViewModels.ImageResultEntry)DataContext).L95xxCurrentImage, ((ViewModels.ImageResultEntry)DataContext).L95xxCurrentImageOverlay);
    }

    private bool ShowImage(ImageEntry image, DrawingImage overlay)
    {
        ImageViewerDialogViewModel dc = new();

        dc.LoadImage(image.Image, overlay);
        if (dc.Image == null) return false;

        var yourParentWindow = (Main.Views.MainWindow)Window.GetWindow(this);

        dc.Width = yourParentWindow.ActualWidth - 100;
        dc.Height = yourParentWindow.ActualHeight - 100;

        _ = DialogCoordinator.Instance.ShowMetroDialogAsync(yourParentWindow.DataContext, new ImageViewerDialogView() { DataContext = dc });

        return true;

    }
}
