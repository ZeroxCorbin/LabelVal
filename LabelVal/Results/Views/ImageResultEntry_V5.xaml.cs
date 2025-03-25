using LabelVal.Dialogs;
using LabelVal.ImageRolls.ViewModels;
using LabelVal.ImageViewer3D.Views;
using LabelVal.Sectors.Extensions;
using LabelVal.Sectors.Views;
using LibImageUtilities.ImageTypes;
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
    private ViewModels.ImageResultEntry _resultEntry => (ViewModels.ImageResultEntry)DataContext;
    public ImageResultEntry_V5() => InitializeComponent();

    private void btnCloseDetails_Click(object sender, RoutedEventArgs e)
    {
        var ire = (ViewModels.ImageResultEntry)DataContext;

        if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
        {
            if (ire.V275FocusedStoredSector != null)
            {
                ire.V275FocusedStoredSector.IsFocused = false;
                ire.V275FocusedStoredSector = null;
                App.Current.Dispatcher.BeginInvoke(() => ire.UpdateV275StoredImageOverlay());
            }
            if (ire.V275FocusedCurrentSector != null)
            {
                ire.V275FocusedCurrentSector.IsFocused = false;
                ire.V275FocusedCurrentSector = null;
                App.Current.Dispatcher.BeginInvoke(() => ire.UpdateV275CurrentImageOverlay());
            }
            if (ire.V5FocusedStoredSector != null)
            {
                ire.V5FocusedStoredSector.IsFocused = false;
                ire.V5FocusedStoredSector = null;
                App.Current.Dispatcher.BeginInvoke(() => ire.UpdateV5StoredImageOverlay());
            }
            if (ire.V5FocusedCurrentSector != null)
            {
                ire.V5FocusedCurrentSector.IsFocused = false;
                ire.V5FocusedCurrentSector = null;
                App.Current.Dispatcher.BeginInvoke(() => ire.UpdateV5CurrentImageOverlay());
            }
            if (ire.L95xxFocusedStoredSector != null)
            {
                ire.L95xxFocusedStoredSector.IsFocused = false;
                ire.L95xxFocusedStoredSector = null;
                App.Current.Dispatcher.BeginInvoke(() => ire.UpdateL95xxStoredImageOverlay());
            }
            if (ire.L95xxFocusedCurrentSector != null)
            {
                ire.L95xxFocusedCurrentSector.IsFocused = false;
                ire.L95xxFocusedCurrentSector = null;
                App.Current.Dispatcher.BeginInvoke(() => ire.UpdateL95xxCurrentImageOverlay());
            }
        }
        else
        {
            switch ((string)((Button)sender).Tag)
            {
                case "v5Stored":
                    if (ire.V5FocusedStoredSector != null)
                    {
                        ire.V5FocusedStoredSector.IsFocused = false;
                        ire.V5FocusedStoredSector = null;
                        App.Current.Dispatcher.BeginInvoke(() => ire.UpdateV5StoredImageOverlay());
                    }
                    break;
                case "v5Current":
                    if (ire.V5FocusedStoredSector != null)
                    {
                        ire.V5FocusedStoredSector.IsFocused = false;
                        ire.V5FocusedStoredSector = null;
                        App.Current.Dispatcher.BeginInvoke(() => ire.UpdateV5StoredImageOverlay());
                    }
                    if (ire.V5FocusedCurrentSector != null)
                    {
                        ire.V5FocusedCurrentSector.IsFocused = false;
                        ire.V5FocusedCurrentSector = null;
                        App.Current.Dispatcher.BeginInvoke(() => ire.UpdateV5CurrentImageOverlay());
                    }
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
                _resultEntry.ImageResults.FocusedTemplate = _resultEntry.V5ResultRow._Config;
                _resultEntry.ImageResults.FocusedReport = _resultEntry.V5ResultRow._Report;
            }
        }
        else
        {
            PopupSectorsDetails pop = new()
            {
                DataContext = ((ViewModels.ImageResultEntry)DataContext).V5StoredSectors
            };

            pop.Popup.PlacementTarget = ScrollV5StoredSectors;
            pop.Popup.Placement = System.Windows.Controls.Primitives.PlacementMode.Center;
            pop.Popup.IsOpen = true;
        }
    }
    private void V5CurrentSectorDetails_Click(object sender, RoutedEventArgs e)
    {
        if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
        {
            if (((ViewModels.ImageResultEntry)DataContext).V5CurrentReport != null)
            {
                _resultEntry.ImageResults.FocusedTemplate = _resultEntry.V5CurrentTemplate;
                _resultEntry.ImageResults.FocusedReport = _resultEntry.V5CurrentReport;
            }
        }
        else
        {
            PopupSectorsDetails pop = new()
            {
                DataContext = ((ViewModels.ImageResultEntry)DataContext).V5CurrentSectors
            };

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
        var parent = Utilities.VisualTreeHelp.GetVisualParent<DockPanel>((Button)sender, 2);
        var sectorDetails = Utilities.VisualTreeHelp.GetVisualChild<Sectors.Views.SectorDetails>(parent);

        if (sectorDetails != null)
        {
            string path;
            if ((path = Utilities.FileUtilities.SaveFileDialog($"{((Sectors.Interfaces.ISector)sectorDetails.DataContext).Template.Username}", "PNG|*.png", "Save sector details.")) != "")
            {
                try
                {
                    SaveToPng(sectorDetails, path);
                }
                catch { }
            }
        }
    }
    private void btnCopyImage_Click(object sender, RoutedEventArgs e)
    {
        var parent = Utilities.VisualTreeHelp.GetVisualParent<DockPanel>((Button)sender, 2);
        var sectorDetails = Utilities.VisualTreeHelp.GetVisualChild<Sectors.Views.SectorDetails>(parent);

        if (sectorDetails != null)
            CopyToClipboard(sectorDetails);
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

    private void V5StoredImage_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && !(Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)))
            _ = ShowImage(((ViewModels.ImageResultEntry)DataContext).V5StoredImage, ((ViewModels.ImageResultEntry)DataContext).V5StoredImageOverlay);
        else if (e.LeftButton == MouseButtonState.Pressed)
            Show3DImage(((ViewModels.ImageResultEntry)DataContext).V5StoredImage.ImageBytes);
    }
    private void V5CurrentImage_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && !(Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)))
            _ = ShowImage(((ViewModels.ImageResultEntry)DataContext).V5CurrentImage, ((ViewModels.ImageResultEntry)DataContext).V5CurrentImageOverlay);
        else if (e.LeftButton == MouseButtonState.Pressed)
            Show3DImage(((ViewModels.ImageResultEntry)DataContext).V5CurrentImage.ImageBytes);
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

    private void currentSectorMouseEnter(object sender, MouseEventArgs e)
    {
        if (sender is Sector sectorView)
        {
            if (sectorView.DataContext is Sectors.Interfaces.ISector sector)
                sector.IsMouseOver = true;

            if (this.DataContext is ViewModels.ImageResultEntry ire)
                App.Current.Dispatcher.BeginInvoke(() => ire.UpdateV5CurrentImageOverlay());
        }
    }
    private void currentSectorMouseLeave(object sender, MouseEventArgs e)
    {
        if (sender is Sector sectorView)
        {
            if (sectorView.DataContext is Sectors.Interfaces.ISector sector)
                sector.IsMouseOver = false;

            if (this.DataContext is ViewModels.ImageResultEntry ire)
                App.Current.Dispatcher.BeginInvoke(() => ire.UpdateV5CurrentImageOverlay());
        }
    }
    private void storedSectorMouseEnter(object sender, MouseEventArgs e)
    {
        if (sender is Sector sectorView)
        {
            if (sectorView.DataContext is Sectors.Interfaces.ISector sector)
                sector.IsMouseOver = true;

            if (this.DataContext is ViewModels.ImageResultEntry ire)
                App.Current.Dispatcher.BeginInvoke(() => ire.UpdateV5StoredImageOverlay());
        }
    }
    private void storedSectorMouseLeave(object sender, MouseEventArgs e)
    {
        if (sender is Sector sectorView)
        {
            if (sectorView.DataContext is Sectors.Interfaces.ISector sector)
                sector.IsMouseOver = false;

            if (this.DataContext is ViewModels.ImageResultEntry ire)
                App.Current.Dispatcher.BeginInvoke(() => ire.UpdateV5StoredImageOverlay());
        }
    }

    private void Show3DImage(byte[] image)
    {
        var img = new ImageViewer3D.ViewModels.ImageViewer3D_SingleMesh(image);

        var yourParentWindow = (Main.Views.MainWindow)Window.GetWindow(this);

        img.Width = yourParentWindow.ActualWidth - 100;
        img.Height = yourParentWindow.ActualHeight - 100;

        var tmp = new ImageViewer3DDialogView() { DataContext = img };
        tmp.Unloaded += (s, e) =>
        img.Dispose();
        _ = DialogCoordinator.Instance.ShowMetroDialogAsync(yourParentWindow.DataContext, tmp);

    }

    private void btnCopySectorsCsvToClipboard_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is System.Collections.ObjectModel.ObservableCollection<Sectors.Interfaces.ISector> sectors)
        {
            _ = sectors.GetSectorsReport(((ViewModels.ImageResultEntry)DataContext).SourceImage.Order.ToString(), true);
        }
        else if (sender is Button btn2 && btn2.Tag is ImageEntry image)
        {
            ImageToClipboard(image.ImageBytes);
        }
    }

    private void ImageToClipboard(byte[] imageBytes)
    {
        byte[] img;
        //If the shift key is pressed, copy the image as Bitmap.
        if (!(Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)))

            img = LibImageUtilities.ImageTypes.Png.Utilities.GetPng(imageBytes);
        else
        {
            var dpi = LibImageUtilities.ImageTypes.ImageUtilities.GetImageDPI(imageBytes);
            LibImageUtilities.ImageTypes.Bmp.Bmp format = new(LibImageUtilities.ImageTypes.Bmp.Utilities.GetBmp(imageBytes));
            //Lvs95xx.lib.Core.Controllers.Controller.ApplyWatermark(format.ImageData);

            img = format.RawData;

            _ = LibImageUtilities.ImageTypes.ImageUtilities.SetImageDPI(img, dpi);
        }

        Clipboard.SetImage(LibImageUtilities.BitmapImage.CreateBitmapImage(img));
    }
}
