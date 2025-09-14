using CommunityToolkit.Mvvm.Input;
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
public partial class ResultsDeviceEntry_V275 : UserControl
{
    private ViewModels.IResultsDeviceEntry _viewModel;

    public ResultsDeviceEntry_V275()
    {
        InitializeComponent();

        DataContextChanged += (e, s) => _viewModel = (ViewModels.IResultsDeviceEntry)DataContext;
    }

    #region Command Execute Methods (moved from toolbar Click handlers)

    [RelayCommand]
    private void CopyToClipboard(object param)
    {
        if (param is System.Collections.ObjectModel.ObservableCollection<Sectors.Interfaces.ISector> sectors)
        {
            if (Sectors.Output.SectorOutputSettings.CurrentOutputType == Sectors.Output.SectorOutputType.Delimited)
                Clipboard.SetText(sectors.GetDelimetedSectorsReport($"{_viewModel.ResultsManagerView.ActiveImageRoll.Name}{(char)Sectors.Output.SectorOutputSettings.CurrentDelimiter}{_viewModel.ResultsEntry.SourceImage.Order}"));
            else if (Sectors.Output.SectorOutputSettings.CurrentOutputType == Sectors.Output.SectorOutputType.JSON)
                Clipboard.SetText(sectors.GetJsonSectorsReport($"{_viewModel.ResultsManagerView.ActiveImageRoll.Name}{(char)Sectors.Output.SectorOutputSettings.CurrentDelimiter}{_viewModel.ResultsEntry.SourceImage.Order}").ToString());
        }
        else if (param is ImageEntry image)
        {
            Clipboard.SetImage(image.Image);
        }
    }

    [RelayCommand]
    private void ShowStoredSectors(object param)
    {
        if (param is string s && s.Equals("json"))
        {
            if (_viewModel.Result != null)
                _viewModel.ResultsManagerView.ShowSectorsDetailsWindow(_viewModel.Result.Template, _viewModel.Result.Report);
        }
        else
        {
            _viewModel.ResultsManagerView.ShowSectorsDetailsWindow(_viewModel.StoredSectors);
        }
    }

    [RelayCommand]
    private void ShowCurrentSectors(object param)
    {
        if (param is string s && s.Equals("json"))
        {
            _viewModel.ResultsManagerView.ShowSectorsDetailsWindow(_viewModel.CurrentTemplate, _viewModel.CurrentReport);
        }
        else
        {
            _viewModel.ResultsManagerView.ShowSectorsDetailsWindow(_viewModel.CurrentSectors);
        }
    }
    #endregion

    // Existing event handlers below (still used by other buttons in main XAML):

    private void btnCloseDetails_Click(object sender, RoutedEventArgs e)
    {
        if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
        {
            foreach (ViewModels.IResultsDeviceEntry device in _viewModel.ResultsEntry.ResultsDeviceEntries)
            {
                if (device.FocusedCurrentSector != null)
                    device.FocusedCurrentSector.IsFocused = false;
                device.FocusedCurrentSector = null;

                if (device.FocusedStoredSector != null)
                    device.FocusedStoredSector.IsFocused = false;
                device.FocusedStoredSector = null;

                _ = Application.Current.Dispatcher.BeginInvoke(device.RefreshCurrentOverlay);
                _ = Application.Current.Dispatcher.BeginInvoke(device.RefreshStoredOverlay);
            }
        }
        else
        {
            switch ((string)((Button)sender).Tag)
            {
                case "Stored":
                    foreach (ViewModels.IResultsDeviceEntry device in _viewModel.ResultsEntry.ResultsDeviceEntries.Where(x => x.Device == _viewModel.Device))
                    {
                        if (device.FocusedStoredSector != null)
                            device.FocusedStoredSector.IsFocused = false;
                        device.FocusedStoredSector = null;
                        _ = Application.Current.Dispatcher.BeginInvoke(device.RefreshStoredOverlay);
                    }
                    break;
                case "Current":
                    foreach (ViewModels.IResultsDeviceEntry device in _viewModel.ResultsEntry.ResultsDeviceEntries.Where(x => x.Device == _viewModel.Device))
                    {
                        if (device.FocusedCurrentSector != null)
                            device.FocusedCurrentSector.IsFocused = false;
                        device.FocusedCurrentSector = null;

                        if (device.FocusedStoredSector != null)
                            device.FocusedStoredSector.IsFocused = false;
                        device.FocusedStoredSector = null;

                        _ = Application.Current.Dispatcher.BeginInvoke(device.RefreshCurrentOverlay);
                        _ = Application.Current.Dispatcher.BeginInvoke(device.RefreshStoredOverlay);
                    }
                    break;
            }
        }
    }

