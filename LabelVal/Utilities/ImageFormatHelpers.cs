using System;
using System.IO;
using System.Windows.Media.Imaging;

namespace LabelVal.Utilities;

public enum ImageContainerFormat
{
    Unknown,
    Png,
    Jpeg,
    Bmp,
    Gif,
    Tiff
}

public static class ImageFormatHelpers
{
    public static ImageContainerFormat DetectFormat(byte[] bytes)
    {
        if (bytes == null || bytes.Length < 4) return ImageContainerFormat.Unknown;

        // Magic numbers
        if (bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47) return ImageContainerFormat.Png;
        if (bytes[0] == 0xFF && bytes[1] == 0xD8) return ImageContainerFormat.Jpeg;
        if (bytes[0] == 0x42 && bytes[1] == 0x4D) return ImageContainerFormat.Bmp;
        if (bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46) return ImageContainerFormat.Gif;
        if ((bytes[0] == 0x49 && bytes[1] == 0x49 && bytes[2] == 0x2A && bytes[3] == 0x00) ||
            (bytes[0] == 0x4D && bytes[1] == 0x4D && bytes[2] == 0x00 && bytes[3] == 0x2A))
            return ImageContainerFormat.Tiff;

        return ImageContainerFormat.Unknown;
    }

    /// <summary>
    /// Ensures a valid DPI is present. If image already has DPI >= 10 it is returned unchanged.
    /// If missing/invalid, it is re-encoded with the fallback DPI using the matching encoder (except JPEG - lossy).
    /// </summary>
    public static byte[] EnsureDpi(byte[] original, double fallbackDpiX, double fallbackDpiY, out double finalDpiX, out double finalDpiY)
    {
        finalDpiX = finalDpiY = 0;
        if (original == null || original.Length == 0) return original ?? Array.Empty<byte>();

        using var ms = new MemoryStream(original, false);
        var decoder = BitmapDecoder.Create(ms, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        var frame = decoder.Frames[0];
        finalDpiX = frame.DpiX;
        finalDpiY = frame.DpiY;

        bool invalid = finalDpiX < 10 || finalDpiY < 10;

        if (!invalid)
            return original; // Already good

        finalDpiX = fallbackDpiX;
        finalDpiY = fallbackDpiY;

        var format = DetectFormat(original);

        // For JPEG we avoid re-encoding unless absolutely necessary (quality loss). If fallback == 96 we accept WPF default.
        if (format == ImageContainerFormat.Jpeg)
        {
            // Re-encode only if caller explicitly wants a DPI different from WPF default 96
            if (Math.Abs(fallbackDpiX - 96) < 0.1 && Math.Abs(fallbackDpiY - 96) < 0.1)
                return original;

            return Reencode(frame, finalDpiX, finalDpiY, new JpegBitmapEncoder { QualityLevel = 100 });
        }

        BitmapEncoder encoder = format switch
        {
            ImageContainerFormat.Png => new PngBitmapEncoder(),
            ImageContainerFormat.Bmp => new BmpBitmapEncoder(),
            ImageContainerFormat.Gif => new GifBitmapEncoder(),
            ImageContainerFormat.Tiff => new TiffBitmapEncoder(),
            _ => new PngBitmapEncoder()
        };

        return Reencode(frame, finalDpiX, finalDpiY, encoder);
    }

    private static byte[] Reencode(BitmapFrame frame, double dpiX, double dpiY, BitmapEncoder encoder)
    {
        int stride = (frame.PixelWidth * frame.Format.BitsPerPixel + 7) / 8;
        var pixels = new byte[stride * frame.PixelHeight];
        frame.CopyPixels(pixels, stride, 0);

        var newFrame = BitmapFrame.Create(
            BitmapSource.Create(frame.PixelWidth, frame.PixelHeight, dpiX, dpiY, frame.Format, frame.Palette, pixels, stride));

        encoder.Frames.Add(newFrame);
        using var outMs = new MemoryStream();
        encoder.Save(outMs);
        return outMs.ToArray();
    }
}