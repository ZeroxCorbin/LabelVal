using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace LabelVal.Converters
{
    [ValueConversion(typeof(byte[]), typeof(BitmapImage))]
    public class Bytes2BitmapImage : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not byte[] imageByteArray || imageByteArray.Length < 2)
                return null;

            BitmapImage img = new();

            using (MemoryStream memStream = new(imageByteArray))
            {
                img.BeginInit();
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.StreamSource = memStream;
                img.EndInit();
                img.Freeze();
            }
            return img;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not BitmapImage img)
                return null;

            using MemoryStream memStream = new();
            PngBitmapEncoder encoder = new();
            encoder.Frames.Add(BitmapFrame.Create(img));
            encoder.Save(memStream);
            return memStream.ToArray();
        }
    }
}
