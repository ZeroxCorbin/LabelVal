using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace LabelVal.Dialogs;

public partial class ImageViewerDialogView : MahApps.Metro.Controls.Dialogs.CustomDialog
{
    private Image _img;
    public ImageViewerDialogView() => InitializeComponent();

    private void CustomDialog_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
            Close();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    private async void Close()
    {
        await MahApps.Metro.Controls.Dialogs.DialogCoordinator.Instance.HideMetroDialogAsync(this.DataContext, this);
        MahApps.Metro.Controls.Dialogs.DialogParticipation.SetRegister(this, null);
        this.DataContext = null;
    }

    private void Reset_Click(object sender, RoutedEventArgs e) => ZoomBorder.Reset();

    private void CustomDialog_Loaded(object sender, RoutedEventArgs e)
    {
        grdOverlays.Height = this.Height - 50;
        ZoomBorder.Width = this.Width - 25;

        int high = ((ImageViewerDialogViewModel)DataContext).Image.PixelWidth;
        foreach (DrawingImage overlay in ((ImageViewerDialogViewModel)DataContext).Overlays)
            high = Math.Max(high, (int)overlay.Width);

        _img = new Image
        {
            Source = ((ImageViewerDialogViewModel)DataContext).Image,
            VerticalAlignment = VerticalAlignment.Top,
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        RenderOptions.SetBitmapScalingMode(_img, BitmapScalingMode.NearestNeighbor);

        grdOverlays.Children.Add(_img);

        AddOverlaysToGrid();
    }

    private void AddOverlaysToGrid()
    {
        foreach (DrawingImage overlay in ((ImageViewerDialogViewModel)DataContext).Overlays)
        {
            Image img = new()
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
