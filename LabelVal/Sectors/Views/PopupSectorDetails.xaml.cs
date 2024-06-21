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

namespace LabelVal.Sectors.Views
{
    /// <summary>
    /// Interaction logic for PopupSectorDetails.xaml
    /// </summary>
    public partial class PopupSectorDetails : UserControl
    {
        public PopupSectorDetails()
        {
            InitializeComponent();
        }

        private void btnSaveImage_Click(object sender, RoutedEventArgs e)
        {
            var card = ItemsControl;

            if (card != null)
            {
                string path;
                if ((path = Utilities.FileUtilities.GetSaveFilePath("plot", "PNG|*.png", "Export Results Plot.")) != "")
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
            var ic = ItemsControl; // Assuming ItemsControl is the visual element you want to copy

            if (ic != null)
                CopyToClipboard(ic);

        }
    }
}
