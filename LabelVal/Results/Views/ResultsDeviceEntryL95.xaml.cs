using LabelVal.Dialogs;
using LabelVal.ImageRolls.ViewModels;
using LabelVal.ImageViewer3D.Views;
using LabelVal.Sectors.Extensions;
using LabelVal.Sectors.Views;
using MahApps.Metro.Controls.Dialogs;
using Newtonsoft.Json.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LabelVal.Results.Views;
public partial class ResultsDeviceEntry_L95 : UserControl
{
    private ViewModels.IResultsDeviceEntry _viewModel;

    // RelayCommand
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

    // DependencyProperty Commands
    public static readonly DependencyProperty CopyToClipboardCommandProperty =
        DependencyProperty.Register(nameof(CopyToClipboardCommand), typeof(ICommand), typeof(ResultsDeviceEntry_L95));
    public ICommand CopyToClipboardCommand
    {
        get => (ICommand)GetValue(CopyToClipboardCommandProperty);
        set => SetValue(CopyToClipboardCommandProperty, value);
    }

    public static readonly DependencyProperty ShowStoredSectorsCommandProperty =
        DependencyProperty.Register(nameof(ShowStoredSectorsCommand), typeof(ICommand), typeof(ResultsDeviceEntry_L95));
    public ICommand ShowStoredSectorsCommand
    {
        get => (ICommand)GetValue(ShowStoredSectorsCommandProperty);
        set => SetValue(ShowStoredSectorsCommandProperty, value);
    }

    public static readonly DependencyProperty ShowCurrentSectorsCommandProperty =
        DependencyProperty.Register(nameof(ShowCurrentSectorsCommand), typeof(ICommand), typeof(ResultsDeviceEntry_L95));
    public ICommand ShowCurrentSectorsCommand
    {
        get => (ICommand)GetValue(ShowCurrentSectorsCommandProperty);
        set => SetValue(ShowCurrentSectorsCommandProperty, value);
    }

