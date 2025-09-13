using LabelVal.Dialogs;
using LabelVal.ImageRolls.ViewModels;
using LabelVal.ImageViewer3D.Views;
using LabelVal.Sectors.Extensions;
using LabelVal.Sectors.Views;
using MahApps.Metro.Controls.Dialogs;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LabelVal.Results.Views;

public partial class ResultsDeviceEntry_V5 : UserControl
{
    private ViewModels.IResultsDeviceEntry _viewModel;

    private class RelayCommand : ICommand
    {
        private readonly Action<object> _exec;
        private readonly Func<object, bool> _can;
        public RelayCommand(Action<object> exec, Func<object, bool> can = null) { _exec = exec; _can = can; }
        public bool CanExecute(object p) => _can?.Invoke(p) ?? true;
        public void Execute(object p) => _exec(p);
        public event EventHandler CanExecuteChanged;
        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    public static readonly DependencyProperty CopyToClipboardCommandProperty =
        DependencyProperty.Register(nameof(CopyToClipboardCommand), typeof(ICommand), typeof(ResultsDeviceEntry_V5));
    public ICommand CopyToClipboardCommand
    {
        get => (ICommand)GetValue(CopyToClipboardCommandProperty);
        set => SetValue(CopyToClipboardCommandProperty, value);
    }

    public static readonly DependencyProperty ShowStoredSectorsCommandProperty =
        DependencyProperty.Register(nameof(ShowStoredSectorsCommand), typeof(ICommand), typeof(ResultsDeviceEntry_V5));
    public ICommand ShowStoredSectorsCommand
    {
        get => (ICommand)GetValue(ShowStoredSectorsCommandProperty);
        set => SetValue(ShowStoredSectorsCommandProperty, value);
    }

    public static readonly DependencyProperty ShowCurrentSectorsCommandProperty =
        DependencyProperty.Register(nameof(ShowCurrentSectorsCommand), typeof(ICommand), typeof(ResultsDeviceEntry_V5));
    public ICommand ShowCurrentSectorsCommand
    {
        get => (ICommand)GetValue(ShowCurrentSectorsCommandProperty);
        set => SetValue(ShowCurrentSectorsCommandProperty, value);
    }

    public ResultsDeviceEntry_V5()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => _viewModel = (ViewModels.IResultsDeviceEntry)DataContext;

        CopyToClipboardCommand    = new RelayCommand(ExecCopyToClipboard);
        ShowStoredSectorsCommand  = new RelayCommand(ExecShowStored);
        ShowCurrentSectorsCommand = new RelayCommand(ExecShowCurrent);
    }

    #region Command Methods
    private void ExecCopyToClipboard(object param)
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

    private void ExecShowStored(object param)
    {
        if (param is string s && s == "json")
        {
            if (_viewModel.Result != null)
                _viewModel.ResultsManagerView.ShowSectorsDetailsWindow(_viewModel.Result.Template, _viewModel.Result.Report);
        }
        else
        {
            _viewModel.ResultsManagerView.ShowSectorsDetailsWindow(_viewModel.StoredSectors);
        }
    }

    private void ExecShowCurrent(object param)
    {
        if (param is string s && s == "json")
            _viewModel.ResultsManagerView.ShowSectorsDetailsWindow(_viewModel.CurrentTemplate, _viewModel.CurrentReport);
        else
            _viewModel.ResultsManagerView.ShowSectorsDetailsWindow(_viewModel.CurrentSectors);
    }
    #endregion

