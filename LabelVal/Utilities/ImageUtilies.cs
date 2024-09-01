using SharpVectors.Converters;
using SharpVectors.Dom.Svg;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Windows.Media.Imaging;

namespace LabelVal.Utilities;

public static class ImageUtilities
{
    private const double InchesPerMeter = 39.3701;
    private const int PngSignatureLength = 8;
    private const int PhysChunkLength = 9;
    private const int PhysChunkType = 0x70485973; // 'pHYs' in ASCII
    private const int IdatChunkType = 0x49444154; // 'IDAT' in ASCII

    public class DPI
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    /// <summary>
    /// Get PNG image from PNG or BMP image.
    /// Copies DPI, PixelFormat, and metadata if converted from BMP.
    /// </summary>
    /// <param name="img"></param>
    /// <returns>Converted BMP or original image</returns>
    public static byte[] GetPng(byte[] img)
    {
        if (IsPng(img))
        {
            return img;
        }

        using MemoryStream ms = new(img);
        using Bitmap bitmap = new(ms);
        using MemoryStream stream = new();

        float dpiX = bitmap.HorizontalResolution;
        float dpiY = bitmap.VerticalResolution;

        bitmap.SetResolution(dpiX, dpiY);
        bitmap.Save(stream, ImageFormat.Png);
        return stream.ToArray();
    }
    /// <summary>
    /// Get PNG image from PNG or BMP image. Sets the DPI in the PNG image, if needed.
    /// Copies PixelFormat and metadata if converted from BMP.
    /// </summary>
    /// <param name="img"></param>
    /// <param name="dpiX"></param>
    /// <param name="dpiY"></param>
    /// <returns>Converted BMP or updated orginal</returns>
    /// <exception cref="ArgumentException"></exception>
    public static byte[] GetPng(byte[] img, int dpiX, int dpiY = 0)
    {
        if (dpiX <= 0)
        {
            throw new ArgumentException("DPI value must be greater than zero.");
        }

        if (dpiY <= 0)
        {
            dpiY = dpiX; // Use dpiX if dpiY is not provided or invalid
        }

        if (IsPng(img))
        {
            DPI dpi = GetPngDPI(img);
            if (dpi.X == dpiX && dpi.Y == dpiY)
            {
                return img;
            }
            else
            {
                return SetPngDPI(img, dpiX, dpiY); ;
            }
        }

        using MemoryStream ms = new(img);
        using Bitmap bitmap = new(ms);
        using MemoryStream stream = new();

        bitmap.SetResolution(dpiX, dpiY);
        bitmap.Save(stream, ImageFormat.Png);
        return stream.ToArray();
    }
    public static byte[] GetPng(byte[] img, PixelFormat pixelFormat)
    {
        if (IsPng(img))
            return GetPngPixelFormat(img) == pixelFormat ? img : SetPngPixelFormat(img, pixelFormat);

        byte[] res = IsBmp(img) ? SetBmpPixelFormat(img, pixelFormat) : throw new ArgumentException("Unsupported image format.");

        using MemoryStream ms = new(res);
        using Bitmap bitmap = new(ms);
        using MemoryStream stream = new();

        bitmap.Save(stream, ImageFormat.Png);
        return stream.ToArray();
    }

    /// <summary>
    /// Get BMP image from PNG or BMP image.
    /// </summary>
    /// <param name="img"></param>
    /// <returns></returns>
    public static byte[] GetBmp(byte[] img)
    {
        if (IsBmp(img))
        {
            return img;
        }

        using MemoryStream ms = new(img);
        using Bitmap bitmap = new(ms);
        using MemoryStream stream = new();

        bitmap.Save(stream, ImageFormat.Bmp);
        return stream.ToArray();
    }
    /// <summary>
    /// Get BMP image from PNG or BMP image. Sets the DPI in the BMP image, if needed.
    /// </summary>
    /// <param name="img"></param>
    /// <param name="dpiX"></param>
    /// <param name="dpiY"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static byte[] GetBmp(byte[] img, int dpiX, int dpiY = 0)
    {
        if (dpiX <= 0)
        {
            throw new ArgumentException("DPI value must be greater than zero.");
        }

        if (dpiY <= 0)
        {
            dpiY = dpiX; // Use dpiX if dpiY is not provided or invalid
        }

        if (IsBmp(img))
        {
            DPI dpi = GetBitmapDPI(img);
            if (dpi.X == dpiX && dpi.Y == dpiY)
            {
                return img;
            }
            else
            {
                return SetBitmapDPI(img, dpiX, dpiY);
            }
        }

        using MemoryStream ms = new(img);
        using Bitmap bitmap = new(ms);
        using MemoryStream stream = new();

        bitmap.SetResolution(dpiX, dpiY);
        bitmap.Save(stream, ImageFormat.Bmp);
        return stream.ToArray();
    }

