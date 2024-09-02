
using DotImaging;
using SharpDX.Direct3D9;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace LabelVal.Utilities;
public class DotImagingUtilities
{



public static byte[] GetBmp(byte[] img)
{
        //if (IsBmp(img))
        //    return img;

        using MemoryStream ms = new(img);
        using var bitmap = new Bitmap(ms);

        // Determine the appropriate type for the ToImage<TColor> method
        var pixelFormat = bitmap.PixelFormat;
        dynamic image;

        switch (pixelFormat)
        {
            case PixelFormat.Format24bppRgb:
                image = bitmap.ToImage<Bgr<byte>>();
                break;
            case PixelFormat.Format32bppArgb:
                image = bitmap.ToImage<Bgra<byte>>();
                break;
            case PixelFormat.Format8bppIndexed:
                image = bitmap.ToImage<Gray<byte>>();
                break;
            default:
                throw new NotSupportedException($"Pixel format {pixelFormat} is not supported.");
        }

        // Create a new memory stream to save the BMP
        using MemoryStream stream = new();

        // Save the image as BMP while preserving the pixel format
        image.Save(stream, ImageFileFormat.Bmp);

        return stream.ToArray();
    }

}