    public ResultsDeviceEntry_L95()
    {
        InitializeComponent();
        DataContextChanged += (e, s) => _viewModel = (ViewModels.IResultsDeviceEntry)DataContext;

        CopyToClipboardCommand = new RelayCommand(ExecCopyToClipboard);
        ShowStoredSectorsCommand = new RelayCommand(ExecShowStored);
        ShowCurrentSectorsCommand = new RelayCommand(ExecShowCurrent);
    }

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
            if (_viewModel?.Result?.Report != null)
            {
                List<JObject> reports = [];
                foreach (JToken group in _viewModel.Result.Report["AllReports"])
                    reports.Add((JObject)group["Report"]);

                List<JObject> templates = [];
                foreach (JToken group in _viewModel.Result.Report["AllReports"])
                    templates.Add((JObject)group["Template"]);

                var template = new JObject { ["Templates"] = JArray.FromObject(templates) };
                var report = new JObject { ["Reports"] = JArray.FromObject(reports) };
                _viewModel.ResultsManagerView.ShowSectorsDetailsWindow(template, report);
            }
        }
        else
        {
            _viewModel.ResultsManagerView.ShowSectorsDetailsWindow(_viewModel.StoredSectors);
        }
    }

    private void ExecShowCurrent(object param)
    {
        if (param is string s && s == "json")
        {
            if (_viewModel?.CurrentReport != null)
            {
                List<JObject> reports = [];
                foreach (JToken group in _viewModel.CurrentReport["AllReports"])
                    reports.Add((JObject)group["Report"]);

                List<JObject> templates = [];
                foreach (JToken group in _viewModel.CurrentReport["AllReports"])
                    templates.Add((JObject)group["Template"]);

                var template = new JObject { ["Templates"] = JArray.FromObject(templates) };
                var report = new JObject { ["Reports"] = JArray.FromObject(reports) };
                _viewModel.ResultsManagerView.ShowSectorsDetailsWindow(template, report);
            }
        }
        else
        {
            _viewModel.ResultsManagerView.ShowSectorsDetailsWindow(_viewModel.CurrentSectors);
        }
    }

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
        if (e.Handled)
        {
            return;
        }

        e.Handled = ScrollStoredSectors.ComputedVerticalScrollBarVisibility == Visibility.Visible;
        ScrollStoredSectors.ScrollToVerticalOffset(ScrollStoredSectors.VerticalOffset - e.Delta);
    }
    private void CurrentSectors_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (e.Handled)
        {
            return;
        }

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
            _ = ShowImage(((ViewModels.IResultsDeviceEntry)DataContext).StoredImage, ((ViewModels.IResultsDeviceEntry)DataContext).StoredImageOverlay);
        else if (e.LeftButton == MouseButtonState.Pressed)
            Show3DImage(((ViewModels.IResultsDeviceEntry)DataContext).StoredImage.ImageBytes);
    }
    private void CurrentImage_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && !(Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)))
            _ = ShowImage(((ViewModels.IResultsDeviceEntry)DataContext).CurrentImage, ((ViewModels.IResultsDeviceEntry)DataContext).CurrentImageOverlay);
        else if (e.LeftButton == MouseButtonState.Pressed)
            Show3DImage(((ViewModels.IResultsDeviceEntry)DataContext).CurrentImage.ImageBytes);
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

            if (DataContext is ViewModels.IResultsDeviceEntry ire)
                _ = Application.Current.Dispatcher.BeginInvoke(() => ire.RefreshCurrentOverlay());
        }
    }
    private void currentSectorMouseLeave(object sender, MouseEventArgs e)
    {
        if (sender is Sector sectorView)
        {
            if (sectorView.DataContext is Sectors.Interfaces.ISector sector)
                sector.IsMouseOver = false;

            _ = Application.Current.Dispatcher.BeginInvoke(() => _viewModel.RefreshCurrentOverlay());
        }
    }
    private void storedSectorMouseEnter(object sender, MouseEventArgs e)
    {
        if (sender is Sector sectorView)
        {
            if (sectorView.DataContext is Sectors.Interfaces.ISector sector)
                sector.IsMouseOver = true;

            _ = Application.Current.Dispatcher.BeginInvoke(() => _viewModel.RefreshStoredOverlay());
        }
    }
    private void storedSectorMouseLeave(object sender, MouseEventArgs e)
    {
        if (sender is Sector sectorView)
        {
            if (sectorView.DataContext is Sectors.Interfaces.ISector sector)
                sector.IsMouseOver = false;

            _ = Application.Current.Dispatcher.BeginInvoke(() => _viewModel.RefreshStoredOverlay());
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

    private void btnCopyToClipboard_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is System.Collections.ObjectModel.ObservableCollection<Sectors.Interfaces.ISector> sectors)
        {
            if (Sectors.Output.SectorOutputSettings.CurrentOutputType == Sectors.Output.SectorOutputType.Delimited)
                Clipboard.SetText(sectors.GetDelimetedSectorsReport($"{_viewModel.ResultsManagerView.ActiveImageRoll.Name}{(char)Sectors.Output.SectorOutputSettings.CurrentDelimiter}{_viewModel.ResultsEntry.SourceImage.Order}"));

            else if (Sectors.Output.SectorOutputSettings.CurrentOutputType == Sectors.Output.SectorOutputType.JSON)
                Clipboard.SetText(sectors.GetJsonSectorsReport($"{_viewModel.ResultsManagerView.ActiveImageRoll.Name}{(char)Sectors.Output.SectorOutputSettings.CurrentDelimiter}{_viewModel.ResultsEntry.SourceImage.Order}").ToString());

        }
        else if (sender is Button btn2 && btn2.Tag is ImageEntry image)
        {
            Clipboard.SetImage(image.Image);
        }
    }

    private void Show3DViewerCurrent(object sender, RoutedEventArgs e) => Show3DImage(((ViewModels.IResultsDeviceEntry)DataContext).CurrentImage.ImageBytes);
    private void Show3DViewerStored(object sender, RoutedEventArgs e) => Show3DImage(((ViewModels.IResultsDeviceEntry)DataContext).StoredImage.ImageBytes);

    private void Show2DViewerStored(object sender, RoutedEventArgs e) => _ = ShowImage(((ViewModels.IResultsDeviceEntry)DataContext).StoredImage, ((ViewModels.IResultsDeviceEntry)DataContext).StoredImageOverlay);
    private void Show2DViewerCurrent(object sender, RoutedEventArgs e) => _ = ShowImage(((ViewModels.IResultsDeviceEntry)DataContext).CurrentImage, ((ViewModels.IResultsDeviceEntry)DataContext).CurrentImageOverlay);
}
