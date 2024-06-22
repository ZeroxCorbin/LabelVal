using LabelVal.Results.Views;
using MahApps.Metro.Controls.Dialogs;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace LabelVal.Sectors.Views;

/// <summary>
/// Interaction logic for SectorControlView.xaml
/// </summary>
public partial class Sector : UserControl
{
    public Sector() => InitializeComponent();
    private void Button_Click(object sender, RoutedEventArgs e) => popSymbolDetails.IsOpen = true;
    private void btnGS1DecodeText_Click(object sender, RoutedEventArgs e) => popGS1DecodeText.IsOpen = true;
    private void Show95xxCompare_Click(object sender, RoutedEventArgs e)
    {
        //LVS_95xx.LVS95xx_SerialPortView sp = new LVS_95xx.LVS95xx_SerialPortView(this.DataContext);

        //var dc = new LVS_95xx.ViewModels.Verifier();

        //var yourParentWindow = Window.GetWindow(this);

        //dc.Width = yourParentWindow.ActualWidth - 200;
        //dc.Height = yourParentWindow.ActualHeight - 200;

        //_ = DialogCoordinator.Instance.ShowMetroDialogAsync(yourParentWindow.DataContext, new LVS_95xx.LVS95xx_SerialPortView() { DataContext = dc });

        //L95xxComparePopup.PlacementTarget = (Button)sender;
        //L95xxComparePopup.IsOpen = true;
    }

    private void btnSaveImage_Click(object sender, RoutedEventArgs e)
    {
        var card = SectorDetails;

        if (card != null)
        {
            string path;
            if ((path = Utilities.FileUtilities.GetSaveFilePath("plot", "PNG|*.png", "Save sector details.")) != "")
            {
                try
                {
                    SaveToPng(card, path);
                }
                catch { }
            }
        }
    }

    public void SaveToPng(FrameworkElement visual, string fileName)
    {
        var encoder = new PngBitmapEncoder();
        EncodeVisual(visual, encoder);

        using var stream = System.IO.File.Create(fileName);
        encoder.Save(stream);
    }
    private static void EncodeVisual(FrameworkElement visual, BitmapEncoder encoder)
    {
        var bitmap = new RenderTargetBitmap((int)visual.ActualWidth, (int)visual.ActualHeight, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        var frame = BitmapFrame.Create(bitmap);
        encoder.Frames.Add(frame);
    }

    public void CopyToClipboard(FrameworkElement visual)
    {
        var encoder = new PngBitmapEncoder();
        EncodeVisual(visual, encoder);

        using (var stream = new System.IO.MemoryStream())
        {
            encoder.Save(stream);
            stream.Seek(0, System.IO.SeekOrigin.Begin);
            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.StreamSource = stream;
            bitmapImage.EndInit();
            Clipboard.SetImage(bitmapImage);
        }
    }

    private void btnCopyImage_Click(object sender, RoutedEventArgs e)
    {
        var ic = SectorDetails; // Assuming ItemsControl is the visual element you want to copy

        if (ic != null)
            CopyToClipboard(ic);

    }
}
