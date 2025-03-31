using LabelVal.Dialogs;
using LabelVal.ImageRolls.ViewModels;
using LabelVal.ImageViewer3D.Views;
using LabelVal.Sectors.Extensions;
using LabelVal.Sectors.Views;
using MahApps.Metro.Controls.Dialogs;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LabelVal.Results.Views;
/// <summary>
/// Interaction logic for (ImageResultDeviceEntry_V275.xaml
/// </summary>
public partial class ImageResultDeviceEntry_V275 : UserControl
{
    private ViewModels.IImageResultDeviceEntry _viewModel;
    public ImageResultDeviceEntry_V275()
    {
        InitializeComponent();

        DataContextChanged += (e, s) => _viewModel = (ViewModels.IImageResultDeviceEntry)DataContext;
    }

    private void btnCloseDetails_Click(object sender, RoutedEventArgs e)
    {
        var ire = (ViewModels.ImageResultEntry)DataContext;

        //if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
        //{
        //    if (ire.V275FocusedStoredSector != null)
        //    {
        //        ire.V275FocusedStoredSector.IsFocused = false;
        //        ire.V275FocusedStoredSector = null;
        //        _ = App.Current.Dispatcher.BeginInvoke(() => ire.UpdateV275StoredImageOverlay());
        //    }
        //    if (ire.V275FocusedCurrentSector != null)
        //    {
        //        ire.V275FocusedCurrentSector.IsFocused = false;
        //        ire.V275FocusedCurrentSector = null;
        //        _ = App.Current.Dispatcher.BeginInvoke(() => ire.UpdateV275CurrentImageOverlay());
        //    }
        //    if (ire.V5FocusedStoredSector != null)
        //    {
        //        ire.V5FocusedStoredSector.IsFocused = false;
        //        ire.V5FocusedStoredSector = null;
        //        _ = App.Current.Dispatcher.BeginInvoke(() => ire.UpdateV5StoredImageOverlay());
        //    }
        //    if (ire.V5FocusedCurrentSector != null)
        //    {
        //        ire.V5FocusedCurrentSector.IsFocused = false;
        //        ire.V5FocusedCurrentSector = null;
        //        _ = App.Current.Dispatcher.BeginInvoke(() => ire.UpdateV5CurrentImageOverlay());
        //    }
        //    if (ire.L95xxFocusedStoredSector != null)
        //    {
        //        ire.L95xxFocusedStoredSector.IsFocused = false;
        //        ire.L95xxFocusedStoredSector = null;
        //        _ = App.Current.Dispatcher.BeginInvoke(() => ire.UpdateL95xxStoredImageOverlay());
        //    }
        //    if (ire.L95xxFocusedCurrentSector != null)
        //    {
        //        ire.L95xxFocusedCurrentSector.IsFocused = false;
        //        ire.L95xxFocusedCurrentSector = null;
        //        _ = App.Current.Dispatcher.BeginInvoke(() => ire.UpdateL95xxCurrentImageOverlay());
        //    }
        //}
        //else
        //{
        //    switch ((string)((Button)sender).Tag)
        //    {
        //        case "v275Stored":
        //            if (ire.V275FocusedStoredSector != null)
        //            {
        //                ire.V275FocusedStoredSector.IsFocused = false;
        //                ire.V275FocusedStoredSector = null;
        //                _ = App.Current.Dispatcher.BeginInvoke(() => ire.UpdateV275StoredImageOverlay());
        //            }
        //            break;
        //        case "v275Current":
        //            if (ire.V275FocusedStoredSector != null)
        //            {
        //                ire.V275FocusedStoredSector.IsFocused = false;
        //                ire.V275FocusedStoredSector = null;
        //                _ = App.Current.Dispatcher.BeginInvoke(() => ire.UpdateV275StoredImageOverlay());
        //            }
        //            if (ire.V275FocusedCurrentSector != null)
        //            {
        //                ire.V275FocusedCurrentSector.IsFocused = false;
        //                ire.V275FocusedCurrentSector = null;
        //                _ = App.Current.Dispatcher.BeginInvoke(() => ire.UpdateV275CurrentImageOverlay());
        //            }
        //            break;
        //    }
        //}
    }
    private void StoredSectors_Click(object sender, RoutedEventArgs e)
    {
        if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
        {
            if (_viewModel.Result != null)
            {
                _viewModel.ImageResultsManager.FocusedTemplate = _viewModel.Result.Template;
                _viewModel.ImageResultsManager.FocusedReport = _viewModel.Result.Report;
            }
        }
        else
        {
            PopupSectorsDetails pop = new()
            {
                DataContext = _viewModel.StoredSectors
            };

            pop.Popup.PlacementTarget = ScrollStoredSectors;
            pop.Popup.Placement = System.Windows.Controls.Primitives.PlacementMode.Center;
            pop.Popup.IsOpen = true;
        }
    }
    private void CurrentSectorDetails_Click(object sender, RoutedEventArgs e)
    {
        if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
        {

            if (_viewModel.Result != null)
            {
                _viewModel.ImageResultsManager.FocusedTemplate = _viewModel.CurrentTemplate;
                _viewModel.ImageResultsManager.FocusedReport = _viewModel.CurrentReport;
            }
        }
        else
        {
            PopupSectorsDetails pop = new()
            {
                DataContext = _viewModel.CurrentSectors
            };

            pop.Popup.PlacementTarget = (Button)sender;
            pop.Popup.IsOpen = true;
        }
    }
    private void ScrollStoredSectors_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (e.VerticalChange != 0)
            ScrollCurrentSectors.ScrollToVerticalOffset(e.VerticalOffset);
    }
    private void ScrollCurrentSectors_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (e.VerticalChange != 0)
            ScrollStoredSectors.ScrollToVerticalOffset(e.VerticalOffset);
    }

    private void btnSaveImage_Click(object sender, RoutedEventArgs e)
    {
        DockPanel parent = Utilities.VisualTreeHelp.GetVisualParent<DockPanel>((Button)sender, 2);
        SectorDetails sectorDetails = Utilities.VisualTreeHelp.GetVisualChild<Sectors.Views.SectorDetails>(parent);

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
        DockPanel parent = Utilities.VisualTreeHelp.GetVisualParent<DockPanel>((Button)sender, 2);
        SectorDetails sectorDetails = Utilities.VisualTreeHelp.GetVisualChild<Sectors.Views.SectorDetails>(parent);

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
        _ = stream.Seek(0, System.IO.SeekOrigin.Begin);
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
        var frame = BitmapFrame.Create(bitmap);
        encoder.Frames.Add(frame);
    }

    private void StoredImage_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && !(Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)))
            _ = ShowImage(((ViewModels.IImageResultDeviceEntry)DataContext).StoredImage, ((ViewModels.IImageResultDeviceEntry)DataContext).StoredImageOverlay);
        else if (e.LeftButton == MouseButtonState.Pressed)
            Show3DImage(((ViewModels.IImageResultDeviceEntry)DataContext).StoredImage.ImageBytes);
    }
    private void CurrentImage_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && !(Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)))
            _ = ShowImage(((ViewModels.IImageResultDeviceEntry)DataContext).CurrentImage, ((ViewModels.IImageResultDeviceEntry)DataContext).CurrentImageOverlay);
        else if (e.LeftButton == MouseButtonState.Pressed)
            Show3DImage(((ViewModels.IImageResultDeviceEntry)DataContext).CurrentImage.ImageBytes);
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

            if (DataContext is ViewModels.IImageResultDeviceEntry ire)
                _ = App.Current.Dispatcher.BeginInvoke(() => ire.RefreshCurrentOverlay());
        }
    }
    private void currentSectorMouseLeave(object sender, MouseEventArgs e)
    {
        if (sender is Sector sectorView)
        {
            if (sectorView.DataContext is Sectors.Interfaces.ISector sector)
                sector.IsMouseOver = false;

            if (DataContext is ViewModels.IImageResultDeviceEntry ire)
                _ = App.Current.Dispatcher.BeginInvoke(() => ire.RefreshCurrentOverlay());
        }
    }
    private void storedSectorMouseEnter(object sender, MouseEventArgs e)
    {
        if (sender is Sector sectorView)
        {
            if (sectorView.DataContext is Sectors.Interfaces.ISector sector)
                sector.IsMouseOver = true;

            if (DataContext is ViewModels.IImageResultDeviceEntry ire)
                _ = App.Current.Dispatcher.BeginInvoke(() => ire.RefreshStoredOverlay());
        }
    }
    private void storedSectorMouseLeave(object sender, MouseEventArgs e)
    {
        if (sender is Sector sectorView)
        {
            if (sectorView.DataContext is Sectors.Interfaces.ISector sector)
                sector.IsMouseOver = false;

            if (DataContext is ViewModels.IImageResultDeviceEntry ire)
                _ = App.Current.Dispatcher.BeginInvoke(() => ire.RefreshStoredOverlay());
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
            var img = (ViewModels.ImageResultEntry)DataContext;
            _ = sectors.GetSectorsReport($"{img.ImageResultsManager.SelectedImageRoll.Name}{(char)Sectors.Classes.SectorOutputSettings.CurrentDelimiter}{img.SourceImage.Order}", true);
        }
        else if (sender is Button btn2 && btn2.Tag is ImageEntry image)
        {
            ImageToClipboard(image.ImageBytes);
        }
    }

    private void ImageToClipboard(byte[] imageBytes)
    {
        using var img = new ImageMagick.MagickImage(imageBytes);
        //If the shift key is pressed, copy the image as Bitmap.
        if (!(Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)))
            Clipboard.SetImage(ImageUtilities.lib.Wpf.BitmapImage.CreateBitmapImage(img.ToByteArray(ImageMagick.MagickFormat.Bmp3)));
        else
            Clipboard.SetImage(ImageUtilities.lib.Wpf.BitmapImage.CreateBitmapImage(img.ToByteArray(ImageMagick.MagickFormat.Png)));
    }
}
