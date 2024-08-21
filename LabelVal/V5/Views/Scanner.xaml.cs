using LabelVal.Dialogs;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Wpf.Controls.PanAndZoom;

namespace LabelVal.V5.Views;

public partial class Scanner : UserControl
{
    public static readonly DependencyProperty IsDockedProperty =
    DependencyProperty.Register(
        nameof(IsDocked),
        typeof(bool),
        typeof(Scanner),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public bool IsDocked
    {
        get => (bool)GetValue(IsDockedProperty);
        set => SetValue(IsDockedProperty, value);
    }

    public Scanner()
    {
        InitializeComponent();
    }



    private void SourceImage_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && e.ClickCount == 2)
            ShowImage(((ViewModels.Scanner)DataContext).Image, ((ViewModels.Scanner)DataContext).ImageOverlay, ((ViewModels.Scanner)DataContext).ImageFocusRegionOverlay);
    }

    private bool ShowImage(byte[] image, DrawingImage overlay, DrawingImage overlay1)
    {
        ImageViewerDialogViewModel dc = new();

        dc.LoadImage(image, [overlay, overlay1]);
        if (dc.Image == null) return false;

        Main.Views.MainWindow yourParentWindow = (Main.Views.MainWindow)Window.GetWindow(this);

        dc.Width = yourParentWindow.ActualWidth - 100;
        dc.Height = yourParentWindow.ActualHeight - 100;

        DialogCoordinator.Instance.ShowMetroDialogAsync(yourParentWindow.DataContext, new ImageViewerDialogView() { DataContext = dc });

        return true;

    }

    private bool ShowImage(BitmapImage image, DrawingImage overlay, DrawingImage overlay1)
    {
        ImageViewerDialogViewModel dc = new();

        dc.LoadImage(image, [overlay, overlay1]);
        if (dc.Image == null) return false;

        Main.Views.MainWindow yourParentWindow = (Main.Views.MainWindow)Window.GetWindow(this);

        dc.Width = yourParentWindow.ActualWidth - 100;
        dc.Height = yourParentWindow.ActualHeight - 100;

        DialogCoordinator.Instance.ShowMetroDialogAsync(yourParentWindow.DataContext, new ImageViewerDialogView() { DataContext = dc });

        return true;

    }

    private void btnResetImageView_Click(object sender, RoutedEventArgs e) => ZoomBorder.Reset();

    private void btnSaveImage_Click(object sender, RoutedEventArgs e)
    {
        string path;
        if ((path = Utilities.FileUtilities.SaveFileDialog("", "PNG|*.png", "Save Image")) != "")
        {
            try
            {
                System.IO.File.WriteAllBytes(path, ((ViewModels.Scanner)DataContext).RawImage);
            }
            catch (Exception ex) { LogError(ex); }
        }
    }

    private void btnShowSettings_Click(object sender, RoutedEventArgs e) => drwSettings.IsTopDrawerOpen = !drwSettings.IsTopDrawerOpen;
    private void drwSettings_DrawerClosing(object sender, MaterialDesignThemes.Wpf.DrawerClosingEventArgs e)
    {
        if(e.Dock == Dock.Top)
            {
            ((ViewModels.Scanner)DataContext).Manager.SaveCommand.Execute(null);
        }
    }

    private void btnOpenInBrowser_Click(object sender, RoutedEventArgs e)
    {
        string addr = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)
            ? $"http://{((ViewModels.Scanner)DataContext).Host}:9898"
            : $"http://{((ViewModels.Scanner)DataContext).Host}:{((ViewModels.Scanner)DataContext).Port}";

        ProcessStartInfo ps = new(addr)
        {
            UseShellExecute = true,
            Verb = "open"
        };
        _ = Process.Start(ps);
    }

    //private void btnSetResults_Click(object sender, RoutedEventArgs e)
    //{
    //    CodeType = ((Scanner)DataContext).Results[0]["type"].ToString();
    //    ExpectedOutDataUTF8 = ((Scanner)DataContext).Results[0]["dataUTF8"].ToString();
    //}

    #region Logging
    private readonly Logging.Logger logger = new();
    public void LogInfo(string message) => logger.LogInfo(this.GetType(), message);
    public void LogDebug(string message) => logger.LogDebug(this.GetType(), message);
    public void LogWarning(string message) => logger.LogInfo(this.GetType(), message);
    public void LogError(string message) => logger.LogError(this.GetType(), message);
    public void LogError(Exception ex) => logger.LogError(this.GetType(), ex);
    public void LogError(string message, Exception ex) => logger.LogError(this.GetType(), message, ex);
    #endregion

    private void btnUnselect(object sender, RoutedEventArgs e)=>
        ((ViewModels.Scanner)this.DataContext).Manager.SelectedDevice = null;


}
