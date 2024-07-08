using LabelVal.Dialogs;
using LabelVal.ImageRolls.ViewModels;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LabelVal.Run.Views;
/// <summary>
/// Interaction logic for RunResult_Images.xaml
/// </summary>
public partial class RunResult_Images : UserControl
{
    public RunResult_Images()
    {
        InitializeComponent();
    }

    private void btnShowDetailsToggle(object sender, RoutedEventArgs e)=>
        ((ViewModels.RunResult)DataContext).ShowDetails = !((ViewModels.RunResult)DataContext).ShowDetails;

    private void btnShowPrinterAreaOverSourceToggle(object sender, RoutedEventArgs e)=>
        ((ViewModels.RunResult)DataContext).ShowPrinterAreaOverSource = !((ViewModels.RunResult)DataContext).ShowPrinterAreaOverSource;
    private void SourceImage_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            _ = ShowImage(((ViewModels.RunResult)DataContext).SourceImage, ((ViewModels.RunResult)DataContext).ShowPrinterAreaOverSource ? ((ViewModels.RunResult)DataContext).CreatePrinterAreaOverlay(false) : null);

    }
    private void V275CurrentImage_MouseDown(object sender, MouseButtonEventArgs e)
    {
       if (e.LeftButton == MouseButtonState.Pressed)
            _ = ShowImage(((ViewModels.RunResult)DataContext).V275CurrentImage, ((ViewModels.RunResult)DataContext).V275CurrentImageOverlay);

    }

    private void V275StoredImage_MouseDown(object sender, MouseButtonEventArgs e)
    {
       if (e.LeftButton == MouseButtonState.Pressed)
            _ = ShowImage(((ViewModels.RunResult)DataContext).V275StoredImage, ((ViewModels.RunResult)DataContext).V275StoredImageOverlay);
    }

    private void V5CurrentImage_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            _ = ShowImage(((ViewModels.RunResult)DataContext).V5CurrentImage, ((ViewModels.RunResult)DataContext).V5CurrentImageOverlay);

    }

    private void V5StoredImage_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            _ = ShowImage(((ViewModels.RunResult)DataContext).V5StoredImage, ((ViewModels.RunResult)DataContext).V5StoredImageOverlay);

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
}