    #region Scroll Sync & Mouse Wheel (restored for parity with V275/L95)
    private void ScrollStoredSectors_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (e.VerticalChange != 0 && ScrollCurrentSectors != null)
            ScrollCurrentSectors.ScrollToVerticalOffset(e.VerticalOffset);
    }

    private void ScrollCurrentSectors_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (e.VerticalChange != 0 && ScrollStoredSectors != null)
            ScrollStoredSectors.ScrollToVerticalOffset(e.VerticalOffset);
    }

    private void StoredSectors_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (e.Handled) return;
        e.Handled = ScrollStoredSectors?.ComputedVerticalScrollBarVisibility == Visibility.Visible;
        ScrollStoredSectors?.ScrollToVerticalOffset(ScrollStoredSectors.VerticalOffset - e.Delta);
    }

    private void CurrentSectors_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (e.Handled) return;
        e.Handled = ScrollCurrentSectors?.ComputedVerticalScrollBarVisibility == Visibility.Visible;
        ScrollCurrentSectors?.ScrollToVerticalOffset(ScrollCurrentSectors.VerticalOffset - e.Delta);
    }
    #endregion

    #region Existing Event Handlers
    private void btnCloseDetails_Click(object sender, RoutedEventArgs e)
    {
        if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
        {
            foreach (var device in _viewModel.ResultsEntry.ResultsDeviceEntries)
            {
                if (device.FocusedCurrentSector != null) device.FocusedCurrentSector.IsFocused = false;
                device.FocusedCurrentSector = null;

                if (device.FocusedStoredSector != null) device.FocusedStoredSector.IsFocused = false;
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
                    foreach (var device in _viewModel.ResultsEntry.ResultsDeviceEntries.Where(x => x.Device == _viewModel.Device))
                    {
                        if (device.FocusedStoredSector != null) device.FocusedStoredSector.IsFocused = false;
                        device.FocusedStoredSector = null;
                        _ = Application.Current.Dispatcher.BeginInvoke(device.RefreshStoredOverlay);
                    }
                    break;
                case "Current":
                    foreach (var device in _viewModel.ResultsEntry.ResultsDeviceEntries.Where(x => x.Device == _viewModel.Device))
                    {
                        if (device.FocusedCurrentSector != null) device.FocusedCurrentSector.IsFocused = false;
                        device.FocusedCurrentSector = null;

                        if (device.FocusedStoredSector != null) device.FocusedStoredSector.IsFocused = false;
                        device.FocusedStoredSector = null;

                        _ = Application.Current.Dispatcher.BeginInvoke(device.RefreshCurrentOverlay);
                        _ = Application.Current.Dispatcher.BeginInvoke(device.RefreshStoredOverlay);
                    }
                    break;
            }
        }
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

        var wnd = (Main.Views.MainWindow)Window.GetWindow(this);
        dc.Width = wnd.ActualWidth - 100;
        dc.Height = wnd.ActualHeight - 100;
        _ = DialogCoordinator.Instance.ShowMetroDialogAsync(wnd.DataContext, new ImageViewerDialogView { DataContext = dc });
        return true;
    }

    private void currentSectorMouseEnter(object sender, MouseEventArgs e)
    {
        if (sender is Sector s && s.DataContext is Sectors.Interfaces.ISector sec)
            sec.IsMouseOver = true;
        _ = Application.Current.Dispatcher.BeginInvoke(() => _viewModel.RefreshCurrentOverlay());
    }
    private void currentSectorMouseLeave(object sender, MouseEventArgs e)
    {
        if (sender is Sector s && s.DataContext is Sectors.Interfaces.ISector sec)
            sec.IsMouseOver = false;
        _ = Application.Current.Dispatcher.BeginInvoke(() => _viewModel.RefreshCurrentOverlay());
    }
    private void storedSectorMouseEnter(object sender, MouseEventArgs e)
    {
        if (sender is Sector s && s.DataContext is Sectors.Interfaces.ISector sec)
            sec.IsMouseOver = true;
        _ = Application.Current.Dispatcher.BeginInvoke(() => _viewModel.RefreshStoredOverlay());
    }
    private void storedSectorMouseLeave(object sender, MouseEventArgs e)
    {
        if (sender is Sector s && s.DataContext is Sectors.Interfaces.ISector sec)
            sec.IsMouseOver = false;
        _ = Application.Current.Dispatcher.BeginInvoke(() => _viewModel.RefreshStoredOverlay());
    }

    private void btnSaveImage_Click(object sender, RoutedEventArgs e)
    {
        DockPanel parent = Utilities.VisualTreeHelp.GetVisualParent<DockPanel>((Button)sender, 2);
        SectorDetails sectorDetails = Utilities.VisualTreeHelp.GetVisualChild<SectorDetails>(parent);
        if (sectorDetails != null)
        {
            var path = Utilities.FileUtilities.SaveFileDialog($"{((Sectors.Interfaces.ISector)sectorDetails.DataContext).Template.Username}", "PNG|*.png", "Save sector details.");
            if (!string.IsNullOrEmpty(path))
                try { SaveToPng(sectorDetails, path); } catch { }
        }
    }
    private void btnCopyImage_Click(object sender, RoutedEventArgs e)
    {
        DockPanel parent = Utilities.VisualTreeHelp.GetVisualParent<DockPanel>((Button)sender, 2);
        SectorDetails sectorDetails = Utilities.VisualTreeHelp.GetVisualChild<SectorDetails>(parent);
        if (sectorDetails != null) CopyToClipboard(sectorDetails);
    }

    public void SaveToPng(FrameworkElement visual, string fileName)
    {
        PngBitmapEncoder encoder = new();
        EncodeVisual(visual, encoder);
        using var fs = System.IO.File.Create(fileName);
        encoder.Save(fs);
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
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
    }

    private void Show3DImage(byte[] image)
    {
        var img = new ImageViewer3D.ViewModels.ImageViewer3D_SingleMesh(image);
        var wnd = (Main.Views.MainWindow)Window.GetWindow(this);
        img.Width = wnd.ActualWidth - 100;
        img.Height = wnd.ActualHeight - 100;
        var dlg = new ImageViewer3DDialogView { DataContext = img };
        dlg.Unloaded += (_, _) => img.Dispose();
        _ = DialogCoordinator.Instance.ShowMetroDialogAsync(wnd.DataContext, dlg);
    }

    private void Show3DViewerStored(object sender, RoutedEventArgs e) => Show3DImage(_viewModel.StoredImage.ImageBytes);
    private void Show3DViewerCurrent(object sender, RoutedEventArgs e) => Show3DImage(_viewModel.CurrentImage.ImageBytes);
    private void Show2DViewerStored(object sender, RoutedEventArgs e) => _ = ShowImage(_viewModel.StoredImage, _viewModel.StoredImageOverlay);
    private void Show2DViewerCurrent(object sender, RoutedEventArgs e) => _ = ShowImage(_viewModel.CurrentImage, _viewModel.CurrentImageOverlay);
    #endregion
}
