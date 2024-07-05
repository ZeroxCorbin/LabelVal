using System;
using System.Collections.Generic;
using System.IO;
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
        bitmap.Freeze();
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
        bitmap.Freeze();
        return bitmap;
    }

    public static System.Windows.Media.Imaging.BitmapImage CreatePNG(System.Drawing.Bitmap png)
    {
        var stream = new System.IO.MemoryStream();
        png.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
        stream.Position = 0;

        var wpfBitmap = new System.Windows.Media.Imaging.BitmapImage();
        wpfBitmap.BeginInit();
        wpfBitmap.StreamSource = stream;
        wpfBitmap.EndInit();
        wpfBitmap.Freeze();
        return wpfBitmap;

    }
    public static System.Windows.Media.Imaging.BitmapImage CreateBitmap(System.Drawing.Bitmap bitmap)
    {
        var stream = new System.IO.MemoryStream();
        bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Bmp);
        stream.Position = 0;

        var wpfBitmap = new System.Windows.Media.Imaging.BitmapImage();
        wpfBitmap.BeginInit();
        wpfBitmap.StreamSource = stream;
        wpfBitmap.EndInit();
        wpfBitmap.Freeze();
        return wpfBitmap;

    }

    public static System.Windows.Media.Imaging.BitmapImage CreateBitmap(byte[] data, int decodePixelWidth, int decodePixelHeight = 0)
    {
        var bitmap = new System.Windows.Media.Imaging.BitmapImage();
        bitmap.BeginInit();
        bitmap.DecodePixelWidth = decodePixelWidth;
        bitmap.DecodePixelHeight = decodePixelHeight;
        bitmap.StreamSource = new System.IO.MemoryStream(data);
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }
    public static System.Windows.Media.Imaging.BitmapImage CreateRandomBitmapImage(int width, int height)
    {
        var randomBitmap = new System.Drawing.Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        var random = new Random();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int alpha = 255; // Full opacity
                int red = random.Next(256); // 0 to 255
                int green = random.Next(256); // 0 to 255
                int blue = random.Next(256); // 0 to 255
                System.Drawing.Color randomColor = System.Drawing.Color.FromArgb(alpha, red, green, blue);
                randomBitmap.SetPixel(x, y, randomColor);
            }
        }


        // Convert System.Drawing.Bitmap to System.Windows.Media.Imaging.BitmapImage
        return CreateBitmap(randomBitmap);
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