    /// <summary>
    /// Get the DPI of a PNG or BMP image.
    /// </summary>
    /// <param name="image"></param>
    /// <returns>DPI</returns>
    /// <exception cref="ArgumentException"></exception>
    public static DPI GetImageDPI(byte[] image)
    {
        if (IsPng(image))
        {
            return GetPngDPI(image);
        }
        else
        {
            return IsBmp(image) ? GetBitmapDPI(image) : throw new ArgumentException("Unsupported image format.");
        }
    }
    /// <summary>
    /// Set the header DPI of a PNG or BMP image.
    /// </summary>
    /// <param name="image"></param>
    /// <param name="dpiX"></param>
    /// <param name="dpiY"></param>
    /// <exception cref="ArgumentException"></exception>
    public static byte[] SetImageDPI(byte[] image, int dpiX, int dpiY = 0)
    {
        if (dpiX <= 0)
        {
            throw new ArgumentException("DPI value must be greater than zero.");
        }

        if (dpiY <= 0)
        {
            dpiY = dpiX; // Use dpiX if dpiY is not provided or invalid
        }

        if (IsPng(image))
        {
            return SetPngDPI(image, dpiX, dpiY);
        }
        else
        {
            return IsBmp(image) ? SetBitmapDPI(image, dpiX, dpiY) : throw new ArgumentException("Unsupported image format.");
        }
    }

