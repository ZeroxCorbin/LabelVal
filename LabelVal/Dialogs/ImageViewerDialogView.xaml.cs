using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;
using System.IO;
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

namespace LabelVal.Dialogs
{
    /// <summary>
    /// Interaction logic for ImageViewerDialog.xaml
    /// </summary>
    public partial class ImageViewerDialogView : MahApps.Metro.Controls.Dialogs.CustomDialog
    {
        private Image _img;
        public ImageViewerDialogView()
        {
            InitializeComponent();
        }


        private void CustomDialog_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void Close()
        {
            await MahApps.Metro.Controls.Dialogs.DialogCoordinator.Instance.HideMetroDialogAsync(this.DataContext, this);
            MahApps.Metro.Controls.Dialogs.DialogParticipation.SetRegister(this, null);
            this.DataContext = null;
        }

        private void Reset_Click(object sender, RoutedEventArgs e) { }// => ZoomBorder.Reset();

        private void CustomDialog_Loaded(object sender, RoutedEventArgs e)
        {
            grdOverlays.Height = this.Height - 50;
            ZoomBorder.Width = this.Width - 25;

            var high = ((ImageViewerDialogViewModel)DataContext).Image.PixelWidth;
            foreach (var overlay in ((ImageViewerDialogViewModel)DataContext).Overlays)
                high = Math.Max(high, (int)overlay.Width);

            _img = new Image
            {
                Source = ((ImageViewerDialogViewModel)DataContext).Image,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left,
                Stretch = Stretch.Uniform,
                StretchDirection = StretchDirection.Both
            };
            RenderOptions.SetBitmapScalingMode(_img, BitmapScalingMode.NearestNeighbor);

            grdOverlays.Children.Add(_img);

            AddOverlaysToGrid();
        }

        private void AddOverlaysToGrid()
        {
            foreach (var overlay in ((ImageViewerDialogViewModel)DataContext).Overlays)
            {
                var img = new Image
                {
                    Source = overlay,
                    VerticalAlignment = VerticalAlignment.Top,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Width = _img.Width,
                    Height = _img.Height,

                };
                RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.NearestNeighbor);

                grdOverlays.Children.Add(img);
            }
        }
    }
}
