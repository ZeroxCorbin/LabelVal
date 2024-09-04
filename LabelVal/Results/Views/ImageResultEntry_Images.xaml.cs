using LabelVal.Dialogs;
using LabelVal.ImageRolls.ViewModels;
using MahApps.Metro.Controls.Dialogs;
using SharpDX.Direct2D1;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace LabelVal.Results.Views;

/// <summary>
/// Interaction logic for ImageResultEntry_Images.xaml
/// </summary>
public partial class ImageResultEntry_Images : UserControl
{
    public ImageResultEntry_Images() => InitializeComponent();
    private void SourceImage_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && !(Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)))
            ShowImage(((ViewModels.ImageResultEntry)DataContext).SourceImage, ((ViewModels.ImageResultEntry)DataContext).ShowPrinterAreaOverSource ? ((ViewModels.ImageResultEntry)DataContext).CreatePrinterAreaOverlay(false) : null);
        else if (e.LeftButton == MouseButtonState.Pressed)
            Show3DImage(((ViewModels.ImageResultEntry)DataContext).SourceImage.ImageBytes);
    }
    private void SourceImage_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl))
            e.Handled = true;
    }

    private void ShowImage(ImageEntry image, DrawingImage overlay)
    {
        ImageViewerDialogViewModel dc = new();

        dc.LoadImage(image.Image, overlay);
        if (dc.Image == null) return;

        var yourParentWindow = (Main.Views.MainWindow)Window.GetWindow(this);

        dc.Width = yourParentWindow.ActualWidth - 100;
        dc.Height = yourParentWindow.ActualHeight - 100;

        _ = DialogCoordinator.Instance.ShowMetroDialogAsync(yourParentWindow.DataContext, new ImageViewerDialogView() { DataContext = dc });


    }

    private void Show3DImage(byte[] image)
    {

     
        var bmp = LibImageUtilities.ImageTypes.Bmp.Utilities.GetBmp(image, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);

        var img = new ImageViewer3D.ViewModels.ImageViewer3D_new(bmp);

        var yourParentWindow = (Main.Views.MainWindow)Window.GetWindow(this);

        var tmp = new ImageViewer3D.Views.ImageViewer3DWindow() { DataContext  = img };
        tmp.Closed += (s, e) => img.Dispose();
        tmp.Owner = yourParentWindow;
        tmp.Show();

        //img.Width = yourParentWindow.ActualWidth - 100;
        //img.Height = yourParentWindow.ActualHeight - 100;

        //var tmp = new ImageViewer3DDialogView() { DataContext = img };
        //tmp.Unloaded += (s, e) => 
        //img.Dispose();
        //_ = DialogCoordinator.Instance.ShowMetroDialogAsync(yourParentWindow.DataContext, tmp);

    }

    private void btnShowPrinterAreaOverSourceToggle(object sender, RoutedEventArgs e) => ((ViewModels.ImageResultEntry)DataContext).ShowPrinterAreaOverSource = !((ViewModels.ImageResultEntry)DataContext).ShowPrinterAreaOverSource;

 
}
