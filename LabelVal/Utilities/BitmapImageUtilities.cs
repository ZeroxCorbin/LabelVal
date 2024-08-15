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
        using (var stream = System.IO.File.OpenWrite(path))
        {
            encoder.Save(stream);
        }
    }

    public static System.Windows.Media.Imaging.BitmapImage CreateBitmapImage(byte[] data)
    {
        var bitmap = new System.Windows.Media.Imaging.BitmapImage();
        bitmap.BeginInit();
        bitmap.StreamSource = new System.IO.MemoryStream(data);
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
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
    public static System.Windows.Media.Imaging.BitmapImage CreateBitmapImage(byte[] data, int decodePixelWidth)
    {
        var bitmap = new System.Windows.Media.Imaging.BitmapImage();
        bitmap.BeginInit();
        bitmap.DecodePixelWidth = decodePixelWidth;
        bitmap.DecodePixelHeight = 0;
        bitmap.StreamSource = new System.IO.MemoryStream(data);
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }
    public static System.Windows.Media.Imaging.BitmapImage CreateBitmapImage(byte[] data, int dpiX, int dpiY)
    {
        SetDPI(data, dpiX, dpiY);
        return CreateBitmapImage(data);
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

    #region DPI
    private const double InchesPerMeter = 39.3701;
    public static void SetDPI(byte[] image, int dpiX, int dpiY)
    {
        if (IsPng(image))
        {
            SetPngDPI(image, dpiX, dpiY);
        }
        else
        {
            SetBitmapDPI(image, dpiX, dpiY);
        }
    }
    public static bool IsPng(byte[] bytes)
    {
        // PNG files start with an 8-byte signature: 89 50 4E 47 0D 0A 1A 0A
        byte[] pngSignature = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };
        return bytes.Take(pngSignature.Length).SequenceEqual(pngSignature);
    }
    public static void SetBitmapDPI(byte[] image, int dpiX, int dpiY)
    {
        var valueX = BitConverter.GetBytes(ConvertDPI(dpiX));
        var valueY = BitConverter.GetBytes(ConvertDPI(dpiY));

        int i = 38;
        foreach (byte b in valueX)
            image[i++] = b;

        i = 42;
        foreach (byte b in valueY)
            image[i++] = b;
    }
    public static void SetPngDPI(byte[] image, int dpiX, int dpiY)
    {
        // Find the pHYs chunk and set the DPI values
        int pos = 8; // Skip the PNG signature
        while (pos < image.Length)
        {
            int length = BitConverter.ToInt32(image.Skip(pos).Take(4).Reverse().ToArray(), 0);
            string type = Encoding.ASCII.GetString(image, pos + 4, 4);
            if (type == "pHYs")
            {
                var dpiXBytes = BitConverter.GetBytes(ConvertDPI(dpiX)).Reverse().ToArray();
                var dpiYBytes = BitConverter.GetBytes(ConvertDPI(dpiY)).Reverse().ToArray();
                Array.Copy(dpiXBytes, 0, image, pos + 8, 4);
                Array.Copy(dpiYBytes, 0, image, pos + 12, 4);
                break;
            }
            pos += length + 12; // Move to the next chunk
        }
    }
    private static int ConvertDPI(int dpi) => (int)Math.Round(dpi * InchesPerMeter);
    #endregion
}
