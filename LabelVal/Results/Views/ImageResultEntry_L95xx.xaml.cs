using LabelVal.Dialogs;
using LabelVal.ImageRolls.ViewModels;
using LabelVal.ImageViewer3D.Views;
using LabelVal.Sectors.Views;
using MahApps.Metro.Controls.Dialogs;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SectorDifferences = LabelVal.Sectors.Views.SectorDifferences;

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
        var ire = (ViewModels.ImageResultEntry)DataContext;

        if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
        {
            if(ire.V275FocusedStoredSector != null)
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
                case "l95xxStored":
                    if (ire.L95xxFocusedStoredSector != null)
                    {
                        ire.L95xxFocusedStoredSector.IsFocused = false;
                        ire.L95xxFocusedStoredSector = null;
                        App.Current.Dispatcher.BeginInvoke(() => ire.UpdateL95xxStoredImageOverlay());
                    }
                    break;
                case "l95xxCurrent":
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
            if (s.SectorName == ((Sectors.Classes.SectorDifferences)sndr.DataContext).Username)
                s.ShowSectorDetails();
        }
    }

    private void L95xxStoredImage_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && !(Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)))
            _ = ShowImage(((ViewModels.ImageResultEntry)DataContext).L95xxStoredImage, ((ViewModels.ImageResultEntry)DataContext).L95xxStoredImageOverlay);
        else if (e.LeftButton == MouseButtonState.Pressed)
            Show3DImage(((ViewModels.ImageResultEntry)DataContext).L95xxStoredImage.ImageBytes);
    }
    private void L95xxCurrentImage_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && !(Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)))
            _ = ShowImage(((ViewModels.ImageResultEntry)DataContext).L95xxCurrentImage, ((ViewModels.ImageResultEntry)DataContext).L95xxCurrentImageOverlay);
        else if (e.LeftButton == MouseButtonState.Pressed)
            Show3DImage(((ViewModels.ImageResultEntry)DataContext).L95xxCurrentImage.ImageBytes);
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
                App.Current.Dispatcher.BeginInvoke(() => ire.UpdateL95xxCurrentImageOverlay());
        }
    }
    private void currentSectorMouseLeave(object sender, MouseEventArgs e)
    {
        if (sender is Sector sectorView)
        {
            if (sectorView.DataContext is Sectors.Interfaces.ISector sector)
                sector.IsMouseOver = false;

            if (this.DataContext is ViewModels.ImageResultEntry ire)
                App.Current.Dispatcher.BeginInvoke(() => ire.UpdateL95xxCurrentImageOverlay());
        }
    }
    private void storedSectorMouseEnter(object sender, MouseEventArgs e)
    {
        if (sender is Sector sectorView)
        {
            if (sectorView.DataContext is Sectors.Interfaces.ISector sector)
                sector.IsMouseOver = true;

            if (this.DataContext is ViewModels.ImageResultEntry ire)
                App.Current.Dispatcher.BeginInvoke(() => ire.UpdateL95xxStoredImageOverlay());
        }
    }
    private void storedSectorMouseLeave(object sender, MouseEventArgs e)
    {
        if (sender is Sector sectorView)
        {
            if (sectorView.DataContext is Sectors.Interfaces.ISector sector)
                sector.IsMouseOver = false;

            if (this.DataContext is ViewModels.ImageResultEntry ire)
                App.Current.Dispatcher.BeginInvoke(() => ire.UpdateL95xxStoredImageOverlay());
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
}
