using CommunityToolkit.Mvvm.Input;
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

        var yourParentWindow = (Main.Views.MainWindow)Window.GetWindow(this);

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

        var yourParentWindow = (Main.Views.MainWindow)Window.GetWindow(this);

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
            catch (Exception ex) { Logger.Error(ex); }
        }
    }

    [RelayCommand]
    private void ShowSettings() => drwSettings.IsTopDrawerOpen = !drwSettings.IsTopDrawerOpen;

    private void drwSettings_DrawerClosing(object sender, MaterialDesignThemes.Wpf.DrawerClosingEventArgs e)
    {
        if(e.Dock == Dock.Top)
            {
            ((ViewModels.Scanner)DataContext).Manager.SaveCommand.Execute(null);
        }
    }


    //private void btnSetResults_Click(object sender, RoutedEventArgs e)
    //{
    //    CodeType = ((Scanner)DataContext).Results[0]["type"].ToString();
    //    ExpectedOutDataUTF8 = ((Scanner)DataContext).Results[0]["dataUTF8"].ToString();
    //}

    private void btnUnselect(object sender, RoutedEventArgs e)=>
        ((ViewModels.Scanner)this.DataContext).Manager.SelectedDevice = null;


}
