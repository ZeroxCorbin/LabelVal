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
/// Interaction logic for ImageResultEntry_V275.xaml
/// </summary>
public partial class ImageResultEntry_V275 : UserControl
{
    public ImageResultEntry_V275() => InitializeComponent();

    private void btnCloseDetails_Click(object sender, RoutedEventArgs e)
    {
        ViewModels.ImageResultEntry ire = (ViewModels.ImageResultEntry)DataContext;

        if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
        {
            if (ire.V275FocusedStoredSector != null)
            {
                ire.V275FocusedStoredSector.IsFocused = false;
                ire.V275FocusedStoredSector = null;
                _ = App.Current.Dispatcher.BeginInvoke(() => ire.UpdateV275StoredImageOverlay());
            }
            if (ire.V275FocusedCurrentSector != null)
            {
                ire.V275FocusedCurrentSector.IsFocused = false;
                ire.V275FocusedCurrentSector = null;
                _ = App.Current.Dispatcher.BeginInvoke(() => ire.UpdateV275CurrentImageOverlay());
            }
            if (ire.V5FocusedStoredSector != null)
            {
                ire.V5FocusedStoredSector.IsFocused = false;
                ire.V5FocusedStoredSector = null;
                _ = App.Current.Dispatcher.BeginInvoke(() => ire.UpdateV5StoredImageOverlay());
            }
            if (ire.V5FocusedCurrentSector != null)
            {
                ire.V5FocusedCurrentSector.IsFocused = false;
                ire.V5FocusedCurrentSector = null;
                _ = App.Current.Dispatcher.BeginInvoke(() => ire.UpdateV5CurrentImageOverlay());
            }
            if (ire.L95xxFocusedStoredSector != null)
            {
                ire.L95xxFocusedStoredSector.IsFocused = false;
                ire.L95xxFocusedStoredSector = null;
                _ = App.Current.Dispatcher.BeginInvoke(() => ire.UpdateL95xxStoredImageOverlay());
            }
            if (ire.L95xxFocusedCurrentSector != null)
            {
                ire.L95xxFocusedCurrentSector.IsFocused = false;
                ire.L95xxFocusedCurrentSector = null;
                _ = App.Current.Dispatcher.BeginInvoke(() => ire.UpdateL95xxCurrentImageOverlay());
            }
        }
        else
        {
            switch ((string)((Button)sender).Tag)
            {
                case "v275Stored":
                    if (ire.V275FocusedStoredSector != null)
                    {
                        ire.V275FocusedStoredSector.IsFocused = false;
                        ire.V275FocusedStoredSector = null;
                        _ = App.Current.Dispatcher.BeginInvoke(() => ire.UpdateV275StoredImageOverlay());
                    }
                    break;
                case "v275Current":
                    if (ire.V275FocusedStoredSector != null)
                    {
                        ire.V275FocusedStoredSector.IsFocused = false;
                        ire.V275FocusedStoredSector = null;
                        _ = App.Current.Dispatcher.BeginInvoke(() => ire.UpdateV275StoredImageOverlay());
                    }
                    if (ire.V275FocusedCurrentSector != null)
                    {
                        ire.V275FocusedCurrentSector.IsFocused = false;
                        ire.V275FocusedCurrentSector = null;
                        _ = App.Current.Dispatcher.BeginInvoke(() => ire.UpdateV275CurrentImageOverlay());
                    }
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

                pop.Popup.PlacementTarget = ScrollV275StoredSectors;
                pop.Popup.IsOpen = true;
            }
        }
        else
        {
            PopupSectorsDetails pop = new()
            {
                DataContext = ((ViewModels.ImageResultEntry)DataContext).V275StoredSectors
            };

            pop.Popup.PlacementTarget = ScrollV275StoredSectors;
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
            if (s.SectorName == ((Sectors.Interfaces.ISectorParameters)sndr.DataContext).Sector.Template.Username)
                s.ShowSectorDetails();
        }

        //}
    }

