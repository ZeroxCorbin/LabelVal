using LabelVal.Dialogs;
using LabelVal.ImageRolls.ViewModels;
using LabelVal.WindowViews;
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

namespace LabelVal.ImageRolls.Views
{
    /// <summary>
    /// Interaction logic for ImageResultEntry_Images.xaml
    /// </summary>
    public partial class ImageResultEntry_Images : UserControl
    {
        public ImageResultEntry_Images()
        {
            InitializeComponent();
        }

        private void SourceImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                _ = ShowImage(((ViewModels.ImageResultEntry)DataContext).SourceImage, null);
        }
        private void SourceImage_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl))
                e.Handled = true;
        }

        private void V275Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                _ = ShowImage(((ViewModels.ImageResultEntry)DataContext).V275Image, ((ViewModels.ImageResultEntry)DataContext).V275SectorsImageOverlay);
        }

        private void V5Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                _ = ShowImage(((ViewModels.ImageResultEntry)DataContext).V5Image, ((ViewModels.ImageResultEntry)DataContext).V5SectorsImageOverlay);
        }

        //private void L95xxImage_MouseDown(object sender, MouseButtonEventArgs e)
        //{
        //    if (e.LeftButton == MouseButtonState.Pressed)
        //        _ = ShowImage(((ViewModels.ImageResultEntry)DataContext).L95xxImage, ((ViewModels.ImageResultEntry)DataContext).L95xxSectorsImageOverlay);
        //}

        private bool ShowImage(byte[] image, DrawingImage overlay)
        {
            var dc = new ImageViewerDialogViewModel();

            dc.LoadImage(image, overlay);
            if (dc.Image == null) return false;

            var yourParentWindow = (MainWindowView)Window.GetWindow(this);

            dc.Width = yourParentWindow.ActualWidth - 100;
            dc.Height = yourParentWindow.ActualHeight - 100;

            _ = DialogCoordinator.Instance.ShowMetroDialogAsync(yourParentWindow.DataContext, new ImageViewerDialogView() { DataContext = dc });

            return true;

        }
        private bool ShowImage(ImageEntry image, DrawingImage overlay)
        {
            var dc = new ImageViewerDialogViewModel();

            dc.LoadImage(image.Image, overlay);
            if (dc.Image == null) return false;

            var yourParentWindow = (MainWindowView)Window.GetWindow(this);

            dc.Width = yourParentWindow.ActualWidth - 100;
            dc.Height = yourParentWindow.ActualHeight - 100;

            _ = DialogCoordinator.Instance.ShowMetroDialogAsync(yourParentWindow.DataContext, new ImageViewerDialogView() { DataContext = dc });

            return true;

        }
    }
}
