using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LabelVal.Dialogs
{
    internal class ImageViewerDialogViewModel : ObservableObject
    {
        private double width;
        public double Width { get => width; set => SetProperty(ref width, value); }

        private double height;
        public double Height { get => height; set => SetProperty(ref height, value); }

        public double ImageHeight => RepeatImage?.PixelHeight ?? 0;
        public double ImageWidth => RepeatImage?.PixelWidth ?? 0;

        public double ImageDPIX => RepeatImage?.DpiX ?? 0;
        public double ImageDPIY => RepeatImage?.DpiY ?? 0;

        private BitmapImage repeatImage;
        public BitmapImage RepeatImage { get => repeatImage; set => SetProperty(ref repeatImage, value); }

        private DrawingImage repeatOverlay;
        public DrawingImage RepeatOverlay { get => repeatOverlay; set => SetProperty(ref repeatOverlay, value); }

        public void CreateImage(byte[] image, DrawingImage overlay)
        {
            if (image == null || image.Length < 2)
                return;

            RepeatImage = new BitmapImage();
            using (MemoryStream memStream = new MemoryStream(image))
            {
                RepeatImage.BeginInit();
                RepeatImage.CacheOption = BitmapCacheOption.OnLoad;
                RepeatImage.StreamSource = memStream;
                RepeatImage.EndInit();
                RepeatImage.Freeze();
            }
            
            RepeatOverlay = overlay;
        }
    }
}
