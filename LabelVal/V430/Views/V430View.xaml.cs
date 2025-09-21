using BarcodeVerification.lib.Common;
using LabelVal.Dialogs;
using LabelVal.V430.ViewModels;
using MahApps.Metro.Controls.Dialogs;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LabelVal.V430.Views;

/// <summary>
/// Interaction logic for V430View.xaml
/// </summary>
public partial class V430View : UserControl
{
    public V430ViewModel ViewModel { get; }

    public V430View()
    {
        InitializeComponent();
        ViewModel = App.GetService<V430ViewModel>()!;
        Loaded += (s, e) => DataContext = this;
    }

    private void SourceImage_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && e.ClickCount == 2)
            ShowImage(((ViewModels.V430ViewModel)DataContext).Image, ((ViewModels.V430ViewModel)DataContext).ImageOverlay, ((ViewModels.V430ViewModel)DataContext).ImageFocusRegionOverlay);

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

    private void btnResetImageView_Click(object sender, RoutedEventArgs e) => ZoomBorder_Source.Reset();

    private void btnSaveImage_Click(object sender, RoutedEventArgs e)
    {
        string path;
        if ((path = Utilities.FileUtilities.SaveFileDialog("", "PNG|*.png", "Save Image")) != "")
        {
            try
            {
                System.IO.File.WriteAllBytes(path, ((ViewModels.V430ViewModel)DataContext).RawImage);
            }
            catch (Exception ex) { Logger.Error(ex); }
        }
    }

    private void btnKCommand_Click(object sender, RoutedEventArgs e) => drwRunSettings.IsRightDrawerOpen = !drwRunSettings.IsRightDrawerOpen;

    //private void lstItmSector_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
    //{
    //    if (sender is not Sector sect) return;
    //    if (sect.DataContext is not ISector sector) return;

    //    CodeType = sector.Report.SymbolType;
    //    ExpectedOutDataUTF8 = sector.Report.DecodeText;
    //   // CodePpe = sector.Report.Original.GetParameter<double>("ipReports[0].decodes[0].ppe");

    //}

    private void btnShowCommandSettings(object sender, RoutedEventArgs e) => drwRunSettings.IsLeftDrawerOpen = !drwRunSettings.IsLeftDrawerOpen;
}