    private void V275StoredImage_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && !(Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)))
            _ = ShowImage(((ViewModels.ImageResultEntry)DataContext).V275StoredImage, ((ViewModels.ImageResultEntry)DataContext).V275StoredImageOverlay);
        else if (e.LeftButton == MouseButtonState.Pressed)
            Show3DImage(((ViewModels.ImageResultEntry)DataContext).V275StoredImage.ImageBytes);
    }
    private void V275CurrentImage_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && !(Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)))
            _ = ShowImage(((ViewModels.ImageResultEntry)DataContext).V275CurrentImage, ((ViewModels.ImageResultEntry)DataContext).V275CurrentImageOverlay);
        else if (e.LeftButton == MouseButtonState.Pressed)
            Show3DImage(((ViewModels.ImageResultEntry)DataContext).V275CurrentImage.ImageBytes);
    }

    private bool ShowImage(ImageEntry image, DrawingImage overlay)
    {
        ImageViewerDialogViewModel dc = new();

        dc.LoadImage(image.Image, overlay);
        if (dc.Image == null) return false;

        Main.Views.MainWindow yourParentWindow = (Main.Views.MainWindow)Window.GetWindow(this);

        dc.Width = yourParentWindow.ActualWidth - 100;
        dc.Height = yourParentWindow.ActualHeight - 100;

        _ = DialogCoordinator.Instance.ShowMetroDialogAsync(yourParentWindow.DataContext, new ImageViewerDialogView() { DataContext = dc });

        return true;

    }

    private void lstStoredSectorClick(object sender, MouseButtonEventArgs e) => e.Handled = true;

    private void currentSectorMouseEnter(object sender, MouseEventArgs e)
    {
        if (sender is Sector sectorView)
        {
            if (sectorView.DataContext is Sectors.Interfaces.ISector sector)
                sector.IsMouseOver = true;

            if (DataContext is ViewModels.ImageResultEntry ire)
                _ = App.Current.Dispatcher.BeginInvoke(() => ire.UpdateV275CurrentImageOverlay());
        }
    }
    private void currentSectorMouseLeave(object sender, MouseEventArgs e)
    {
        if (sender is Sector sectorView)
        {
            if (sectorView.DataContext is Sectors.Interfaces.ISector sector)
                sector.IsMouseOver = false;

            if (DataContext is ViewModels.ImageResultEntry ire)
                _ = App.Current.Dispatcher.BeginInvoke(() => ire.UpdateV275CurrentImageOverlay());
        }
    }
    private void storedSectorMouseEnter(object sender, MouseEventArgs e)
    {
        if (sender is Sector sectorView)
        {
            if (sectorView.DataContext is Sectors.Interfaces.ISector sector)
                sector.IsMouseOver = true;

            if (DataContext is ViewModels.ImageResultEntry ire)
                _ = App.Current.Dispatcher.BeginInvoke(() => ire.UpdateV275StoredImageOverlay());
        }
    }
    private void storedSectorMouseLeave(object sender, MouseEventArgs e)
    {
        if (sender is Sector sectorView)
        {
            if (sectorView.DataContext is Sectors.Interfaces.ISector sector)
                sector.IsMouseOver = false;

            if (DataContext is ViewModels.ImageResultEntry ire)
                _ = App.Current.Dispatcher.BeginInvoke(() => ire.UpdateV275StoredImageOverlay());
        }
    }

    private void Show3DImage(byte[] image)
    {
        ImageViewer3D.ViewModels.ImageViewer3D_SingleMesh img = new(image);

        Main.Views.MainWindow yourParentWindow = (Main.Views.MainWindow)Window.GetWindow(this);

        img.Width = yourParentWindow.ActualWidth - 100;
        img.Height = yourParentWindow.ActualHeight - 100;

        ImageViewer3DDialogView tmp = new() { DataContext = img };
        tmp.Unloaded += (s, e) =>
        img.Dispose();
        _ = DialogCoordinator.Instance.ShowMetroDialogAsync(yourParentWindow.DataContext, tmp);

    }

    private void btnCopySectorsCsvToClipboard_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is System.Collections.ObjectModel.ObservableCollection<Sectors.Interfaces.ISector> sectors)
        {
            _ = sectors.GetSectorsCSV(true);
        }
        else if (sender is Button btn2 && btn2.Tag is ImageEntry image)
        {
            ImageToClipboard(image.ImageBytes);
        }
    }

    private void ImageToClipboard(byte[] imageBytes)
    {
        if (!(Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)))
        {
            Clipboard.SetImage(LibImageUtilities.BitmapImage.CreateBitmapImage(LibImageUtilities.ImageTypes.Png.Utilities.GetPng(imageBytes)));
        }
        else
        {
            ImageUtilities.DPI dpi = LibImageUtilities.ImageTypes.ImageUtilities.GetImageDPI(imageBytes);
            LibImageUtilities.ImageTypes.Bmp.Bmp format = new(LibImageUtilities.ImageTypes.Bmp.Utilities.GetBmp(imageBytes));
            //Lvs95xx.lib.Core.Controllers.Controller.ApplyWatermark(format.ImageData);
            byte[] img = format.RawData;
            _ = LibImageUtilities.ImageTypes.ImageUtilities.SetImageDPI(img, dpi);
            Clipboard.SetImage(LibImageUtilities.BitmapImage.CreateBitmapImage(img));
        }

        //var data = new DataObject();
        ////If the shift key is pressed, copy the image as Bitmap.
        //if (!(Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)))
        //{
        //    using MemoryStream pngMemStream = new MemoryStream(LibImageUtilities.ImageTypes.Png.Utilities.GetPng(imageBytes));
        //    data.SetData("PNG", pngMemStream, false);
        //    Clipboard.SetDataObject(data, true);
        //}

        //else
        //{
        //    ImageUtilities.DPI dpi = LibImageUtilities.ImageTypes.ImageUtilities.GetImageDPI(imageBytes);
        //    LibImageUtilities.ImageTypes.Bmp.Bmp format = new(LibImageUtilities.ImageTypes.Bmp.Utilities.GetBmp(imageBytes));
        //    //Lvs95xx.lib.Core.Controllers.Controller.ApplyWatermark(format.ImageData);

        //    var img = format.RawData;

        //    _ = LibImageUtilities.ImageTypes.ImageUtilities.SetImageDPI(img, dpi);

        //    data.SetData(DataFormats.Bitmap,img, false);
        //    Clipboard.SetDataObject(data, true);
        //}

    }
}
