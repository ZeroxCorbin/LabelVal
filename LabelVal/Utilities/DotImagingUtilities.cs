
using DotImaging;
using SharpDX.Direct3D9;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace LabelVal.Utilities;
public class DotImagingUtilities
{

    public static byte[] GetBmp(byte[] img, PixelFormat pixelFormat)
    {
        //if (IsBmp(img))
        //    return img;

        using MemoryStream ms = new(img);
        using var bitmap = new Bitmap(ms);

        //Use DotImaging to convert the bitmap to the desired pixel format
        if(pixelFormat == PixelFormat.Format32bppArgb)
        {
            var newImg = bitmap.ToImage<Bgr<byte>>();
            var newBmp = newImg.ToBitmap();
            using MemoryStream ms2 = new();
            newBmp.Save(ms2, ImageFormat.Bmp);
            return ms2.ToArray();
        }

        if(pixelFormat == PixelFormat.Format8bppIndexed)
        {
            var newImg = bitmap.ToImage<Gray<byte>>();
            var newBmp = newImg.ToBitmap();
            using MemoryStream ms2 = new();
            newBmp.Save(ms2, ImageFormat.Bmp);
            return ms2.ToArray();
        }

        return img;


    }
}
