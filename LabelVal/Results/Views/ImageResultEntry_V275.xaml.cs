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
/// Interaction logic for ImageResultEntry_V275.xaml
/// </summary>
public partial class ImageResultEntry_V275 : UserControl
{
    public ImageResultEntry_V275() => InitializeComponent();

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
                case "v275Stored":
                    ((ViewModels.ImageResultEntry)DataContext).V275FocusedStoredSector = null;
                    break;
                case "v275Current":
                    ((ViewModels.ImageResultEntry)DataContext).V275FocusedCurrentSector = null;
                    ((ViewModels.ImageResultEntry)DataContext).V275FocusedStoredSector = null;
                    break;
            }
        }
    }

    private void V275StoredSectors_Click(object sender, RoutedEventArgs e)
    {
        if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
        {
            if (((ViewModels.ImageResultEntry)DataContext).V275ResultRow != null)
            {
                PopupJSONViewer pop = new();
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
            PopupSectorsDetails pop = new()
            {
                DataContext = ((ViewModels.ImageResultEntry)DataContext).V275StoredSectors
            };

            pop.Popup.PlacementTarget = (Button)sender;
            pop.Popup.IsOpen = true;
        }
    }
    private void V275CurrentSectorDetails_Click(object sender, RoutedEventArgs e)
    {
        if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
        {
            PopupJSONViewer pop = new();
            pop.Viewer1.JSON = ((ViewModels.ImageResultEntry)DataContext).V275CurrentTemplate;
            pop.Viewer1.Title = "Template";
            pop.Viewer2.JSON = ((ViewModels.ImageResultEntry)DataContext).V275CurrentReport;
            pop.Viewer2.Title = "Report";

            pop.Popup.PlacementTarget = (Button)sender;
            pop.Popup.IsOpen = true;
        }
        else
        {
            PopupSectorsDetails pop = new()
            {
                DataContext = ((ViewModels.ImageResultEntry)DataContext).V275CurrentSectors
            };

            pop.Popup.PlacementTarget = (Button)sender;
            pop.Popup.IsOpen = true;
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

    private void btnSaveImage_Click(object sender, RoutedEventArgs e)
    {
        Sector sect = Utilities.VisualTreeHelp.GetVisualParent<Sector>((Button)sender);

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
        Sector sect = Utilities.VisualTreeHelp.GetVisualParent<Sector>((Button)sender);

        if (sect != null)
            CopyToClipboard(sect);

    }
    public void SaveToPng(FrameworkElement visual, string fileName)
    {
        PngBitmapEncoder encoder = new();
        EncodeVisual(visual, encoder);

        using System.IO.FileStream stream = System.IO.File.Create(fileName);
        encoder.Save(stream);
    }
    public void CopyToClipboard(FrameworkElement visual)
    {
        PngBitmapEncoder encoder = new();
        EncodeVisual(visual, encoder);

        using System.IO.MemoryStream stream = new();
        encoder.Save(stream);
        stream.Seek(0, System.IO.SeekOrigin.Begin);
        BitmapImage bitmapImage = new();
        bitmapImage.BeginInit();
        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
        bitmapImage.StreamSource = stream;
        bitmapImage.EndInit();
        Clipboard.SetImage(bitmapImage);
    }
    private static void EncodeVisual(FrameworkElement visual, BitmapEncoder encoder)
    {
        RenderTargetBitmap bitmap = new((int)visual.ActualWidth, (int)visual.ActualHeight, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        BitmapFrame frame = BitmapFrame.Create(bitmap);
        encoder.Frames.Add(frame);
    }

    private void lstDissimilarSector_Click(object sender, MouseButtonEventArgs e)
    {
        SectorDifferences sndr = (SectorDifferences)sender;
        //var ire = Utilities.VisualTreeHelp.GetVisualParent<ImageResultEntry_V275>(sndr);
        //if (ire != null)
        //{
        System.Collections.ObjectModel.Collection<Sector> sectors = Utilities.VisualTreeHelp.GetVisualChildren<Sector>(this);
        foreach (Sector s in sectors)
        {
            if (s.SectorName == ((Sectors.Interfaces.ISectorDifferences)sndr.DataContext).UserName)
                s.ShowSectorDetails();
        }

        //}
    }


    private void V275StoredImage_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            _ = ShowImage(((ViewModels.ImageResultEntry)DataContext).V275StoredImage, ((ViewModels.ImageResultEntry)DataContext).V275StoredImageOverlay);
    }
    private void V275CurrentImage_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            _ = ShowImage(((ViewModels.ImageResultEntry)DataContext).V275CurrentImage, ((ViewModels.ImageResultEntry)DataContext).V275CurrentImageOverlay);
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
