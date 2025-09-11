using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LabelVal.Utilities;

public static class BitmapHelpers
{
    /// <summary>
    /// Creates a BitmapImage from a byte array.
    /// </summary>
    /// <param name="bytes">The byte array containing the image data.</param>
    /// <param name="decodePixelWidth">The desired width to decode the image to. If 0, the original width is used.</param>
    /// <returns>A BitmapImage.</returns>
    public static BitmapImage CreateBitmapImage(byte[] bytes, int decodePixelWidth = 0)
    {
        if (bytes == null || bytes.Length == 0)
            return null;

        var image = new BitmapImage();
        using (var mem = new MemoryStream(bytes))
        {
            mem.Position = 0;
            image.BeginInit();
            image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = null;
            if (decodePixelWidth > 0)
            {
                image.DecodePixelWidth = decodePixelWidth;
            }
            image.StreamSource = mem;
            image.EndInit();
        }
        image.Freeze();
        return image;
    }

    /// <summary>
    /// Loads a BitmapImage from a file path.
    /// </summary>
    /// <param name="path">The path to the image file.</param>
    /// <param name="decodePixelWidth">The desired width to decode the image to. If 0, the original width is used.</param>
    /// <returns>A BitmapImage.</returns>
    public static BitmapImage LoadBitmapImage(string path, int decodePixelWidth = 0)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            return null;

        var image = new BitmapImage();
        image.BeginInit();
        image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
        image.CacheOption = BitmapCacheOption.OnLoad;
        if (decodePixelWidth > 0)
        {
            image.DecodePixelWidth = decodePixelWidth;
        }
        image.UriSource = new Uri(path);
        image.EndInit();
        image.Freeze();
        return image;
    }

    /// <summary>
    /// Gets the dimensions of an image from its byte array.
    /// </summary>
    /// <param name="imageData">The byte array of the image.</param>
    /// <returns>A tuple containing the width and height of the image.</returns>
    public static (int Width, int Height) GetImageDimensions(byte[] imageData)
    {
        if (imageData == null || imageData.Length == 0)
            return (0, 0);

        using (var stream = new MemoryStream(imageData))
        {
            var (width, height, _, _, _, _) = GetImageMetadata(stream);
            return (width, height);
        }
    }

    /// <summary>
    /// Gets comprehensive metadata for an image from a stream.
    /// </summary>
    /// <param name="stream">The stream containing the image data.</param>
    /// <returns>A tuple with the image's metadata.</returns>
    public static (int Width, int Height, double DpiX, double DpiY, PixelFormat Format, int BitDepth) GetImageMetadata(Stream stream)
    {
        var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.IgnoreColorProfile, BitmapCacheOption.Default);
        var frame = decoder.Frames[0];
        return (frame.PixelWidth, frame.PixelHeight, frame.DpiX, frame.DpiY, frame.Format, frame.Format.BitsPerPixel);
    }

    /// <summary>
    /// Converts a BitmapImage to a byte array (PNG format).
    /// </summary>
    /// <param name="image">The BitmapImage to convert.</param>
    /// <returns>A byte array representing the image.</returns>
    public static byte[] ImageToBytes(BitmapImage image)
    {
        if (image == null)
            return null;

        BitmapEncoder encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(image));
        using (var ms = new MemoryStream())
        {
            encoder.Save(ms);
            return ms.ToArray();
        }
    }
}