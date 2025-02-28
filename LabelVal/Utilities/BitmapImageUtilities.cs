using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace LabelVal.Utilities;
public static class BitmapImageUtilities
{
    public static System.Windows.Media.Imaging.BitmapImage LoadBitmapImage(string path, int pixelWidth = 0)
    {
        var bitmap = new System.Windows.Media.Imaging.BitmapImage();
        bitmap.BeginInit();
        bitmap.DecodePixelWidth = pixelWidth;
        bitmap.UriSource = new Uri(path);
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }
    public static void SaveBitmapImage(System.Windows.Media.Imaging.BitmapImage bitmap, string path)
    {
        var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
        encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
        using var stream = System.IO.File.OpenWrite(path);
        encoder.Save(stream);
    }

    public static System.Windows.Media.Imaging.BitmapImage CreateBitmapImage(byte[] image)
    {
        var bitmap = new System.Windows.Media.Imaging.BitmapImage();
        bitmap.BeginInit();
        bitmap.StreamSource = new System.IO.MemoryStream(image);
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }

    public static System.Windows.Media.Imaging.BitmapImage CreateBitmapImage(byte[] image, bool flip)
    {
        if (flip)
        {
            var bitmap = new System.Windows.Media.Imaging.BitmapImage();
            bitmap.BeginInit();
            bitmap.StreamSource = new System.IO.MemoryStream(image);
            bitmap.Rotation = System.Windows.Media.Imaging.Rotation.Rotate180;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        else
        {
            return CreateBitmapImage(image);
        }
    }

    public static System.Windows.Media.Imaging.BitmapImage CreateBitmapImage(System.Drawing.Bitmap image, bool png = true)
    {
        var stream = new System.IO.MemoryStream();
        image.Save(stream, png ? System.Drawing.Imaging.ImageFormat.Png : System.Drawing.Imaging.ImageFormat.Bmp);
        stream.Position = 0;

        var wpfBitmap = new System.Windows.Media.Imaging.BitmapImage();
        wpfBitmap.BeginInit();
        wpfBitmap.StreamSource = stream;
        wpfBitmap.EndInit();
        wpfBitmap.Freeze();
        return wpfBitmap;

    }
    public static System.Windows.Media.Imaging.BitmapImage CreateBitmapImage(byte[] image, int decodePixelWidth)
    {
        var bitmap = new System.Windows.Media.Imaging.BitmapImage();
        bitmap.BeginInit();
        bitmap.DecodePixelWidth = decodePixelWidth;
        bitmap.DecodePixelHeight = 0;
        bitmap.StreamSource = new System.IO.MemoryStream(image);
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }
    public static System.Windows.Media.Imaging.BitmapImage CreateBitmapImage(byte[] image, int dpiX, int dpiY)
    {
       // SetDPI(image, dpiX, dpiY);
        return CreateBitmapImage(image);
    }

    public static System.Windows.Media.Imaging.BitmapImage ResizeImage(System.Windows.Media.Imaging.BitmapImage image, int width, int height)
    {
        var widthx = (double)image.PixelHeight / height;
        var resized = new System.Windows.Media.Imaging.TransformedBitmap(image, new System.Windows.Media.ScaleTransform((double)width / image.PixelWidth, (double)height / image.PixelHeight));
        var bitmapImage = new System.Windows.Media.Imaging.BitmapImage();
        using (var stream = new System.IO.MemoryStream())
        {
            var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(resized));
            encoder.Save(stream);
            stream.Seek(0, System.IO.SeekOrigin.Begin);
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = stream;
            bitmapImage.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            bitmapImage.Freeze();
        }
        return bitmapImage;
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
        return CreateBitmapImage(randomBitmap);
    }

    public static string ImageUID(System.Windows.Media.Imaging.BitmapImage image)
    {
        try
        {
            using SHA256 md5 = SHA256.Create();
            return BitConverter.ToString(md5.ComputeHash(ImageToBytes(image))).Replace("-", String.Empty);
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }
    public static byte[] ImageToBytes(System.Windows.Media.Imaging.BitmapImage image, bool png = true)
    {
        if (png)
            using (var ms = new System.IO.MemoryStream())
            {
                var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(image));
                encoder.Save(ms);
                return ms.ToArray();
            }
        else
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
}
