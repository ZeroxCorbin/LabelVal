using nQuant;
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
