using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabelVal.Utilities;
public class BitmapUtilities
{
    public static System.Drawing.Bitmap LoadBitmap(string path) => new System.Drawing.Bitmap(path);
    public static void SaveBitmap(System.Drawing.Bitmap bitmap, string path) => bitmap.Save(path);

    public static System.Drawing.Bitmap CreateBitmap(int width, int height) => new System.Drawing.Bitmap(width, height);
    public static System.Drawing.Bitmap CreateBitmap(int width, int height, System.Drawing.Imaging.PixelFormat format) => new System.Drawing.Bitmap(width, height, format);
    public static System.Drawing.Bitmap CreateBitmap(int width, int height, System.Drawing.Imaging.PixelFormat format, System.Drawing.Color color)
    {
        var bitmap = new System.Drawing.Bitmap(width, height, format);
        using (var g = System.Drawing.Graphics.FromImage(bitmap))
        {
            g.Clear(color);
        }
        return bitmap;
    }
    public static System.Drawing.Bitmap CreateBitmap(int width, int height, System.Drawing.Color color) => CreateBitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb, color);
    public static System.Drawing.Bitmap CreateBitmap(int width, int height, byte[] bytes)
    {
        var bitmap = new System.Drawing.Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        var data = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, width, height), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        System.Runtime.InteropServices.Marshal.Copy(bytes, 0, data.Scan0, bytes.Length);
        bitmap.UnlockBits(data);
        return bitmap;
    }


    public static System.Drawing.Bitmap BitmapFromBytes(byte[] bytes)
    {
        using (var ms = new System.IO.MemoryStream(bytes))
        {
            return new System.Drawing.Bitmap(ms);
        }
    }

    public static byte[] BytesFromBitmap(System.Drawing.Bitmap bitmap)
    {
        using (var ms = new System.IO.MemoryStream())
        {
            bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
            return ms.ToArray();
        }
    }


    public static System.Drawing.Bitmap ResizeBitmap(System.Drawing.Bitmap bitmap, int width, int height)
    {
        var resized = new System.Drawing.Bitmap(width, height);
        using (var g = System.Drawing.Graphics.FromImage(resized))
        {
            g.DrawImage(bitmap, 0, 0, width, height);
        }
        return resized;
    }

    public static System.Drawing.Bitmap CropBitmap(System.Drawing.Bitmap bitmap, int x, int y, int width, int height)
    {
        var cropped = new System.Drawing.Bitmap(width, height);
        using (var g = System.Drawing.Graphics.FromImage(cropped))
        {
            g.DrawImage(bitmap, new System.Drawing.Rectangle(0, 0, width, height), new System.Drawing.Rectangle(x, y, width, height), System.Drawing.GraphicsUnit.Pixel);
        }
        return cropped;
    }

    public static System.Drawing.Bitmap RotateBitmap(System.Drawing.Bitmap bitmap, float angle)
    {
        var rotated = new System.Drawing.Bitmap(bitmap.Width, bitmap.Height);
        using (var g = System.Drawing.Graphics.FromImage(rotated))
        {
            g.TranslateTransform(bitmap.Width / 2, bitmap.Height / 2);
            g.RotateTransform(angle);
            g.TranslateTransform(-bitmap.Width / 2, -bitmap.Height / 2);
            g.DrawImage(bitmap, 0, 0);
        }
        return rotated;
    }

    public static System.Drawing.Bitmap FlipBitmap(System.Drawing.Bitmap bitmap, bool horizontal, bool vertical)
    {
        var flipped = new System.Drawing.Bitmap(bitmap.Width, bitmap.Height);
        using (var g = System.Drawing.Graphics.FromImage(flipped))
        {
            if (horizontal)
            {
                g.ScaleTransform(-1, 1);
                g.TranslateTransform(-bitmap.Width, 0);
            }
            if (vertical)
            {
                g.ScaleTransform(1, -1);
                g.TranslateTransform(0, -bitmap.Height);
            }
            g.DrawImage(bitmap, 0, 0);
        }
        return flipped;
    }

}
