using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace LabelVal.Utilities;
public class BitmapImageUtilities
{
    public static System.Windows.Media.Imaging.BitmapImage LoadBitmap(string path, int pixelWidth = 0)
    {
        var bitmap = new System.Windows.Media.Imaging.BitmapImage();
        bitmap.BeginInit();
        bitmap.DecodePixelWidth = pixelWidth;
        bitmap.UriSource = new Uri(path);
        bitmap.EndInit();
        return bitmap;
    }

    public static void SaveBitmap(System.Windows.Media.Imaging.BitmapImage bitmap, string path)
    {
        var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
        encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
        using (var stream = System.IO.File.OpenWrite(path))
        {
            encoder.Save(stream);
        }
    }

    public static System.Windows.Media.Imaging.BitmapImage CreateBitmap(byte[] data)
    {
        var bitmap = new System.Windows.Media.Imaging.BitmapImage();
        bitmap.BeginInit();
        bitmap.StreamSource = new System.IO.MemoryStream(data);
        bitmap.EndInit();
        return bitmap;
    }

    public static System.Windows.Media.Imaging.BitmapImage CreateBitmap(byte[] data, int decodePixelWidth, int decodePixelHeight = 0)
    {
        var bitmap = new System.Windows.Media.Imaging.BitmapImage();
        bitmap.BeginInit();
        bitmap.DecodePixelWidth = decodePixelWidth;
        bitmap.DecodePixelHeight = decodePixelHeight;
        bitmap.StreamSource = new System.IO.MemoryStream(data);
        bitmap.EndInit();
        return bitmap;
    }

    public static string ImageUID(System.Windows.Media.Imaging.BitmapImage image)
    {
        try
        {
            using SHA256 md5 = SHA256.Create();
            return BitConverter.ToString(md5.ComputeHash(ImageToBytesPNG(image))).Replace("-", String.Empty);
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }

    public static byte[] ImageToBytesPNG(System.Windows.Media.Imaging.BitmapImage image)
    {
        using (var ms = new System.IO.MemoryStream())
        {
            var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(image));
            encoder.Save(ms);
            return ms.ToArray();
        }
    }

    public static byte[] ImageToBytesBMP(System.Windows.Media.Imaging.BitmapImage image)
    {
        using (var ms = new System.IO.MemoryStream())
        {
            var encoder = new System.Windows.Media.Imaging.BmpBitmapEncoder();
            encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(image));
            encoder.Save(ms);
            return ms.ToArray();
        }
    }

}
