using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace LabelVal.Results.Helpers;
public class ConvertImageToBgr32PreserveDpi
{
    // Helper: ensure BGR32 (32bpp) and preserve or reconstruct DPI
    public static byte[] Convert(byte[] image, out double dpiX, out double dpiY)
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

            // If already Bgr32 with valid DPI just return original bytes
            if (frame.Format == System.Windows.Media.PixelFormats.Bgr32 && dpiX >= 10 && dpiY >= 10)
                return image;

            // Convert format if needed
            BitmapSource working = frame.Format == System.Windows.Media.PixelFormats.Bgr32
                ? frame
                : new FormatConvertedBitmap(frame, System.Windows.Media.PixelFormats.Bgr32, null, 0);

            // If DPI invalid, leave (we will fallback later in caller); otherwise we keep it
            if (dpiX < 10 || dpiY < 10)
            {
                // Keep placeholder (caller supplies fallback); do not force here to avoid assuming context
                dpiX = dpiY = 0;
            }

            // Re-pack to guarantee 32bpp BGR byte ordering (BMP stores pixels-per-meter for DPI)
            var encoder = new BmpBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(CloneWithOptionalDpi(working, dpiX, dpiY)));
            using var outMs = new MemoryStream();
            encoder.Save(outMs);
            return outMs.ToArray();
        }
        catch (Exception ex)
        {
            Logger.Warning($"ConvertImageToBgr32PreserveDpi failed: {ex.Message}");
            return image;
        }

        static BitmapSource CloneWithOptionalDpi(BitmapSource src, double dpiX, double dpiY)
        {
            // If dpiX/dpiY are zero or invalid we just keep pixel data; DPI fallback applied later.
            if (dpiX < 10 || dpiY < 10)
                (dpiX, dpiY) = (src.DpiX, src.DpiY);

            int stride = (src.PixelWidth * src.Format.BitsPerPixel + 7) / 8;
            var pixels = new byte[stride * src.PixelHeight];
            src.CopyPixels(pixels, stride, 0);
            return BitmapSource.Create(src.PixelWidth, src.PixelHeight, dpiX, dpiY, src.Format, src.Palette, pixels, stride);
        }
    }

}