    // (StoredSectors_Click / CurrentSectors_Click / btnCopyToClipboard_Click replaced by commands.)

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

    private void StoredSectors_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (e.Handled) return;
        e.Handled = ScrollStoredSectors.ComputedVerticalScrollBarVisibility == Visibility.Visible;
        ScrollStoredSectors.ScrollToVerticalOffset(ScrollStoredSectors.VerticalOffset - e.Delta);
    }
    private void CurrentSectors_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (e.Handled) return;
        e.Handled = ScrollCurrentSectors.ComputedVerticalScrollBarVisibility == Visibility.Visible;
        ScrollCurrentSectors.ScrollToVerticalOffset(ScrollCurrentSectors.VerticalOffset - e.Delta);
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
                try { SaveToPng(sectorDetails, path); } catch { }
            }
        }
    }
    private void btnCopyImage_Click(object sender, RoutedEventArgs e)
    {
        DockPanel parent = Utilities.VisualTreeHelp.GetVisualParent<DockPanel>((Button)sender, 2);
        SectorDetails sectorDetails = Utilities.VisualTreeHelp.GetVisualChild<Sectors.Views.SectorDetails>(parent);
        if (sectorDetails != null) CopyToClipboard(sectorDetails);
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
            _ = ShowImage(_viewModel.StoredImage, _viewModel.StoredImageOverlay);
        else if (e.LeftButton == MouseButtonState.Pressed)
            Show3DImage(_viewModel.StoredImage.ImageBytes);
    }
    private void CurrentImage_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && !(Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)))
            _ = ShowImage(_viewModel.CurrentImage, _viewModel.CurrentImageOverlay);
        else if (e.LeftButton == MouseButtonState.Pressed)
            Show3DImage(_viewModel.CurrentImage.ImageBytes);
    }

    private bool ShowImage(ImageEntry image, DrawingImage overlay)
    {
        ImageViewerDialogViewModel dc = new();
        dc.LoadImage(image.Image, overlay);
        if (dc.Image == null) return false;

        var parentWindow = (Main.Views.MainWindow)Window.GetWindow(this);
        dc.Width = parentWindow.ActualWidth - 100;
        dc.Height = parentWindow.ActualHeight - 100;
        _ = DialogCoordinator.Instance.ShowMetroDialogAsync(parentWindow.DataContext, new ImageViewerDialogView { DataContext = dc });
        return true;
    }

    private void currentSectorMouseEnter(object sender, MouseEventArgs e)
    {
        if (sender is Sector sectorView && sectorView.DataContext is Sectors.Interfaces.ISector sector)
        {
            sector.IsMouseOver = true;
            _ = Application.Current.Dispatcher.BeginInvoke(() => _viewModel.RefreshCurrentOverlay());
        }
    }
    private void currentSectorMouseLeave(object sender, MouseEventArgs e)
    {
        if (sender is Sector sectorView && sectorView.DataContext is Sectors.Interfaces.ISector sector)
            sector.IsMouseOver = false;
        _ = Application.Current.Dispatcher.BeginInvoke(() => _viewModel.RefreshCurrentOverlay());
    }
    private void storedSectorMouseEnter(object sender, MouseEventArgs e)
    {
        if (sender is Sector sectorView && sectorView.DataContext is Sectors.Interfaces.ISector sector)
            sector.IsMouseOver = true;
        _ = Application.Current.Dispatcher.BeginInvoke(() => _viewModel.RefreshStoredOverlay());
    }
    private void storedSectorMouseLeave(object sender, MouseEventArgs e)
    {
        if (sender is Sector sectorView && sectorView.DataContext is Sectors.Interfaces.ISector sector)
            sector.IsMouseOver = false;
        _ = Application.Current.Dispatcher.BeginInvoke(() => _viewModel.RefreshStoredOverlay());
    }

    [RelayCommand]
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

    private void Show3DViewerCurrent(object sender, RoutedEventArgs e) => Show3DImage(_viewModel.CurrentImage.ImageBytes);
    private void Show3DViewerStored(object sender, RoutedEventArgs e) => Show3DImage(_viewModel.StoredImage.ImageBytes);
    private void Show2DViewerStored(object sender, RoutedEventArgs e) => _ = ShowImage(_viewModel.StoredImage, _viewModel.StoredImageOverlay);
    private void Show2DViewerCurrent(object sender, RoutedEventArgs e) => _ = ShowImage(_viewModel.CurrentImage, _viewModel.CurrentImageOverlay);
}