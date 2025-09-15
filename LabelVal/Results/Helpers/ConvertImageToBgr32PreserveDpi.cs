using System;
using System.IO;
using System.Windows.Media.Imaging;

namespace LabelVal.Results.Helpers;
public class ConvertImageToBgr32PreserveDpi
{
    public static byte[] Convert(byte[] image, out double dpiX, out double dpiY)
    {
        return ConvertInternal(image, null, out dpiX, out dpiY);
    }

    public static byte[] Convert(byte[] image, int fallbackDpi, out double dpiX, out double dpiY)
    {
        return ConvertInternal(image, fallbackDpi, out dpiX, out dpiY);
    }

    private static byte[] ConvertInternal(byte[] image, int? fallback, out double dpiX, out double dpiY)
    {
        dpiX = dpiY = 0;
        if (image == null || image.Length == 0)
            return image ?? Array.Empty<byte>();

        try
        {
            using var ms = new MemoryStream(image, false);
            var decoder = BitmapDecoder.Create(ms, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            var frame = decoder.Frames[0];

            dpiX = frame.DpiX;
            dpiY = frame.DpiY;

            bool dpiInvalid = dpiX < 10 || dpiY < 10;

            BitmapSource working = frame.Format == System.Windows.Media.PixelFormats.Bgr32
                ? frame
                : new FormatConvertedBitmap(frame, System.Windows.Media.PixelFormats.Bgr32, null, 0);

            double outDpiX = dpiInvalid && fallback.HasValue ? fallback.Value : working.DpiX;
            double outDpiY = dpiInvalid && fallback.HasValue ? fallback.Value : working.DpiY;

            if (!dpiInvalid)
            {
                // If already BGR32 + valid DPI just return original bytes
                if (frame.Format == System.Windows.Media.PixelFormats.Bgr32)
                    return image;
            }

            int stride = (working.PixelWidth * working.Format.BitsPerPixel + 7) / 8;
            var pixels = new byte[stride * working.PixelHeight];
            working.CopyPixels(pixels, stride, 0);

            var bmpSource = BitmapSource.Create(
                working.PixelWidth,
                working.PixelHeight,
                outDpiX,
                outDpiY,
                working.Format,
                working.Palette,
                pixels,
                stride);

            var encoder = new BmpBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bmpSource));
            using var outMs = new MemoryStream();
            encoder.Save(outMs);

            // Report final DPI
            dpiX = outDpiX;
            dpiY = outDpiY;

            return outMs.ToArray();
        }
        catch (Exception ex)
        {
            Logger.Warning($"ConvertImageToBgr32PreserveDpi failed: {ex.Message}");
            return image;
        }
    }
}
