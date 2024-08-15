using nQuant;
using System;
using System.Drawing;
using System.IO;

namespace LabelVal.Utilities;
public static class ImageQuantUtilities
{
    public static byte[] RawBitmapToQuantImageBytes(byte[] image)
    {
        Bitmap bmp = CreateBitmap(image);
        if (bmp.PixelFormat == System.Drawing.Imaging.PixelFormat.Format8bppIndexed)
            return image;

        Image quantizedImage = new WuQuantizer().QuantizeImage(bmp);
        return ImageToBytes(quantizedImage); ;
    }

    public static System.Windows.Media.Imaging.BitmapImage RawBitmapToQuantBitmapImage(byte[] image)
    {
        Bitmap bmp = CreateBitmap(image);
        if (bmp == null)
            return new System.Windows.Media.Imaging.BitmapImage();

        Image quantizedImage = new WuQuantizer().QuantizeImage(bmp);
        return CreateBitmapImage(ImageToBytes(quantizedImage));
    }

    public static int GetBitDepth(Bitmap bitmap)
    {
        switch (bitmap.PixelFormat)
        {
            case System.Drawing.Imaging.PixelFormat.Format1bppIndexed:
                return 1;
            case System.Drawing.Imaging.PixelFormat.Format4bppIndexed:
                return 4;
            case System.Drawing.Imaging.PixelFormat.Format8bppIndexed:
                return 8;
            case System.Drawing.Imaging.PixelFormat.Format16bppArgb1555:
            case System.Drawing.Imaging.PixelFormat.Format16bppGrayScale:
            case System.Drawing.Imaging.PixelFormat.Format16bppRgb555:
            case System.Drawing.Imaging.PixelFormat.Format16bppRgb565:
                return 16;
            case System.Drawing.Imaging.PixelFormat.Format24bppRgb:
                return 24;
            case System.Drawing.Imaging.PixelFormat.Format32bppArgb:
            case System.Drawing.Imaging.PixelFormat.Format32bppPArgb:
            case System.Drawing.Imaging.PixelFormat.Format32bppRgb:
                return 32;
            case System.Drawing.Imaging.PixelFormat.Format48bppRgb:
                return 48;
            case System.Drawing.Imaging.PixelFormat.Format64bppArgb:
            case System.Drawing.Imaging.PixelFormat.Format64bppPArgb:
                return 64;
            default:
                return 0;
        }
    }

    private static System.Windows.Media.Imaging.BitmapImage CreateBitmapImage(byte[] data)
    {
        var bitmap = new System.Windows.Media.Imaging.BitmapImage();
        bitmap.BeginInit();
        bitmap.StreamSource = new System.IO.MemoryStream(data);
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }

    private static System.Drawing.Bitmap CreateBitmap(byte[] image)
    {
        using MemoryStream ms = new(image);
        try
        {
            return new System.Drawing.Bitmap(ms);
        }
        catch
        {
            return null;
        }
        
    }

    private static byte[] ImageToBytes(System.Drawing.Image image)
    {
        System.Drawing.ImageConverter converter = new();
        return (byte[])converter.ConvertTo(image, typeof(byte[]));
    }
}