    /// <summary>
    /// Get the UID of the entire byte array.
    /// </summary>
    /// <param name="image"></param>
    /// <returns>SHA256 Hash string with hyphen removed</returns>
    public static string GetImageUID(byte[] image)
    {
        try
        {
            using SHA256 md5 = SHA256.Create();
            return BitConverter.ToString(md5.ComputeHash(image)).Replace("-", String.Empty);

        }
        catch (Exception)
        {
            return string.Empty;
        }
    }
    /// <summary>
    /// Get the UID of the image data array only. No header information.
    /// </summary>
    /// <param name="image"></param>
    /// <returns>SHA256 Hash string with hyphen removed</returns>
    public static string GetImageDataUID(byte[] image)
    {
        try
        {
            byte[] imageData;

            if (IsPng(image))
            {
                imageData = ExtractPngData(image);
            }
            else
            {
                imageData = IsBmp(image) ? ExtractBitmapData(image) : throw new ArgumentException("Unsupported image format.");
            }

            using SHA256 sha256 = SHA256.Create();
            return BitConverter.ToString(sha256.ComputeHash(imageData)).Replace("-", string.Empty);
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }

    private static bool IsPng(byte[] img) =>
        img.Length >= 8 &&
        img[0] == 0x89 &&
        img[1] == 0x50 &&
        img[2] == 0x4E &&
        img[3] == 0x47 &&
        img[4] == 0x0D &&
        img[5] == 0x0A &&
        img[6] == 0x1A &&
        img[7] == 0x0A &&
        img.Length > PngSignatureLength + PhysChunkLength;
    private static bool IsBmp(byte[] img) =>
        img.Length >= 2 &&
        img[0] == 0x42 &&
        img[1] == 0x4D &&
        img.Length >= 54;

    private static DPI GetBitmapDPI(byte[] image) =>
        IsBmp(image) == false
            ? throw new ArgumentException("The provided byte array is not a valid BMP image.")
            : new DPI
            {
                X = DotsPerInch(BitConverter.ToInt32(image, 38)),
                Y = DotsPerInch(BitConverter.ToInt32(image, 42))
            };
    /// <summary>
    /// Get the pixel format of a BMP image by reading the header bytes.
    /// </summary>
    /// <param name="image">BMP image byte array</param>
    /// <returns>PixelFormat</returns>
    public static PixelFormat GetBmpPixelFormat(byte[] image)
    {
        if (!IsBmp(image))
            throw new ArgumentException("The provided byte array is not a valid BMP image.");

        // Bit count per pixel is at byte 28 in the BMP header
        int bitCount = BitConverter.ToInt16(image, 28);

        return bitCount switch
        {
            1 => PixelFormat.Format1bppIndexed,
            4 => PixelFormat.Format4bppIndexed,
            8 => PixelFormat.Format8bppIndexed,
            16 => PixelFormat.Format16bppRgb565,
            24 => PixelFormat.Format24bppRgb,
            32 => PixelFormat.Format32bppArgb,
            _ => throw new NotSupportedException("Unsupported BMP bit count.")
        };
    }
    public static byte[] SetBitmapDPI(byte[] image, int dpiX, int dpiY)
    {
        if (!IsBmp(image))
            throw new ArgumentException("The provided byte array is not a valid BMP image.");

        int dpiXInMeters = DotsPerMeter(dpiX);
        int dpiYInMeters = DotsPerMeter(dpiY);

        // Set the horizontal DPI
        for (int i = 38; i < 42; i++)
        {
            image[i] = BitConverter.GetBytes(dpiXInMeters)[i - 38];
        }

        // Set the vertical DPI
        for (int i = 42; i < 46; i++)
        {
            image[i] = BitConverter.GetBytes(dpiYInMeters)[i - 42];
        }

        return image;
    }
    /// <summary>
    /// Converts a BMP byte array to a new PixelFormat.
    /// </summary>
    /// <param name="image">BMP image byte array</param>
    /// <param name="newPixelFormat">The new PixelFormat</param>
    /// <returns>Converted BMP image byte array</returns>
    private static byte[] SetBmpPixelFormat(byte[] image, PixelFormat newPixelFormat)
    {
        if (!IsBmp(image))
            throw new ArgumentException("The provided byte array is not a valid BMP image.");

        using MemoryStream inputStream = new(image);
        using Bitmap originalBitmap = new(inputStream);
        using Bitmap newBitmap = originalBitmap.Clone(new Rectangle(0, 0, originalBitmap.Width, originalBitmap.Height), newPixelFormat);

        using MemoryStream outputStream = new();
        newBitmap.Save(outputStream, ImageFormat.Bmp);
        return outputStream.ToArray();
    }
    public static byte[] ExtractBitmapData(byte[] image)
    {
        if (!IsBmp(image))
            throw new ArgumentException("The provided byte array is not a valid BMP image.");

        int dataOffset = BitConverter.ToInt32(image, 10);
        return image[dataOffset..];
    }
    public static byte[] ExtractBitmapIndexedColorPallet(byte[] image)
    {
        if (!IsBmp(image))
            throw new ArgumentException("The provided byte array is not a valid BMP image.");

        int dataOffset = BitConverter.ToInt32(image, 10);
        int palletOffset = 54;

        return image[palletOffset..dataOffset];
    }

    private static DPI GetPngDPI(byte[] image)
    {
        if (!IsPng(image))
            throw new ArgumentException("The provided byte array is not a valid PNG image.");

        using MemoryStream ms = new(image);
        using BinaryReader reader = new(ms);

        // Skip the PNG signature
        reader.BaseStream.Seek(PngSignatureLength, SeekOrigin.Begin);

        while (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            int chunkLength = BitConverter.ToInt32(reader.ReadBytes(4).Reverse().ToArray(), 0);
            int chunkType = BitConverter.ToInt32(reader.ReadBytes(4), 0);

            if (chunkType == PhysChunkType)
            {
                int pixelsPerUnitX = BitConverter.ToInt32(reader.ReadBytes(4).Reverse().ToArray(), 0);
                int pixelsPerUnitY = BitConverter.ToInt32(reader.ReadBytes(4).Reverse().ToArray(), 0);
                byte unitSpecifier = reader.ReadByte();

                return unitSpecifier == 1
                    ? new DPI()
                    {
                        X = (int)(pixelsPerUnitX * InchesPerMeter),
                        Y = (int)(pixelsPerUnitY * InchesPerMeter)
                    }
                    : throw new ArgumentException("Unsupported unit specifier in pHYs chunk.");
            }
            else
            {
                // Skip the chunk data and CRC
                reader.BaseStream.Seek(chunkLength + 4, SeekOrigin.Current);
            }
        }

        throw new ArgumentException("pHYs chunk not found in PNG file.");
    }
    /// <summary>
    /// Get the pixel format of a PNG image by reading the header bytes.
    /// </summary>
    /// <param name="image">PNG image byte array</param>
    /// <returns>PixelFormat</returns>
    public static PixelFormat GetPngPixelFormat(byte[] image)
    {
        if (!IsPng(image))
            throw new ArgumentException("The provided byte array is not a valid PNG image.");

        // Color type is at byte 25 in the PNG header
        byte colorType = image[25];

        return colorType switch
        {
            0 => PixelFormat.Format8bppIndexed, // Grayscale
            2 => PixelFormat.Format24bppRgb,    // Truecolor
            3 => PixelFormat.Format8bppIndexed, // Indexed-color
            4 => PixelFormat.Format16bppGrayScale, // Grayscale with alpha
            6 => PixelFormat.Format32bppArgb,   // Truecolor with alpha
            _ => throw new NotSupportedException("Unsupported PNG color type.")
        };
    }
    private static byte[] SetPngDPI(byte[] image, int dpiX, int dpiY)
    {
        if (!IsPng(image))
            throw new ArgumentException("The provided byte array is not a valid PNG image.");

        using MemoryStream ms = new(image);
        using BinaryReader reader = new(ms);
        using BinaryWriter writer = new(ms);

        // Skip the PNG signature
        reader.BaseStream.Seek(PngSignatureLength, SeekOrigin.Begin);

        while (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            int chunkLength = BitConverter.ToInt32(reader.ReadBytes(4).Reverse().ToArray(), 0);
            int chunkType = BitConverter.ToInt32(reader.ReadBytes(4), 0);

            if (chunkType == PhysChunkType)
            {
                ms.Seek(-4, SeekOrigin.Current); // Move back to the start of the chunk data
                writer.Write(BitConverter.GetBytes(chunkLength).Reverse().ToArray());
                writer.Write(BitConverter.GetBytes(chunkType));

                writer.Write(BitConverter.GetBytes((int)(dpiX / InchesPerMeter)).Reverse().ToArray());
                writer.Write(BitConverter.GetBytes((int)(dpiY / InchesPerMeter)).Reverse().ToArray());
                writer.Write((byte)1); // Unit specifier: 1 for meters

                // Skip the CRC
                reader.BaseStream.Seek(4, SeekOrigin.Current);

                return image;
            }
            else
            {
                // Skip the chunk data and CRC
                reader.BaseStream.Seek(chunkLength + 4, SeekOrigin.Current);
            }
        }

        throw new ArgumentException("pHYs chunk not found in PNG file.");
    }
    /// <summary>
    /// Converts a PNG byte array to a new PixelFormat.
    /// </summary>
    /// <param name="image">PNG image byte array</param>
    /// <param name="newPixelFormat">The new PixelFormat</param>
    /// <returns>Converted PNG image byte array</returns>
    public static byte[] SetPngPixelFormat(byte[] image, PixelFormat newPixelFormat)
    {
        if (!IsPng(image))
            throw new ArgumentException("The provided byte array is not a valid PNG image.");

        using MemoryStream inputStream = new(image);
        using Bitmap originalBitmap = new(inputStream);
        Bitmap newBitmap = new Bitmap(originalBitmap.Width, originalBitmap.Height, newPixelFormat);

        // Use Graphics to draw the original bitmap onto the new bitmap
        using (Graphics g = Graphics.FromImage(newBitmap))
            g.DrawImage(originalBitmap, new Rectangle(0, 0, newBitmap.Width, newBitmap.Height));

        using MemoryStream outputStream = new();
        newBitmap.Save(outputStream, ImageFormat.Png);
        return outputStream.ToArray();
    }
    private static byte[] ExtractPngData(byte[] image)
    {
        if (!IsPng(image))
            throw new ArgumentException("The provided byte array is not a valid PNG image.");

        using MemoryStream ms = new(image);
        using BinaryReader reader = new(ms);

        // Skip the PNG signature
        reader.BaseStream.Seek(PngSignatureLength, SeekOrigin.Begin);

        using MemoryStream dataStream = new();

        while (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            int chunkLength = BitConverter.ToInt32(reader.ReadBytes(4).Reverse().ToArray(), 0);
            int chunkType = BitConverter.ToInt32(reader.ReadBytes(4), 0);

            if (chunkType == IdatChunkType)
            {
                byte[] chunkData = reader.ReadBytes(chunkLength);
                dataStream.Write(chunkData, 0, chunkLength);
            }
            else
            {
                // Skip the chunk data and CRC
                reader.BaseStream.Seek(chunkLength + 4, SeekOrigin.Current);
            }
        }

        return dataStream.ToArray();
    }

    private static int DotsPerInch(int dpm) => (int)Math.Round(dpm / InchesPerMeter);
    private static int DotsPerMeter(int dpi) => (int)Math.Round(dpi * InchesPerMeter);

    //Seek #FileIndex, 7   ' position of balance information
    //Get #FileIndex, , BalanceInfo
    //If BalanceInfo = Asc("C") Or BalanceInfo = Asc("3") Then
    //  ColorFlag = Chr(BalanceInfo)
    //End If
    //public static void SetBitmapColorFlag(byte[] image, char balanceInfo)
    //{
    //    byte[] value = BitConverter.GetBytes(balanceInfo);

    //    int i = 7;
    //    foreach (byte b in value)
    //        image[i++] = b;
    //}

    //public static byte[] RotatePNG(byte[] img, double angle = 0)
    //{

    //    System.Windows.Media.Imaging.PngBitmapEncoder encoder = new();
    //    using MemoryStream ms = new(img);
    //    using MemoryStream stream = new();

    //    // Create a BitmapImage from the byte array
    //    BitmapImage bitmapImage = new();
    //    bitmapImage.BeginInit();
    //    bitmapImage.StreamSource = ms;
    //    bitmapImage.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
    //    bitmapImage.EndInit();
    //    bitmapImage.Freeze();

    //    // Apply rotation if angle is not zero
    //    if (angle != 0)
    //    {
    //        TransformedBitmap transformedBitmap = new(
    //            bitmapImage, new System.Windows.Media.RotateTransform(angle));
    //        encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(transformedBitmap));
    //    }
    //    else
    //    {
    //        encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmapImage));
    //    }

    //    encoder.Save(stream);
    //    stream.Close();

    //    return stream.ToArray();

    //}

    //public static byte[] AddOverlayPNG(byte[] img, byte[] overlay)
    //{

    //    System.Windows.Media.DrawingGroup dg = new();

    //    BitmapImage renderBitmap1 = CreateBitmap(img);
    //    BitmapImage renderBitmap2 = CreateBitmap(overlay);

    //    System.Windows.Media.ImageDrawing id1 = new(renderBitmap1, new System.Windows.Rect(0, 0, renderBitmap1.Width, renderBitmap1.Height));
    //    System.Windows.Media.ImageDrawing id2 = new(renderBitmap2, new System.Windows.Rect(0, 0, renderBitmap2.Width, renderBitmap2.Height));

    //    dg.Children.Add(id1);
    //    dg.Children.Add(id2);

    //    RenderTargetBitmap combinedImg = new(
    //        (int)renderBitmap1.Width,
    //        (int)renderBitmap1.Height, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);

    //    System.Windows.Media.DrawingVisual dv = new();
    //    using (System.Windows.Media.DrawingContext dc = dv.RenderOpen())
    //    {
    //        dc.DrawDrawing(dg);
    //    }

    //    combinedImg.Render(dv);

    //    System.Windows.Media.Imaging.PngBitmapEncoder encoder = new();
    //    using MemoryStream stream = new();
    //    encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(combinedImg));

    //    encoder.Save(stream);
    //    stream.Close();

    //    return stream.ToArray();

    //}

    ////public static byte[] ConvertToBmp(byte[] img)
    ////{
    ////    System.Windows.Media.Imaging.BmpBitmapEncoder encoder = new();
    ////    using var ms = new System.IO.MemoryStream(img);
    ////    using MemoryStream stream = new();
    ////    encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(ms));
    ////    encoder.Save(stream);
    ////    stream.Close();

    ////    return stream.ToArray();
    ////}
    ////public static byte[] ConvertToBmp(byte[] img, int dpi)
    ////{
    ////    System.Windows.Media.Imaging.BmpBitmapEncoder encoder = new();
    ////    using var ms = new System.IO.MemoryStream(img);
    ////    using MemoryStream stream = new();
    ////    encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(ms));
    ////    encoder.Save(stream);
    ////    stream.Close();

    ////    byte[] ret = stream.ToArray();

    ////    if (dpi > 0)
    ////        SetBitmapDPI(ret, dpi);

    ////    return ret;
    ////}
    //public static System.Windows.Media.Imaging.BitmapImage CreateBitmap(byte[] data, int decodePixelWidth = 0)
    //{
    //    if (data == null || data.Length < 2)
    //        return null;

    //    try
    //    {
    //        System.Windows.Media.Imaging.BitmapImage img = new();

    //        using (MemoryStream memStream = new(data))
    //        {
    //            img.BeginInit();
    //            img.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
    //            img.StreamSource = memStream;
    //            img.DecodePixelWidth = decodePixelWidth;
    //            img.EndInit();
    //            img.Freeze();

    //        }
    //        return img;
    //    }
    //    catch { }

    //    return null;
    //}

    //public static byte[] ImageToBytes(System.Drawing.Image image)
    //{
    //    System.Drawing.ImageConverter converter = new();
    //    return (byte[])converter.ConvertTo(image, typeof(byte[]));
    //}

    //private static byte[] imageToBytes(System.Windows.Media.ImageSource imageSource)
    //{
    //    System.Windows.Media.Imaging.PngBitmapEncoder encoder = new();
    //    byte[] bytes = null;

    //    if (imageSource is System.Windows.Media.Imaging.BitmapSource bitmapSource)
    //    {
    //        encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmapSource));

    //        using MemoryStream stream = new();
    //        encoder.Save(stream);
    //        bytes = stream.ToArray();
    //    }

    //    return bytes;
    //}
    //public static byte[] ImageToBytes(this System.Windows.Media.DrawingImage source)
    //{
    //    System.Windows.Media.DrawingVisual drawingVisual = new();
    //    System.Windows.Media.DrawingContext drawingContext = drawingVisual.RenderOpen();
    //    drawingContext.DrawImage(source, new System.Windows.Rect(new System.Windows.Point(0, 0), new System.Windows.Size(source.Width, source.Height)));
    //    drawingContext.Close();

    //    RenderTargetBitmap bmp = new((int)source.Width, (int)source.Height, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
    //    bmp.Render(drawingVisual);
    //    return imageToBytes(bmp);
    //}

    //public static System.Windows.Media.Imaging.BitmapSource ToBitmapSource(this System.Windows.Media.DrawingImage source)
    //{
    //    System.Windows.Media.DrawingVisual drawingVisual = new();
    //    System.Windows.Media.DrawingContext drawingContext = drawingVisual.RenderOpen();
    //    drawingContext.DrawImage(source, new System.Windows.Rect(new System.Windows.Point(0, 0), new System.Windows.Size(source.Width, source.Height)));
    //    drawingContext.Close();

    //    RenderTargetBitmap bmp = new((int)source.Width, (int)source.Height, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
    //    bmp.Render(drawingVisual);
    //    return bmp;
    //}

    public static void RedrawFiducial(string path, bool is300)
    {
        // load your photo
        using FileStream fs = new(path, FileMode.Open);
        Bitmap photo = (Bitmap)Bitmap.FromStream(fs);
        fs.Close();

        Bitmap newmap = new(photo.Width, photo.Height);
        newmap.SetResolution(photo.HorizontalResolution, photo.VerticalResolution);
        //if (photo.Height != 2400)
        //    File.AppendAllText($"{UserDataDirectory}\\Small Images List", Path.GetFileName(path));

        //if (is300)
        //{//600 DPI
        //    if ((photo.Height > 2400 && photo.Height != 4800) || photo.Height < 2000)
        //        return;
        //}
        //else
        //{//300 DPI
        //    if ((photo.Height > 1200) || photo.Height < 1000)
        //        return;
        //}

        using Graphics graphics = Graphics.FromImage(newmap);
        graphics.DrawImage(photo, 0, 0, photo.Width, photo.Height);

        if (is300)
        {//300 DPI
            graphics.FillRectangle(Brushes.White, 0, 976, 150, photo.Height - 976);
            graphics.FillRectangle(Brushes.Black, 15, 975, 45, 45);
        }
        else
        {
            graphics.FillRectangle(Brushes.White, 0, 1900, 195, photo.Height - 1900);
            graphics.FillRectangle(Brushes.Black, 30, 1950, 90, 90);
        }

        newmap.Save(path, ImageFormat.Png);

    }
}
