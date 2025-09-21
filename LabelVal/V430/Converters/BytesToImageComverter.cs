using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace LensCharacterization.WPF.Converters
{
    internal class BytesToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is byte[] imageByteArray) || imageByteArray.Length < 2)
                return null;

            try
            {
                BitmapImage img = new();

                using (MemoryStream memStream = new(imageByteArray))
                {
                    img.BeginInit();
                    img.CacheOption = BitmapCacheOption.OnLoad;
                    img.StreamSource = memStream;
                    //img.DecodePixelWidth = 400;
                    img.EndInit();
                    img.Freeze();

                }
                return img;
            }
            catch { }

            return null;

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
